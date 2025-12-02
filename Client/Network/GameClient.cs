using System.Buffers;
using System.Net.Sockets;
using RealmOfReality.Shared.Network;
using RealmOfReality.Shared.Serialization;

namespace RealmOfReality.Client.Network;

/// <summary>
/// TCP client for connecting to game server
/// </summary>
public class GameClient : IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    
    private readonly string _serverAddress;
    private readonly int _serverPort;
    
    public bool IsConnected => _client?.Connected ?? false;
    public event Action? Connected;
    public event Action<string>? Disconnected;
    public event Action<Packet>? PacketReceived;
    
    public GameClient(string serverAddress = "127.0.0.1", int port = 7775)
    {
        _serverAddress = serverAddress;
        _serverPort = port;
    }
    
    /// <summary>
    /// Connect to the server
    /// </summary>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_serverAddress, _serverPort, cancellationToken);
            _stream = _client.GetStream();
            
            _cts = new CancellationTokenSource();
            _receiveTask = ReceiveLoopAsync(_cts.Token);
            
            Connected?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            Disconnect($"Connection failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Disconnect from server
    /// </summary>
    public void Disconnect(string reason = "")
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        
        _stream?.Dispose();
        _stream = null;
        
        _client?.Dispose();
        _client = null;
        
        Disconnected?.Invoke(reason);
    }
    
    /// <summary>
    /// Send a packet to the server
    /// </summary>
    public async Task SendAsync(Packet packet)
    {
        if (_stream == null || !IsConnected)
            throw new InvalidOperationException("Not connected");
        
        var data = packet.Build();
        await _stream.WriteAsync(data);
        await _stream.FlushAsync();
    }
    
    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var accumulator = new List<byte>();
        
        try
        {
            while (!cancellationToken.IsCancellationRequested && _stream != null)
            {
                var bytesRead = await _stream.ReadAsync(buffer, cancellationToken);
                
                if (bytesRead == 0)
                {
                    Disconnect("Server closed connection");
                    return;
                }
                
                accumulator.AddRange(buffer.Take(bytesRead));
                
                // Try to parse packets
                while (TryParsePacket(accumulator, out var packet))
                {
                    PacketReceived?.Invoke(packet);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal disconnect
        }
        catch (IOException ex)
        {
            Disconnect($"Connection lost: {ex.Message}");
        }
        catch (Exception ex)
        {
            Disconnect($"Error: {ex.Message}");
        }
    }
    
    private bool TryParsePacket(List<byte> buffer, out Packet packet)
    {
        packet = null!;
        
        if (buffer.Count < 6) // Header size
            return false;
        
        // Read length from first 4 bytes
        var length = BitConverter.ToInt32(buffer.Take(4).ToArray(), 0);
        var totalLength = 4 + length;
        
        if (buffer.Count < totalLength)
            return false;
        
        // Parse packet
        var data = buffer.Take(totalLength).ToArray();
        packet = PacketFactory.Deserialize(data)!;
        
        // Remove parsed bytes
        buffer.RemoveRange(0, totalLength);
        
        return packet != null;
    }
    
    public void Dispose()
    {
        Disconnect();
    }
}
