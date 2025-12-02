using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Entities;
using RealmOfReality.Shared.Serialization;

namespace RealmOfReality.Server.Data;

/// <summary>
/// Account data (stored in database/file)
/// </summary>
public class AccountData
{
    public AccountId Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime LastLogin { get; set; }
    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }
    public List<CharacterId> Characters { get; set; } = new();
    public AccessLevel AccessLevel { get; set; } = AccessLevel.Player;
}

/// <summary>
/// Character data (stored per-character)
/// </summary>
public class CharacterData
{
    public CharacterId Id { get; set; }
    public AccountId AccountId { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime LastPlayed { get; set; }
    public TimeSpan TotalPlayTime { get; set; }
    
    // Appearance
    public byte Gender { get; set; }
    public ushort BodyType { get; set; }
    public ushort SkinHue { get; set; }
    public ushort HairStyle { get; set; }
    public ushort HairHue { get; set; }
    
    // Location
    public ushort MapId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public Direction Facing { get; set; }
    
    // Stats
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int Mana { get; set; } = 100;
    public int MaxMana { get; set; } = 100;
    public int Stamina { get; set; } = 100;
    public int MaxStamina { get; set; } = 100;
    
    // Currency
    public long Gold { get; set; }
    
    // Skills (skill ID -> skill value 0-1000)
    public Dictionary<ushort, int> Skills { get; set; } = new();
    
    /// <summary>
    /// Create a PlayerEntity from this data
    /// </summary>
    public PlayerEntity ToEntity(EntityId entityId)
    {
        return new PlayerEntity
        {
            Id = entityId,
            AccountId = AccountId,
            CharacterId = Id,
            Name = Name,
            Position = new WorldPosition(X, Y, Z),
            Facing = Facing,
            MapId = MapId,
            Gender = Gender,
            BodyType = BodyType,
            SkinHue = SkinHue,
            HairStyle = HairStyle,
            HairHue = HairHue,
            Level = Level,
            Experience = Experience,
            Strength = Strength,
            Dexterity = Dexterity,
            Intelligence = Intelligence,
            Health = Health,
            MaxHealth = MaxHealth,
            Mana = Mana,
            MaxMana = MaxMana,
            Stamina = Stamina,
            MaxStamina = MaxStamina,
            Gold = Gold
        };
    }
    
    /// <summary>
    /// Update this data from a PlayerEntity
    /// </summary>
    public void UpdateFrom(PlayerEntity entity)
    {
        MapId = entity.MapId;
        X = entity.Position.X;
        Y = entity.Position.Y;
        Z = entity.Position.Z;
        Facing = entity.Facing;
        Level = entity.Level;
        Experience = entity.Experience;
        Health = entity.Health;
        MaxHealth = entity.MaxHealth;
        Mana = entity.Mana;
        MaxMana = entity.MaxMana;
        Stamina = entity.Stamina;
        MaxStamina = entity.MaxStamina;
        Gold = entity.Gold;
    }
}

/// <summary>
/// Simple file-based data store for accounts and characters
/// In production, replace with a proper database
/// </summary>
public class DataStore
{
    private readonly string _accountsPath;
    private readonly string _charactersPath;
    
    private readonly ConcurrentDictionary<AccountId, AccountData> _accounts = new();
    private readonly ConcurrentDictionary<CharacterId, CharacterData> _characters = new();
    private readonly ConcurrentDictionary<string, AccountId> _usernameIndex = new();
    private readonly ConcurrentDictionary<string, CharacterId> _characterNameIndex = new();
    
    private uint _nextAccountId = 1;
    private uint _nextCharacterId = 1;
    private readonly object _idLock = new();
    
    public DataStore(string accountsPath, string charactersPath)
    {
        _accountsPath = accountsPath;
        _charactersPath = charactersPath;
    }
    
