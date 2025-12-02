using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Network;
using RealmOfReality.Shared.Serialization;

namespace RealmOfReality.Server.Network;

/// <summary>
/// Represents a connected client
/// </summary>
public class ClientConnection : IDisposable
{
    private static int _nextId = 0;
    
    public int ConnectionId { get; }
    public TcpClient TcpClient { get; }
    public NetworkStream Stream { get; }
    public IPEndPoint RemoteEndPoint { get; }
    
    public DateTime ConnectedAt { get; }
    public DateTime LastActivity { get; set; }
    public long BytesSent { get; private set; }
    public long BytesReceived { get; private set; }
    
    // Session state
    public ClientState State { get; set; } = ClientState.Connected;
    public AccountId? AccountId { get; set; }
    public CharacterId? CharacterId { get; set; }
    public EntityId? PlayerEntityId { get; set; }
    
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;
    
    public CancellationToken CancellationToken => _cts.Token;
    
    public ClientConnection(TcpClient client)
    {
        ConnectionId = Interlocked.Increment(ref _nextId);
        TcpClient = client;
        Stream = client.GetStream();
        RemoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint!;
        ConnectedAt = DateTime.UtcNow;
        LastActivity = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Send a packet to this client
    /// </summary>
    public async Task SendAsync(Packet packet)
    {
        if (_disposed) return;
        
        var data = packet.Build();
        
        await _sendLock.WaitAsync(_cts.Token);
        try
        {
            await Stream.WriteAsync(data, _cts.Token);
            await Stream.FlushAsync(_cts.Token);
            BytesSent += data.Length;
        }
        finally
        {
            _sendLock.Release();
        }
    }
    
    /// <summary>
    /// Send raw data
    /// </summary>
    public async Task SendRawAsync(byte[] data)
    {
        if (_disposed) return;
        
        await _sendLock.WaitAsync(_cts.Token);
        try
        {
            await Stream.WriteAsync(data, _cts.Token);
            await Stream.FlushAsync(_cts.Token);
            BytesSent += data.Length;
        }
        finally
        {
            _sendLock.Release();
        }
    }
    
    public void AddBytesReceived(long bytes) => BytesReceived += bytes;
    
    public void Disconnect()
    {
        _cts.Cancel();
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _cts.Cancel();
        _cts.Dispose();
        _sendLock.Dispose();
        Stream.Dispose();
        TcpClient.Dispose();
    }
}

/// <summary>
/// Client connection state
/// </summary>
public enum ClientState
{
    Connected,
    Authenticating,
    Authenticated,
    CharacterSelect,
    EnteringWorld,
    InWorld,
    Disconnecting
}

/// <summary>
/// TCP server for handling client connections
/// </summary>
public class GameServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<int, ClientConnection> _clients = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _running;
    
    public event Action<ClientConnection>? ClientConnected;
    public event Action<ClientConnection>? ClientDisconnected;
    public event Action<ClientConnection, Packet>? PacketReceived;
    
    public int MaxConnections { get; set; } = 1000;
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public int ConnectionCount => _clients.Count;
    
    public GameServer(IPAddress bindAddress, int port, ILogger logger)
    {
        _listener = new TcpListener(bindAddress, port);
        _logger = logger;
    }
    
    /// <summary>
    /// Start accepting connections
    /// </summary>
    public void Start()
    {
        _listener.Start();
        _running = true;
        
        _logger.LogInformation("Server started on {EndPoint}", _listener.LocalEndpoint);
        
        // Start accept loop
        _ = AcceptLoopAsync();
    }
    
    /// <summary>
    /// Stop the server
    /// </summary>
    public void Stop()
    {
        _running = false;
        _cts.Cancel();
        _listener.Stop();
        
        // Disconnect all clients
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }
        _clients.Clear();
        