    /// <summary>
    /// Load all data from disk
    /// </summary>
    public async Task LoadAsync()
    {
        // Load accounts
        if (File.Exists(_accountsPath))
        {
            var json = await File.ReadAllTextAsync(_accountsPath);
            var accounts = JsonSerializer.Deserialize<List<AccountData>>(json, JsonConfig.Default);
            if (accounts != null)
            {
                foreach (var account in accounts)
                {
                    _accounts[account.Id] = account;
                    _usernameIndex[account.Username.ToLowerInvariant()] = account.Id;
                    if (account.Id.Value >= _nextAccountId)
                        _nextAccountId = account.Id.Value + 1;
                }
            }
        }
        
        // Load characters
        Directory.CreateDirectory(_charactersPath);
        foreach (var file in Directory.GetFiles(_charactersPath, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var character = JsonSerializer.Deserialize<CharacterData>(json, JsonConfig.Default);
                if (character != null)
                {
                    _characters[character.Id] = character;
                    _characterNameIndex[character.Name.ToLowerInvariant()] = character.Id;
                    if (character.Id.Value >= _nextCharacterId)
                        _nextCharacterId = character.Id.Value + 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load character file {file}: {ex.Message}");
            }
        }
        
        Console.WriteLine($"Loaded {_accounts.Count} accounts and {_characters.Count} characters");
    }
    
    /// <summary>
    /// Save all data to disk
    /// </summary>
    public async Task SaveAsync()
    {
        // Save accounts
        Directory.CreateDirectory(Path.GetDirectoryName(_accountsPath) ?? ".");
        var accountsJson = JsonSerializer.Serialize(_accounts.Values.ToList(), JsonConfig.Pretty);
        await File.WriteAllTextAsync(_accountsPath, accountsJson);
        
        // Save characters
        Directory.CreateDirectory(_charactersPath);
        foreach (var character in _characters.Values)
        {
            var path = Path.Combine(_charactersPath, $"{character.Id.Value}.json");
            var json = JsonSerializer.Serialize(character, JsonConfig.Pretty);
            await File.WriteAllTextAsync(path, json);
        }
    }
    
    /// <summary>
    /// Hash a password
    /// </summary>
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
    
    /// <summary>
    /// Create a new account
    /// </summary>
    public AccountData? CreateAccount(string username, string passwordHash, string email)
    {
        var lowerUsername = username.ToLowerInvariant();
        
        if (_usernameIndex.ContainsKey(lowerUsername))
            return null; // Username taken
        
        AccountId id;
        lock (_idLock)
        {
            id = new AccountId(_nextAccountId++);
        }
        
        var account = new AccountData
        {
            Id = id,
            Username = username,
            PasswordHash = passwordHash,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };
        
        if (!_usernameIndex.TryAdd(lowerUsername, id))
            return null; // Race condition
        
        _accounts[id] = account;
        return account;
    }
    
    /// <summary>
    /// Get account by username
    /// </summary>
    public AccountData? GetAccountByUsername(string username)
    {
        if (_usernameIndex.TryGetValue(username.ToLowerInvariant(), out var id))
            return _accounts.GetValueOrDefault(id);
        return null;
    }
    
    /// <summary>
    /// Get account by ID
    /// </summary>
    public AccountData? GetAccount(AccountId id)
    {
        return _accounts.GetValueOrDefault(id);
    }
    
    /// <summary>
    /// Validate login credentials
    /// </summary>
    public AccountData? ValidateLogin(string username, string passwordHash)
    {
        var account = GetAccountByUsername(username);
        if (account == null || account.IsBanned)
            return null;
        
        if (account.PasswordHash != passwordHash)
            return null;
        
        account.LastLogin = DateTime.UtcNow;
        return account;
    }
    
    /// <summary>
    /// Check if a character name is available
    /// </summary>
    public bool IsCharacterNameAvailable(string name)
    {
        return !_characterNameIndex.ContainsKey(name.ToLowerInvariant());
    }
    
    /// <summary>
    /// Create a new character
    /// </summary>
    public CharacterData? CreateCharacter(AccountId accountId, string name, CharacterData template)
    {
        var account = GetAccount(accountId);
        if (account == null)
            return null;
        
        var lowerName = name.ToLowerInvariant();
        if (_characterNameIndex.ContainsKey(lowerName))
            return null; // Name taken
        
        CharacterId id;
        lock (_idLock)
        {
            id = new CharacterId(_nextCharacterId++);
        }
        
        template.Id = id;
        template.AccountId = accountId;
        template.Name = name;
        template.CreatedAt = DateTime.UtcNow;
        
        if (!_characterNameIndex.TryAdd(lowerName, id))
            return null; // Race condition
        
        _characters[id] = template;
        account.Characters.Add(id);
        
        return template;
    }
    
    /// <summary>
    /// Get character by ID
    /// </summary>
    public CharacterData? GetCharacter(CharacterId id)
    {
        return _characters.GetValueOrDefault(id);
    }
    
    /// <summary>
    /// Get all characters for an account
    /// </summary>
    public List<CharacterData> GetCharactersForAccount(AccountId accountId)
    {
        var account = GetAccount(accountId);
        if (account == null)
            return new List<CharacterData>();
        
        return account.Characters
            .Select(id => _characters.GetValueOrDefault(id))
            .Where(c => c != null)
            .Cast<CharacterData>()
            .ToList();
    }
    
    /// <summary>
    /// Save a single character
    /// </summary>
    public async Task SaveCharacterAsync(CharacterData character)
    {
        character.LastPlayed = DateTime.UtcNow;
        var path = Path.Combine(_charactersPath, $"{character.Id.Value}.json");
        var json = JsonSerializer.Serialize(character, JsonConfig.Pretty);
        await File.WriteAllTextAsync(path, json);
    }
}