        _logger.LogInformation("Server stopped");
    }
    
    private async Task AcceptLoopAsync()
    {
        while (_running)
        {
            try
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(_cts.Token);
                
                if (_clients.Count >= MaxConnections)
                {
                    _logger.LogWarning("Max connections reached, rejecting {Remote}", tcpClient.Client.RemoteEndPoint);
                    tcpClient.Close();
                    continue;
                }
                
                var client = new ClientConnection(tcpClient);
                _clients[client.ConnectionId] = client;
                
                _logger.LogInformation("Client connected: {Id} from {Remote}", 
                    client.ConnectionId, client.RemoteEndPoint);
                
                ClientConnected?.Invoke(client);
                
                // Start receive loop for this client
                _ = ReceiveLoopAsync(client);
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting connection");
            }
        }
    }
    
    private async Task ReceiveLoopAsync(ClientConnection client)
    {
        var buffer = new byte[4096];
        var accumulator = new MemoryStream();
        
        try
        {
            while (!client.CancellationToken.IsCancellationRequested)
            {
                // Read from socket
                var bytesRead = await client.Stream.ReadAsync(buffer, client.CancellationToken);
                
                if (bytesRead == 0)
                {
                    // Client disconnected
                    break;
                }
                
                client.AddBytesReceived(bytesRead);
                client.LastActivity = DateTime.UtcNow;
                
                // Append to accumulator
                accumulator.Write(buffer, 0, bytesRead);
                
                // Try to parse packets
                while (TryParsePacket(accumulator, out var packet))
                {
                    try
                    {
                        PacketReceived?.Invoke(client, packet);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling packet {0} from client {1}", 
                            packet.Opcode, client.ConnectionId);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal disconnect
        }
        catch (IOException)
        {
            // Connection reset
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in receive loop for client {0}", client.ConnectionId);
        }
        finally
        {
            accumulator.Dispose();
            DisconnectClient(client);
        }
    }
    
    private bool TryParsePacket(MemoryStream accumulator, out Packet packet)
    {
        packet = null!;
        
        var data = accumulator.ToArray();
        
        // Need at least 6 bytes for header (4 length + 2 opcode)
        if (data.Length < PacketHeader.Size)
        {
            return false;
        }
        
        // Read length (little-endian)
        var length = BitConverter.ToInt32(data, 0);
        
        // Need the full packet
        var totalLength = 4 + length;
        if (data.Length < totalLength)
        {
            return false;
        }
        
        // Parse packet
        var packetData = new byte[totalLength];
        Array.Copy(data, 0, packetData, 0, totalLength);
        packet = PacketFactory.Deserialize(packetData)!;
        
        // Remove parsed bytes from accumulator
        var remaining = data.Length - totalLength;
        accumulator.SetLength(0);
        if (remaining > 0)
        {
            accumulator.Write(data, totalLength, remaining);
        }
        
        if (packet == null)
        {
            // Unknown packet, skip it
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Disconnect a client
    /// </summary>
    public void DisconnectClient(ClientConnection client, string reason = "")
    {
        if (!_clients.TryRemove(client.ConnectionId, out _))
            return;
        
        _logger.LogInformation("Client disconnected: {Id} ({Reason})", 
            client.ConnectionId, string.IsNullOrEmpty(reason) ? "closed" : reason);
        
        ClientDisconnected?.Invoke(client);
        client.Dispose();
    }
    
    /// <summary>
    /// Get a client by connection ID
    /// </summary>
    public ClientConnection? GetClient(int connectionId)
    {
        _clients.TryGetValue(connectionId, out var client);
        return client;
    }
    
    /// <summary>
    /// Get all connected clients
    /// </summary>
    public IEnumerable<ClientConnection> GetAllClients() => _clients.Values;
    
    /// <summary>
    /// Broadcast a packet to all clients in a specific state
    /// </summary>
    public async Task BroadcastAsync(Packet packet, ClientState? requiredState = null)
    {
        var data = packet.Build();
        var tasks = new List<Task>();
        
        foreach (var client in _clients.Values)
        {
            if (requiredState == null || client.State == requiredState)
            {
                tasks.Add(client.SendRawAsync(data));
            }
        }
        
        await Task.WhenAll(tasks);
    }
    
    /// <summary>
    /// Send to all clients on a specific map
    /// </summary>
    public async Task BroadcastToMapAsync(Packet packet, ushort mapId, 
        Func<ClientConnection, ushort?> getMapId)
    {
        var data = packet.Build();
        var tasks = new List<Task>();
        
        foreach (var client in _clients.Values)
        {
            if (client.State == ClientState.InWorld && getMapId(client) == mapId)
            {
                tasks.Add(client.SendRawAsync(data));
            }
        }
        
        await Task.WhenAll(tasks);
    }
    
    public void Dispose()
    {
        Stop();
        _cts.Dispose();
    }
}
