using RealmOfReality.Server.Config;
using RealmOfReality.Server.Data;
using RealmOfReality.Server.Gumps;
using RealmOfReality.Server.Network;
using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Entities;
using RealmOfReality.Shared.Gumps;
using RealmOfReality.Shared.Items;
using RealmOfReality.Shared.Network;
using RealmOfReality.Shared.Skills;

namespace RealmOfReality.Server.Game;

/// <summary>
/// Handles incoming packets from clients
/// </summary>
public class PacketHandler
{
    private readonly ILogger _logger;
    private readonly ServerConfig _config;
    private readonly DataStore _dataStore;
    private readonly WorldManager _world;
    private readonly GameServer _server;
    
    public PacketHandler(
        ILogger logger,
        ServerConfig config,
        DataStore dataStore,
        WorldManager world,
        GameServer server)
    {
        _logger = logger;
        _config = config;
        _dataStore = dataStore;
        _world = world;
        _server = server;
        
        // Subscribe to paperdoll button events
        PaperdollGump.ButtonClicked += OnPaperdollButtonClicked;
        
        // Subscribe to logout confirmation
        LogoutGump.LogoutConfirmed += OnLogoutConfirmed;
        
        // Subscribe to paperdoll equipment click events
        PaperdollGump.EquipmentClicked += OnEquipmentClicked;
    }
    
    /// <summary>
    /// Handle equipment layer click on paperdoll (for unequip)
    /// </summary>
    private async void OnEquipmentClicked(PlayerEntity player, Shared.Items.Layer layer)
    {
        try
        {
            var conn = _world.GetPlayerConnection(player.Id);
            if (conn == null) return;
            
            var item = player.Equipment.GetItem(layer);
            if (item == null) return;
            
            // Unequip the item
            var packet = new ItemUnequipPacket
            {
                ItemSerial = item.Id.Value,
                Layer = (byte)layer
            };
            
            await HandleItemUnequipAsync(conn, packet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling equipment click");
        }
    }
    
    /// <summary>
    /// Handle logout confirmation
    /// </summary>
    private async void OnLogoutConfirmed(PlayerEntity? player)
    {
        if (player == null) return;
        
        try
        {
            var conn = _world.GetPlayerConnection(player.Id);
            if (conn != null)
            {
                await _world.SendSystemMessageAsync(conn, "Goodbye!", Color.Yellow);
                // Disconnect after a short delay
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    conn.Disconnect();
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling logout");
        }
    }
    
    /// <summary>
    /// Handle paperdoll button clicks
    /// </summary>
    private async void OnPaperdollButtonClicked(PlayerEntity player, PaperdollAction action)
    {
        try
        {
            var conn = _world.GetPlayerConnection(player.Id);
            
            switch (action)
            {
                case PaperdollAction.Status:
                    await _world.OpenStatusGumpAsync(player);
                    break;
                    
                case PaperdollAction.Help:
                    var helpGump = new HelpGump();
                    await _world.SendGumpAsync(player, helpGump);
                    break;
                    
                case PaperdollAction.Skills:
                    var skillsGump = new SkillsGump(player);
                    await _world.SendGumpAsync(player, skillsGump);
                    break;
                    
                case PaperdollAction.Options:
                    var optionsGump = new OptionsGump();
                    await _world.SendGumpAsync(player, optionsGump);
                    break;
                    
                case PaperdollAction.Quests:
                    var questsGump = new QuestsGump(player);
                    await _world.SendGumpAsync(player, questsGump);
                    break;
                    
                case PaperdollAction.Guild:
                    var guildGump = new GuildGump(player);
                    await _world.SendGumpAsync(player, guildGump);
                    break;
                    
                case PaperdollAction.Logout:
                    var logoutGump = new LogoutGump();
                    await _world.SendGumpAsync(player, logoutGump);
                    break;
                    
                case PaperdollAction.TogglePeaceWar:
                    player.Flags ^= EntityFlags.WarMode;
                    var warStatus = player.Flags.HasFlag(EntityFlags.WarMode) ? "War" : "Peace";
                    if (conn != null)
                    {
                        await _world.SendSystemMessageAsync(conn, $"You are now in {warStatus} mode.", Color.Yellow);
                    }
                    // Re-open paperdoll to show updated button
                    await _world.OpenPaperdollAsync(player);
                    break;
                    
                case PaperdollAction.Profile:
                    if (conn != null)
                    {
                        await _world.SendSystemMessageAsync(conn, "Profile feature coming soon!", Color.Gray);
                    }
                    break;
                    
                case PaperdollAction.Virtue:
                    if (conn != null)
                    {
                        await _world.SendSystemMessageAsync(conn, "Virtue system coming soon!", Color.Gray);
                    }
                    break;
                    
                case PaperdollAction.CombatBook:
                    if (conn != null)
                    {
                        await _world.SendSystemMessageAsync(conn, "Combat abilities coming soon!", Color.Gray);
                    }
                    break;
                    
                case PaperdollAction.RacialBook:
                    if (conn != null)
                    {
                        await _world.SendSystemMessageAsync(conn, "Racial abilities coming soon!", Color.Gray);
                    }
                    break;
                    
                default:
                    _logger.LogDebug("Unhandled paperdoll action: {Action}", action);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling paperdoll button click");
        }
    }
    
    /// <summary>
    /// Handle an incoming packet
    /// </summary>
    public async Task HandlePacketAsync(ClientConnection client, Packet packet)
    {
        _logger.LogDebug("Received {Opcode} from client {Id}", packet.Opcode, client.ConnectionId);
        
        switch (packet)
        {
            case PingPacket ping:
                await HandlePingAsync(client, ping);
                break;
            
            case LoginRequestPacket login:
                await HandleLoginAsync(client, login);
                break;
            
            case CharacterListRequestPacket:
                await HandleCharacterListRequestAsync(client);
                break;
            
            case CreateCharacterRequestPacket create:
                await HandleCreateCharacterAsync(client, create);
                break;
            
            case SelectCharacterRequestPacket select:
                await HandleSelectCharacterAsync(client, select);
                break;
            
            case MoveRequestPacket move:
                await HandleMoveRequestAsync(client, move);
                break;
            
            case ChatMessagePacket chat:
                await HandleChatMessageAsync(client, chat);
                break;
            
            case AttackRequestPacket attack:
                await HandleAttackRequestAsync(client, attack);
                break;
            
            case CastSpellRequestPacket spell:
                await HandleCastSpellAsync(client, spell);
                break;
            
            case AdminSpawnNpcPacket spawn:
                await HandleAdminSpawnNpcAsync(client, spawn);
                break;
            
            case AdminKillPacket kill:
                await HandleAdminKillAsync(client, kill);
                break;
            
            case AdminHealPacket heal:
                await HandleAdminHealAsync(client);
                break;
            
            case AdminTeleportPacket teleport:
                await HandleAdminTeleportAsync(client, teleport);
                break;
            
            case GumpResponsePacket gumpResponse:
                await HandleGumpResponseAsync(client, gumpResponse);
                break;
            
            case ItemUsePacket itemUse:
                await HandleItemUseAsync(client, itemUse);
                break;
            
            case ItemEquipPacket itemEquip:
                await HandleItemEquipAsync(client, itemEquip);
                break;
            
            case ItemUnequipPacket itemUnequip:
                await HandleItemUnequipAsync(client, itemUnequip);
                break;
            
            default:
                _logger.LogWarning("Unhandled packet type: {Opcode}", packet.Opcode);
                break;
        }
    }
    
    private async Task HandleAdminSpawnNpcAsync(ClientConnection client, AdminSpawnNpcPacket spawn)
    {
        if (!client.PlayerEntityId.HasValue) return;
        
        var player = _world.GetPlayer(client.PlayerEntityId.Value);
        if (player == null) return;
        
        // Check access level - only GM+ can spawn NPCs
        if (player.AccessLevel < AccessLevel.GameMaster)
        {
            await _world.SendSystemMessageAsync(client, "Access denied - GM+ only", Color.Red);
            return;
        }
        
        var npc = _world.SpawnNpc(spawn.Name, (ushort)spawn.TypeId, spawn.Position, 1, spawn.Level);
        _logger.LogInformation("[{AccessLevel}] {Player} spawned {NpcName} at {Position}", 
            player.AccessLevel, player.Name, spawn.Name, spawn.Position);
        
        await _world.SendSystemMessageAsync(client, $"Spawned {spawn.Name}", new Color(144, 238, 144));
    }
    
    private async Task HandleAdminKillAsync(ClientConnection client, AdminKillPacket kill)
    {
        if (!client.PlayerEntityId.HasValue) return;
        
        var player = _world.GetPlayer(client.PlayerEntityId.Value);
        if (player == null) return;
        
        // Check access level - only GM+ can kill
        if (player.AccessLevel < AccessLevel.GameMaster)
        {
            await _world.SendSystemMessageAsync(client, "Access denied - GM+ only", Color.Red);
            return;
        }
        
        var target = _world.GetEntity(kill.TargetId) as Mobile;
        if (target == null) return;
        
        // Can't kill invulnerable players (other staff)
        if (target is PlayerEntity targetPlayer && targetPlayer.IsStaff)
        {
            await _world.SendSystemMessageAsync(client, "Cannot kill staff members", Color.Yellow);
            return;
        }
        
        target.Health = 0;
        await HandleEntityDeathAsync(target, player);
        
        _logger.LogInformation("[{AccessLevel}] {Player} killed {Target}", 
            player.AccessLevel, player.Name, target.Name);
        await _world.SendSystemMessageAsync(client, $"Killed {target.Name}", new Color(144, 238, 144));
    }
    
    private async Task HandleAdminHealAsync(ClientConnection client)
    {
        if (!client.PlayerEntityId.HasValue) return;
        
        var player = _world.GetPlayer(client.PlayerEntityId.Value);
        if (player == null) return;
        
        // Check access level - Counselor+ can heal self
        if (player.AccessLevel < AccessLevel.Counselor)
        {
            await _world.SendSystemMessageAsync(client, "Access denied - Counselor+ only", Color.Red);
            return;
        }
        
        player.Health = player.MaxHealth;
        player.Mana = player.MaxMana;
        player.Stamina = player.MaxStamina;
        
        _logger.LogInformation("[{AccessLevel}] {Player} healed self", player.AccessLevel, player.Name);
        await _world.SendSystemMessageAsync(client, "Fully healed!", new Color(144, 238, 144));
    }
    
    private async Task HandleAdminTeleportAsync(ClientConnection client, AdminTeleportPacket teleport)
    {
        if (!client.PlayerEntityId.HasValue) return;
        
        var player = _world.GetPlayer(client.PlayerEntityId.Value);
        if (player == null) return;
        
        // Check access level - Counselor+ can teleport
        if (player.AccessLevel < AccessLevel.Counselor)
        {
            await _world.SendSystemMessageAsync(client, "Access denied - Counselor+ only", Color.Red);
            return;
        }
        
        // Teleport the player
        player.Position = teleport.Position;
        
        _logger.LogInformation("[{AccessLevel}] {Player} teleported to ({X}, {Y}, {Z})", 
            player.AccessLevel, player.Name, 
            teleport.Position.X, teleport.Position.Y, teleport.Position.Z);
        
        // Notify client of new position
        await _world.BroadcastEntityMoveAsync(player);
        await _world.SendSystemMessageAsync(client, 
            $"Teleported to ({teleport.Position.X:F0}, {teleport.Position.Y:F0})", 
            new Color(0, 255, 255));
    }
    
    private async Task HandlePingAsync(ClientConnection client, PingPacket ping)
    {
        var pong = new PongPacket
        {
            ClientTime = ping.ClientTime,
            ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await client.SendAsync(pong);
    }
    
    private async Task HandleLoginAsync(ClientConnection client, LoginRequestPacket login)
    {
        _logger.LogInformation("Login attempt from {Username}", login.Username);
        
        // Validate client version
        if (string.IsNullOrEmpty(login.ClientVersion))
        {
            await client.SendAsync(new LoginResponsePacket
            {
                Result = ResultCode.Failed,
                Message = "Invalid client version"
            });
            return;
        }
        
        // Authenticate
        var account = _dataStore.ValidateLogin(login.Username, login.PasswordHash);
        
        if (account == null)
        {
            _logger.LogWarning("Failed login attempt for {Username}", login.Username);
            await client.SendAsync(new LoginResponsePacket
            {
                Result = ResultCode.InvalidCredentials,
                Message = "Invalid username or password"
            });
            return;
        }
        
        // Success
        client.AccountId = account.Id;
        client.State = ClientState.CharacterSelect;
        
        _logger.LogInformation("User {Username} logged in (Account: {AccountId})", 
            login.Username, account.Id);
        
        await client.SendAsync(new LoginResponsePacket
        {
            Result = ResultCode.Success,
            AccountId = account.Id,
            Message = $"Welcome back, {login.Username}!"
        });
        
        // Automatically send character list
        await HandleCharacterListRequestAsync(client);
    }
    
    private async Task HandleCharacterListRequestAsync(ClientConnection client)
    {
        if (client.AccountId == null)
        {
            _logger.LogWarning("Character list request without login from {Id}", client.ConnectionId);
            return;
        }
        
        var characters = _dataStore.GetCharactersForAccount(client.AccountId.Value);
        
        var packet = new CharacterListPacket
        {
            MaxCharacters = (byte)_config.GameRules.MaxCharactersPerAccount,
            Characters = characters.Select(c => new CharacterListEntry
            {
                Id = c.Id,
                Name = c.Name,
                Level = (byte)c.Level,
                Location = $"Map {c.MapId}"
            }).ToList()
        };
        
        await client.SendAsync(packet);
    }
    
    private async Task HandleCreateCharacterAsync(ClientConnection client, CreateCharacterRequestPacket create)
    {
        if (client.AccountId == null)
        {
            _logger.LogWarning("Create character request without login from {Id}", client.ConnectionId);
            return;
        }
        
        // Validate name
        if (string.IsNullOrWhiteSpace(create.Name) || create.Name.Length < 3 || create.Name.Length > 20)
        {
            await client.SendAsync(new CreateCharacterResponsePacket
            {
                Result = ResultCode.InvalidName,
                Message = "Name must be between 3 and 20 characters"
            });
            return;
        }
        
        if (!create.Name.All(c => char.IsLetterOrDigit(c) || c == ' '))
        {
            await client.SendAsync(new CreateCharacterResponsePacket
            {
                Result = ResultCode.InvalidName,
                Message = "Name can only contain letters, numbers, and spaces"
            });
            return;
        }
        
        // Check name availability
        if (!_dataStore.IsCharacterNameAvailable(create.Name))
        {
            await client.SendAsync(new CreateCharacterResponsePacket
            {
                Result = ResultCode.CharacterNameTaken,
                Message = "That name is already taken"
            });
            return;
        }
        
        // Validate stats
        var statTotal = create.Strength + create.Dexterity + create.Intelligence;
        if (statTotal != _config.GameRules.StartingStatTotal)
        {
            await client.SendAsync(new CreateCharacterResponsePacket
            {
                Result = ResultCode.Failed,
                Message = $"Stats must total {_config.GameRules.StartingStatTotal}"
            });
            return;
        }
        
        // Check max characters
        var existingChars = _dataStore.GetCharactersForAccount(client.AccountId.Value);
        if (existingChars.Count >= _config.GameRules.MaxCharactersPerAccount)
        {
            await client.SendAsync(new CreateCharacterResponsePacket
            {
                Result = ResultCode.Failed,
                Message = "Maximum characters reached"
            });
            return;
        }
        
        // Create character
        var template = new CharacterData
        {
            Gender = create.Gender,
            BodyType = create.BodyType,
            SkinHue = create.SkinHue,
            HairStyle = create.HairStyle,
            HairHue = create.HairHue,
            Strength = create.Strength,
            Dexterity = create.Dexterity,
            Intelligence = create.Intelligence,
            MapId = _config.GameRules.StartingMapId,
            X = _config.GameRules.StartingX,
            Y = _config.GameRules.StartingY,
            Z = 0,
            MaxHealth = 50 + create.Strength * 2,
            MaxMana = 50 + create.Intelligence * 2,
            MaxStamina = 50 + create.Dexterity * 2
        };
        template.Health = template.MaxHealth;
        template.Mana = template.MaxMana;
        template.Stamina = template.MaxStamina;
        
        var character = _dataStore.CreateCharacter(client.AccountId.Value, create.Name, template);
        
        if (character == null)
        {
            await client.SendAsync(new CreateCharacterResponsePacket
            {
                Result = ResultCode.Failed,
                Message = "Failed to create character"
            });
            return;
        }
        
        _logger.LogInformation("Character created: {Name} (Account: {AccountId})", 
            character.Name, client.AccountId);
        
        await _dataStore.SaveCharacterAsync(character);
        
        await client.SendAsync(new CreateCharacterResponsePacket
        {
            Result = ResultCode.Success,
            CharacterId = character.Id,
            Message = $"Welcome to the Realm, {character.Name}!"
        });
        
        // Send updated character list
        await HandleCharacterListRequestAsync(client);
    }
    
    private async Task HandleSelectCharacterAsync(ClientConnection client, SelectCharacterRequestPacket select)
    {
        if (client.AccountId == null)
        {
            _logger.LogWarning("Select character request without login from {Id}", client.ConnectionId);
            return;
        }
        
        var character = _dataStore.GetCharacter(select.CharacterId);
        
        if (character == null || character.AccountId != client.AccountId.Value)
        {
            await _world.SendSystemMessageAsync(client, "Character not found", Color.Red);
            return;
        }
        
        // Get account's access level
        var account = _dataStore.GetAccount(client.AccountId.Value);
        var accessLevel = account?.AccessLevel ?? AccessLevel.Player;
        
        client.CharacterId = character.Id;
        client.State = ClientState.EnteringWorld;
        
        // Spawn player entity with their access level
        var player = _world.SpawnPlayer(character, client, accessLevel);
        client.PlayerEntityId = player.Id;
        client.State = ClientState.InWorld;
        
        // Send enter world packet
        await client.SendAsync(new EnterWorldPacket
        {
            PlayerEntityId = player.Id,
            Position = player.Position,
            Facing = player.Facing,
            MapId = player.MapId,
            ServerTick = _world.GameTime.TickCount,
            AccessLevel = (byte)player.AccessLevel
        });
        
        // Notify nearby players
        await _world.BroadcastEntitySpawnAsync(player);
        
        // Send nearby entities to the new player
        foreach (var entity in _world.GetEntitiesInRange(player.Position, _config.GameRules.VisibilityRange))
        {
            if (entity.Id == player.Id) continue;
            
            var health = entity is Mobile m ? m.Health : 100;
            var maxHealth = entity is Mobile m2 ? m2.MaxHealth : 100;
            var level = entity is Mobile m3 ? m3.Level : 1;
            
            await client.SendAsync(new EntitySpawnPacket
            {
                EntityId = entity.Id,
                EntityType = entity.TypeId,
                Position = entity.Position,
                Facing = entity.Facing,
                Name = entity.Name,
                BodyHue = entity.Hue,
                Flags = (byte)entity.Flags,
                Health = health,
                MaxHealth = maxHealth,
                Level = level
            });
        }
        
        await _world.SendSystemMessageAsync(client, 
            $"Welcome to {_config.ServerName}! {_world.OnlinePlayerCount} player(s) online.", 
            Color.Green);
        
        _logger.LogInformation("Player {Name} entered the world", character.Name);
    }
    
    private async Task HandleMoveRequestAsync(ClientConnection client, MoveRequestPacket move)
    {
        if (client.PlayerEntityId == null)
            return;
        
        var player = _world.GetPlayer(client.PlayerEntityId.Value);
        if (player == null)
            return;
        
        var success = _world.TryMovePlayer(player, move.Direction, move.Running);
        
        // Always send confirmation/correction
        await client.SendAsync(new MoveConfirmPacket
        {
            SequenceNumber = move.SequenceNumber,
            Position = player.Position
        });
        
        if (success)
        {
            // Broadcast movement to nearby players
            await _world.BroadcastEntityMoveAsync(player, (byte)(move.Running ? 1 : 0));
        }
    }
    
    private async Task HandleChatMessageAsync(ClientConnection client, ChatMessagePacket chat)
    {
        if (client.PlayerEntityId == null)
            return;
        
        var player = _world.GetPlayer(client.PlayerEntityId.Value);
        if (player == null)
            return;
        
        // Validate message
        if (string.IsNullOrWhiteSpace(chat.Message) || chat.Message.Length > 200)
            return;
        
        // Check for commands
        if (chat.Message.StartsWith("/"))
        {
            await HandleCommandAsync(client, player, chat.Message);
            return;
        }
        
        _logger.LogInformation("[{Channel}] {Name}: {Message}", 
            chat.Channel, player.Name, chat.Message);
        
        await _world.BroadcastChatAsync(player, chat.Channel, chat.Message);
    }
    
    private async Task HandleCommandAsync(ClientConnection client, PlayerEntity player, string message)
    {
        var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLower();
        var args = parts.Skip(1).ToArray();
        
        _logger.LogInformation("Command from {Name}: {Command}", player.Name, message);
        
        switch (command)
        {
            // ===== GUMP TEST COMMANDS =====
            
            case "/gump":
            case "/testgump":
                await ShowTestGumpMenuAsync(client, player);
                break;
            
            case "/confirm":
                await ShowConfirmGumpAsync(client, player, args);
                break;
            
            case "/input":
                await ShowInputGumpAsync(client, player);
                break;
            
            case "/menu":
                await ShowMenuGumpAsync(client, player);
                break;
            
            case "/dialog":
            case "/talk":
                await ShowDialogGumpAsync(client, player);
                break;
            
            case "/vendor":
            case "/shop":
                await ShowVendorGumpAsync(client, player);
                break;
            
            case "/settings":
            case "/options":
                await ShowSettingsGumpAsync(client, player);
                break;
            
            case "/stats":
                await ShowStatsGumpAsync(client, player);
                break;
            
            case "/paperdoll":
            case "/pd":
                await ShowPaperdollGumpAsync(client, player);
                break;
            
            case "/backpack":
            case "/bag":
            case "/inventory":
                await ShowBackpackGumpAsync(client, player);
                break;
            
            // ===== ADMIN COMMANDS =====
            
            case "/spawn":
                if (args.Length >= 1)
                {
                    var typeArg = args[0].ToLower();
                    ushort typeId = typeArg switch
                    {
                        "goblin" => 10,
                        "skeleton" => 20,
                        "wolf" => 30,
                        "healer" => 40,
                        "dragon" => 100,
                        _ => ushort.TryParse(typeArg, out var id) ? id : (ushort)10
                    };
                    
                    var npc = _world.SpawnNpc(typeArg, typeId, player.Position, player.MapId);
                    await _world.SendSystemMessageAsync(client, $"Spawned {npc.Name} (ID: {npc.Id.Value})", Color.LightGreen);
                }
                else
                {
                    await _world.SendSystemMessageAsync(client, "Usage: /spawn <type>", Color.Yellow);
                }
                break;
            
            case "/kill":
                // Kill target or self
                await _world.SendSystemMessageAsync(client, "Use attack mode to kill targets", Color.Yellow);
                break;
            
            case "/heal":
                player.Health = player.MaxHealth;
                player.Mana = player.MaxMana;
                await _world.SendSystemMessageAsync(client, "Healed!", Color.LightGreen);
                break;
            
            case "/use":
                // Use an item from inventory by slot number
                if (args.Length >= 1 && int.TryParse(args[0], out int useSlot))
                {
                    await UseItemAsync(client, player, useSlot);
                }
                else
                {
                    await _world.SendSystemMessageAsync(client, "Usage: /use <slot>", Color.Yellow);
                }
                break;
            
            case "/teleport":
            case "/tp":
                if (args.Length >= 2 && float.TryParse(args[0], out var x) && float.TryParse(args[1], out var y))
                {
                    player.Position = new WorldPosition(x, y);
                    await _world.SendSystemMessageAsync(client, $"Teleported to {x}, {y}", Color.LightGreen);
                }
                else
                {
                    await _world.SendSystemMessageAsync(client, "Usage: /teleport <x> <y>", Color.Yellow);
                }
                break;
            
            case "/help":
                await ShowHelpAsync(client);
                break;
            
            default:
                await _world.SendSystemMessageAsync(client, $"Unknown command: {command}. Type /help for commands.", Color.Red);
                break;
        }
    }
    
    private async Task ShowHelpAsync(ClientConnection client)
    {
        await _world.SendSystemMessageAsync(client, "=== Commands ===", Color.Yellow);
        await _world.SendSystemMessageAsync(client, "/gump - Test gump menu", Color.White);
        await _world.SendSystemMessageAsync(client, "/confirm - Confirm dialog", Color.White);
        await _world.SendSystemMessageAsync(client, "/input - Text input dialog", Color.White);
        await _world.SendSystemMessageAsync(client, "/menu - Multi-page menu", Color.White);
        await _world.SendSystemMessageAsync(client, "/dialog - NPC dialog", Color.White);
        await _world.SendSystemMessageAsync(client, "/vendor - Vendor shop", Color.White);
        await _world.SendSystemMessageAsync(client, "/settings - Settings gump", Color.White);
        await _world.SendSystemMessageAsync(client, "/stats - Character stats", Color.White);
        await _world.SendSystemMessageAsync(client, "/spawn <type> - Spawn NPC", Color.White);
        await _world.SendSystemMessageAsync(client, "/heal - Heal yourself", Color.White);
        await _world.SendSystemMessageAsync(client, "/tp <x> <y> - Teleport", Color.White);
    }
    
    // ===== GUMP TEST METHODS =====
    
    private async Task ShowTestGumpMenuAsync(ClientConnection client, PlayerEntity player)
    {
        var options = new List<(string text, int id)>
        {
            ("Confirm Dialog", 1),
            ("Text Input", 2),
            ("Multi-Page Menu", 3),
            ("NPC Dialog", 4),
            ("Vendor Shop", 5),
            ("Settings", 6),
            ("Character Stats", 7),
            ("Paperdoll", 8),
            ("Backpack", 9),
        };
        
        var gump = new MenuGump("Gump Test Menu", options, async buttonId =>
        {
            switch (buttonId)
            {
                case 1: await ShowConfirmGumpAsync(client, player, Array.Empty<string>()); break;
                case 2: await ShowInputGumpAsync(client, player); break;
                case 3: await ShowMenuGumpAsync(client, player); break;
                case 4: await ShowDialogGumpAsync(client, player); break;
                case 5: await ShowVendorGumpAsync(client, player); break;
                case 6: await ShowSettingsGumpAsync(client, player); break;
                case 7: await ShowStatsGumpAsync(client, player); break;
                case 8: await ShowPaperdollGumpAsync(client, player); break;
                case 9: await ShowBackpackGumpAsync(client, player); break;
            }
        });
        
        await SendGumpAsync(client, player, gump);
    }
    
    private async Task ShowConfirmGumpAsync(ClientConnection client, PlayerEntity player, string[] args)
    {
        var message = args.Length > 0 ? string.Join(" ", args) : "Do you want to proceed with this action?";
        
        var gump = new ConfirmGump("Confirmation", message, async confirmed =>
        {
            if (confirmed)
            {
                await _world.SendSystemMessageAsync(client, "You clicked YES!", Color.LightGreen);
            }
            else
            {
                await _world.SendSystemMessageAsync(client, "You clicked NO!", Color.Red);
            }
        });
        
        await SendGumpAsync(client, player, gump);
    }
    
    private async Task ShowInputGumpAsync(ClientConnection client, PlayerEntity player)
    {
        var gump = new TextInputGump("Enter Text", "What is your favorite color?", "", async text =>
        {
            if (text != null)
            {
                await _world.SendSystemMessageAsync(client, $"You entered: {text}", Color.LightGreen);
            }
            else
            {
                await _world.SendSystemMessageAsync(client, "Input cancelled.", Color.Yellow);
            }
        });
        
        await SendGumpAsync(client, player, gump);
    }
    
    private async Task ShowMenuGumpAsync(ClientConnection client, PlayerEntity player)
    {
        var options = new List<(string text, int id)>();
        for (int i = 1; i <= 20; i++)
        {
            options.Add(($"Option {i} - This is a test menu item", i));
        }
        
        var gump = new MenuGump("Multi-Page Menu", options, async buttonId =>
        {
            await _world.SendSystemMessageAsync(client, $"You selected option {buttonId}", Color.LightGreen);
        });
        
        await SendGumpAsync(client, player, gump);
    }
    
    private async Task ShowDialogGumpAsync(ClientConnection client, PlayerEntity player)
    {
        var responses = new List<(string text, int id)>
        {
            ("Tell me about this place.", 1),
            ("Do you have any quests?", 2),
            ("What items do you sell?", 3),
            ("Goodbye.", 0)
        };
        
        var gump = new DialogGump("Old Man", 
            "Greetings, traveler! Welcome to our humble village. " +
            "I've lived here for many years and know much about the surrounding lands. " +
            "How may I help you today?",
            responses,
            async buttonId =>
            {
                switch (buttonId)
                {
                    case 1:
                        await _world.SendSystemMessageAsync(client, "This is the village of Millbrook, founded 200 years ago...", Color.White);
                        break;
                    case 2:
                        await _world.SendSystemMessageAsync(client, "Indeed! Goblins have been raiding our farms. Can you help?", Color.White);
                        break;
                    case 3:
                        await ShowVendorGumpAsync(client, player);
                        break;
                    default:
                        await _world.SendSystemMessageAsync(client, "Farewell, traveler!", Color.White);
                        break;
                }
            });
        
        await SendGumpAsync(client, player, gump);
    }
    
    private async Task ShowVendorGumpAsync(ClientConnection client, PlayerEntity player)
    {
        var items = new List<VendorGump.VendorItem>
        {
            new() { ItemId = 3573, Name = "Iron Sword", Price = 100, Stock = 5, Hue = 0 },
            new() { ItemId = 3574, Name = "Steel Sword", Price = 250, Stock = 3, Hue = 0 },
            new() { ItemId = 3909, Name = "War Axe", Price = 180, Stock = 4, Hue = 0 },
            new() { ItemId = 5062, Name = "Leather Armor", Price = 150, Stock = 10, Hue = 0 },
            new() { ItemId = 5135, Name = "Health Potion", Price = 50, Stock = 20, Hue = 38 },
            new() { ItemId = 5136, Name = "Mana Potion", Price = 75, Stock = 15, Hue = 88 },
            new() { ItemId = 3834, Name = "Torch", Price = 10, Stock = 50, Hue = 0 },
            new() { ItemId = 3643, Name = "Bandages", Price = 5, Stock = 100, Hue = 0 },
        };
        
        var gump = new VendorGump("Merchant", items, async (itemIndex, quantity) =>
        {
            var item = items[itemIndex];
            await _world.SendSystemMessageAsync(client, 
                $"You bought {item.Name} for {item.Price}gp!", Color.LightGreen);
        });
        
        await SendGumpAsync(client, player, gump);
    }
    
    private async Task ShowSettingsGumpAsync(ClientConnection client, PlayerEntity player)
    {
        var currentSettings = new SettingsGump.Settings
        {
            SoundEnabled = true,
            MusicEnabled = true,
            ShowNames = true,
            ShowHealthBars = true,
            Difficulty = 1
        };
        
        var gump = new SettingsGump(currentSettings, async newSettings =>
        {
            await _world.SendSystemMessageAsync(client, 
                $"Settings saved! Sound={newSettings.SoundEnabled}, Music={newSettings.MusicEnabled}, " +
                $"Names={newSettings.ShowNames}, HealthBars={newSettings.ShowHealthBars}, " +
                $"Difficulty={(newSettings.Difficulty == 0 ? "Easy" : newSettings.Difficulty == 1 ? "Normal" : "Hard")}", 
                Color.LightGreen);
        });
        
        await SendGumpAsync(client, player, gump);
    }
    
    private async Task ShowStatsGumpAsync(ClientConnection client, PlayerEntity player)
    {
        var gump = new CharacterStatsGump(
            player.Name,
            player.Strength,
            player.Dexterity,
            player.Intelligence,
            player.Health,
            player.MaxHealth,
            player.Mana,
            player.MaxMana
        );
        
        await SendGumpAsync(client, player, gump);
    }
    
    private async Task ShowPaperdollGumpAsync(ClientConnection client, PlayerEntity player)
    {
        // Ensure player has starting equipment
        EnsureStartingEquipment(player);
        
        var gump = new PaperdollGump(
            player,
            isSelf: true,
            isWarMode: false
        );
        
        await SendGumpAsync(client, player, gump);
    }
    
    private async Task ShowBackpackGumpAsync(ClientConnection client, PlayerEntity player)
    {
        // Ensure player has starting inventory
        EnsureStartingInventory(player);
        
        // Create backpack from real inventory
        var gump = BackpackGump.FromPlayerInventory(player);
        
        await SendGumpAsync(client, player, gump);
    }
    
    /// <summary>
    /// Ensure player has starting equipment (shirt, pants, sandals, hair, beard)
    /// </summary>
    private void EnsureStartingEquipment(PlayerEntity player)
    {
        // Only add if they don't have equipment yet
        if (player.Equipment.GetAllEquipped().Any())
            return;
        
        _logger.LogInformation("Giving starting equipment to {Player}", player.Name);
        
        // Shirt
        var shirt = ItemDatabase.CreateItem(1); // Fancy Shirt
        player.Equipment.TryEquip(shirt, out _);
        
        // Pants
        var pants = ItemDatabase.CreateItem(2); // Long Pants
        player.Equipment.TryEquip(pants, out _);
        
        // Sandals
        var sandals = ItemDatabase.CreateItem(3); // Sandals
        player.Equipment.TryEquip(sandals, out _);
        
        // Hair - set on player entity directly for now
        // In UO, hair/beard are special equipment layers but also stored on mobile
        var hair = ItemDatabase.CreateItem(100); // Long Hair
        player.HairStyle = 1; // Long hair style
        player.HairHue = 0x044E; // Brown
        player.Equipment[Layer.Hair] = hair;
        
        // Beard (for males)
        if (player.Gender == 0) // Male
        {
            var beard = ItemDatabase.CreateItem(101); // Long Beard
            player.BeardStyle = 1; // Long beard
            player.BeardHue = 0x044E; // Brown
            player.Equipment[Layer.FacialHair] = beard;
        }
    }
    
    /// <summary>
    /// Ensure player has starting inventory (scroll of fireball, greater healing potion)
    /// </summary>
    private void EnsureStartingInventory(PlayerEntity player)
    {
        // Only add if inventory is empty
        if (player.Inventory.Count > 0)
            return;
        
        _logger.LogInformation("Giving starting inventory to {Player}", player.Name);
        
        // Scroll of Fireball x3
        var fireballScroll = ItemDatabase.CreateItem(210, 3);
        player.Inventory.TryAdd(fireballScroll, out _);
        
        // Greater Healing Potion x5
        var healPotion = ItemDatabase.CreateItem(200, 5);
        player.Inventory.TryAdd(healPotion, out _);
        
        // Some bandages
        var bandages = ItemDatabase.CreateItem(300, 20);
        player.Inventory.TryAdd(bandages, out _);
        
        // A bit of gold
        var gold = ItemDatabase.CreateItem(301, 100);
        player.Inventory.TryAdd(gold, out _);
    }
    
    /// <summary>
    /// Use an item from inventory
    /// </summary>
    private async Task UseItemAsync(ClientConnection client, PlayerEntity player, int slot)
    {
        var item = player.Inventory[slot];
        if (item == null)
        {
            await _world.SendSystemMessageAsync(client, $"No item in slot {slot}", Color.Red);
            return;
        }
        
        if (item.Definition == null)
        {
            await _world.SendSystemMessageAsync(client, "Unknown item", Color.Red);
            return;
        }
        
        var def = item.Definition;
        
        // Check if usable
        if (!def.Flags.HasFlag(ItemFlags.Usable))
        {
            await _world.SendSystemMessageAsync(client, $"{def.Name} cannot be used", Color.Yellow);
            return;
        }
        
        // Handle based on item type
        string message;
        
        // Healing items
        if (def.HealAmount > 0)
        {
            if (player.Health >= player.MaxHealth)
            {
                await _world.SendSystemMessageAsync(client, "You are already at full health", Color.Yellow);
                return;
            }
            
            var healed = player.Heal(def.HealAmount);
            ConsumeItem(player, item, slot);
            message = $"You use {def.Name} and recover {healed} health. ({player.Health}/{player.MaxHealth})";
            await _world.SendSystemMessageAsync(client, message, Color.LightGreen);
            return;
        }
        
        // Spell scrolls
        if (!string.IsNullOrEmpty(def.SpellEffect))
        {
            var random = new Random();
            int damage = random.Next(def.MinDamage, def.MaxDamage + 1);
            ConsumeItem(player, item, slot);
            
            // Play effect message
            message = $"You read the {def.Name} and cast {def.SpellEffect}! ({damage} damage)";
            await _world.SendSystemMessageAsync(client, message, Color.Orange);
            
            // TODO: Actually apply damage to target
            return;
        }
        
        await _world.SendSystemMessageAsync(client, $"Used {def.Name}", Color.White);
    }
    
    private void ConsumeItem(PlayerEntity player, Item item, int slot)
    {
        if (item.Amount > 1)
        {
            item.Amount--;
        }
        else
        {
            player.Inventory.Remove(slot);
        }
    }
    
    private async Task SendGumpAsync(ClientConnection client, PlayerEntity player, Gump gump)
    {
        // Register gump for response tracking
        _world.GumpManager.RegisterGump(player.Id.Value, gump);
        
        // Build and send packet
        var data = gump.BuildData();
        
        _logger.LogInformation("[GUMP] Sending gump {TypeId} with {Elements} elements and {Texts} texts to {Player}", 
            data.GumpTypeId, data.Elements.Count, data.Texts.Count, player.Name);
        
        var packet = new GumpOpenPacket
        {
            GumpData = data
        };
        
        await client.SendAsync(packet);
        _logger.LogDebug("Sent gump {TypeId} (serial {Serial}) to {Player}", 
            gump.TypeId, gump.Serial, player.Name);
    }
    
    private Task HandleGumpResponseAsync(ClientConnection client, GumpResponsePacket packet)
    {
        if (client.PlayerEntityId == null || packet.Response == null)
            return Task.CompletedTask;
        
        var player = _world.GetPlayer(client.PlayerEntityId.Value);
        if (player == null)
            return Task.CompletedTask;
        
        _logger.LogDebug("Gump response from {Player}: TypeId={TypeId}, Serial={Serial}, Button={Button}", 
            player.Name, packet.Response.GumpTypeId, packet.Response.Serial, packet.Response.ButtonId);
        
        // Route response to the registered gump
        _world.GumpManager.HandleResponse(player.Id.Value, packet.Response, player);
        return Task.CompletedTask;
    }
    
    private async Task HandleAttackRequestAsync(ClientConnection client, AttackRequestPacket attack)
    {
        if (client.PlayerEntityId == null) return;
        
        var player = _world.GetPlayer(client.PlayerEntityId.Value);
        if (player == null || player.Health <= 0) return;
        
        var target = _world.GetEntity(attack.TargetId);
        if (target == null) return;
        
        // Check range (melee = 2 tiles)
        var distance = player.Position.DistanceTo(target.Position);
        if (distance > 2.5f)
        {
            await _world.SendSystemMessageAsync(client, "Too far away!", Color.Red);
            return;
        }
        
        // Calculate damage based on player stats
        var baseDamage = 5 + player.Strength / 5;
        var variance = new Random().Next(-3, 4);
        var isCritical = new Random().Next(100) < 10; // 10% crit chance
        var damage = Math.Max(1, baseDamage + variance);
        if (isCritical) damage = (int)(damage * 1.5);
        
        // Apply damage
        if (target is Mobile mobile)
        {
            mobile.Health = Math.Max(0, mobile.Health - damage);
            
            // Broadcast damage
            var damagePacket = new DamageDealtPacket
            {
                AttackerId = player.Id,
                TargetId = target.Id,
                Damage = damage,
                DamageType = RealmOfReality.Shared.Network.DamageType.Physical,
                IsCritical = isCritical,
                TargetHealth = mobile.Health
            };
            await _world.BroadcastToNearbyAsync(player.Position, damagePacket);
            
            // Check for death
            if (mobile.Health <= 0)
            {
                await HandleEntityDeathAsync(mobile, player);
            }
        }
    }
    
    private async Task HandleCastSpellAsync(ClientConnection client, CastSpellRequestPacket spell)
    {
        if (client.PlayerEntityId == null) return;
        
        var player = _world.GetPlayer(client.PlayerEntityId.Value);
        if (player == null || player.Health <= 0) return;
        
        // Get spell info
        var (manaCost, baseDamage, baseHeal, range, damageType) = GetSpellInfo(spell.SpellId);
        
        // Check mana
        if (player.Mana < manaCost)
        {
            await client.SendAsync(new SpellEffectPacket
            {
                CasterId = player.Id,
                SpellId = spell.SpellId,
                Success = false,
                FailReason = "Not enough mana!"
            });
            return;
        }
        
        // Deduct mana
        player.Mana -= manaCost;
        
        // Get target
        Mobile? target = null;
        if (spell.TargetId.HasValue)
        {
            target = _world.GetEntity(spell.TargetId.Value) as Mobile;
        }
        
        // Calculate effect
        var actualDamage = 0;
        var actualHeal = 0;
        
        if (baseDamage > 0 && target != null)
        {
            // Check range
            if (player.Position.DistanceTo(target.Position) > range)
            {
                await client.SendAsync(new SpellEffectPacket
                {
                    CasterId = player.Id,
                    SpellId = spell.SpellId,
                    Success = false,
                    FailReason = "Out of range!"
                });
                return;
            }
            
            // Calculate damage based on intelligence
            var bonus = player.Intelligence / 10;
            actualDamage = baseDamage + bonus + new Random().Next(-5, 6);
            actualDamage = Math.Max(1, actualDamage);
            
            target.Health = Math.Max(0, target.Health - actualDamage);
            
            // Check death
            if (target.Health <= 0)
            {
                await HandleEntityDeathAsync(target, player);
            }
        }
        
        if (baseHeal > 0)
        {
            // Heal target or self
            var healTarget = target ?? player;
            var bonus = player.Intelligence / 10;
            actualHeal = baseHeal + bonus + new Random().Next(-5, 6);
            healTarget.Health = Math.Min(healTarget.MaxHealth, healTarget.Health + actualHeal);
        }
        
        // Broadcast effect
        var effectPacket = new SpellEffectPacket
        {
            CasterId = player.Id,
            SpellId = spell.SpellId,
            TargetId = target?.Id,
            TargetPosition = spell.TargetPosition ?? target?.Position ?? player.Position,
            Damage = actualDamage,
            Healing = actualHeal,
            Success = true
        };
        await _world.BroadcastToNearbyAsync(player.Position, effectPacket);
        
        _logger.LogDebug("{Player} cast spell {SpellId} for {Damage} damage, {Heal} heal",
            player.Name, spell.SpellId, actualDamage, actualHeal);
    }
    
    private (int manaCost, int baseDamage, int baseHeal, float range, RealmOfReality.Shared.Network.DamageType type) GetSpellInfo(ushort spellId)
    {
        var spell = SpellDefinitions.GetSpell(spellId);
        if (spell == null)
            return (5, 10, 0, 8f, RealmOfReality.Shared.Network.DamageType.Physical);
        
        var baseDamage = spell.Effect switch
        {
            SpellEffect.Damage or SpellEffect.AreaDamage => (spell.MinDamage + spell.MaxDamage) / 2,
            _ => 0
        };
        
        var baseHeal = spell.Effect switch
        {
            SpellEffect.Heal => 10 + spell.Circle * 8,
            _ => 0
        };
        
        var range = spell.Circle switch
        {
            <= 3 => 8f,
            <= 5 => 10f,
            _ => 12f
        };
        
        var damageType = spell.DamageType switch
        {
            RealmOfReality.Shared.Skills.DamageType.Fire => RealmOfReality.Shared.Network.DamageType.Fire,
            RealmOfReality.Shared.Skills.DamageType.Cold => RealmOfReality.Shared.Network.DamageType.Cold,
            RealmOfReality.Shared.Skills.DamageType.Poison => RealmOfReality.Shared.Network.DamageType.Poison,
            RealmOfReality.Shared.Skills.DamageType.Lightning => RealmOfReality.Shared.Network.DamageType.Energy,
            _ => RealmOfReality.Shared.Network.DamageType.Physical
        };
        
        return (spell.ManaCost, baseDamage, baseHeal, range, damageType);
    }
    
    private async Task HandleEntityDeathAsync(Mobile entity, PlayerEntity killer)
    {
        var deathPacket = new DeathPacket
        {
            EntityId = entity.Id,
            KillerId = killer.Id
        };
        await _world.BroadcastToNearbyAsync(entity.Position, deathPacket);
        
        // Grant experience to killer
        var expGain = 50 + entity.Level * 10;
        
        // Notify killer
        var killerConn = _world.GetPlayerConnection(killer.Id);
        if (killerConn != null)
        {
            await _world.SendSystemMessageAsync(killerConn, 
                $"You killed {entity.Name}! +{expGain} XP", Color.Gold);
        }
        
        // If NPC, spawn corpse and schedule respawn
        if (entity is NpcEntity npc)
        {
            _logger.LogInformation("{Killer} killed {Npc}", killer.Name, npc.Name);
            
            // Spawn corpse with loot
            await _world.SpawnCorpseAsync(npc);
            
            // Remove the NPC (it will respawn later)
            await _world.DespawnNpcAsync(npc);
        }
    }
    
    /// <summary>
    /// Handle item use request (double-click on item)
    /// </summary>
    private async Task HandleItemUseAsync(ClientConnection client, ItemUsePacket packet)
    {
        if (!client.PlayerEntityId.HasValue) return;
        
        var player = _world.GetPlayer(client.PlayerEntityId.Value);
        if (player == null) return;
        
        var itemId = new ItemId(packet.ItemSerial);
        
        // Find item in inventory
        Item? item = null;
        int slotIndex = -1;
        foreach (var (slot, invItem) in player.Inventory.GetAllItems())
        {
            if (invItem.Id == itemId)
            {
                item = invItem;
                slotIndex = slot;
                break;
            }
        }
        
        if (item?.Definition == null)
        {
            await _world.SendSystemMessageAsync(client, "Item not found.", Color.Red);
            return;
        }
        
        var def = item.Definition;
        
        // Check if item is consumable
        if (def.Category == ItemCategory.Consumable || def.Flags.HasFlag(ItemFlags.Usable))
        {
            // Apply effects
            bool consumed = false;
            
            if (def.HealAmount > 0)
            {
                player.Heal(def.HealAmount);
                await _world.SendSystemMessageAsync(client, $"You drink the {def.Name} and restore {def.HealAmount} health.", Color.Green);
                consumed = true;
            }
            
            if (def.ManaAmount > 0)
            {
                player.Mana = Math.Min(player.MaxMana, player.Mana + def.ManaAmount);
                await _world.SendSystemMessageAsync(client, $"You drink the {def.Name} and restore {def.ManaAmount} mana.", Color.Blue);
                consumed = true;
            }
            
            if (!string.IsNullOrEmpty(def.SpellEffect))
            {
                await _world.SendSystemMessageAsync(client, $"You read the {def.Name}...", Color.Cyan);
                consumed = true;
            }
            
            // Remove item if consumed
            if (consumed && def.Flags.HasFlag(ItemFlags.Consumable))
            {
                if (item.Amount > 1)
                {
                    item.Amount--;
                }
                else
                {
                    player.Inventory.Remove(slotIndex);
                }
                
                // Send inventory update
                await SendInventoryUpdateAsync(client, player);
            }
        }
        // Check if item is equippable
        else if (def.Layer != Shared.Items.Layer.Invalid)
        {
            // Equip the item
            await EquipItemAsync(client, player, item, slotIndex);
        }
        // Otherwise just show info
        else
        {
            await _world.SendSystemMessageAsync(client, $"{def.Name}: {def.Description}", Color.White);
        }
    }
    
    /// <summary>
    /// Handle equip item request
    /// </summary>
    private async Task HandleItemEquipAsync(ClientConnection client, ItemEquipPacket packet)
    {
        if (!client.PlayerEntityId.HasValue) return;
        
        var player = _world.GetPlayer(client.PlayerEntityId.Value);
        if (player == null) return;
        
        var itemId = new ItemId(packet.ItemSerial);
        
        // Find item in inventory
        Item? item = null;
        int slotIndex = -1;
        foreach (var (slot, invItem) in player.Inventory.GetAllItems())
        {
            if (invItem.Id == itemId)
            {
                item = invItem;
                slotIndex = slot;
                break;
            }
        }
        
        if (item == null)
        {
            await _world.SendSystemMessageAsync(client, "Item not found in inventory.", Color.Red);
            return;
        }
        
        await EquipItemAsync(client, player, item, slotIndex);
    }
    
    /// <summary>
    /// Handle unequip item request
    /// </summary>
    private async Task HandleItemUnequipAsync(ClientConnection client, ItemUnequipPacket packet)
    {
        if (!client.PlayerEntityId.HasValue) return;
        
        var player = _world.GetPlayer(client.PlayerEntityId.Value);
        if (player == null) return;
        
        var layer = (Shared.Items.Layer)packet.Layer;
        
        // Get equipped item
        var item = player.Equipment.GetItem(layer);
        if (item == null)
        {
            await _world.SendSystemMessageAsync(client, "Nothing equipped in that slot.", Color.Red);
            return;
        }
        
        // Check if inventory has space
        if (player.Inventory.FreeSlots == 0)
        {
            await _world.SendSystemMessageAsync(client, "Your backpack is full!", Color.Red);
            return;
        }
        
        // Unequip
        player.Equipment.Unequip(layer);
        
        // Add to inventory
        player.Inventory.TryAdd(item, out _);
        
        await _world.SendSystemMessageAsync(client, $"You unequip {item.Name}.", Color.White);
        
        // Send inventory and paperdoll updates
        await SendInventoryUpdateAsync(client, player);
        await _world.OpenPaperdollAsync(player);
    }
    
    /// <summary>
    /// Equip an item from inventory
    /// </summary>
    private async Task EquipItemAsync(ClientConnection client, PlayerEntity player, Item item, int inventorySlot)
    {
        if (item.Definition == null || item.Definition.Layer == Shared.Items.Layer.Invalid)
        {
            await _world.SendSystemMessageAsync(client, "This item cannot be equipped.", Color.Red);
            return;
        }
        
        var def = item.Definition;
        
        // Check requirements
        if (player.Level < def.RequiredLevel)
        {
            await _world.SendSystemMessageAsync(client, $"You need to be level {def.RequiredLevel} to equip this.", Color.Red);
            return;
        }
        
        if (player.Strength < def.RequiredStrength)
        {
            await _world.SendSystemMessageAsync(client, $"You need {def.RequiredStrength} strength to equip this.", Color.Red);
            return;
        }
        
        // Remove from inventory
        player.Inventory.Remove(inventorySlot);
        
        // Equip (returns any previously equipped item)
        if (player.Equipment.TryEquip(item, out var previousItem))
        {
            await _world.SendSystemMessageAsync(client, $"You equip {def.Name}.", Color.White);
            
            // If there was a previous item, add it back to inventory
            if (previousItem != null)
            {
                if (!player.Inventory.TryAdd(previousItem, out _))
                {
                    // Inventory full - drop on ground (not implemented yet)
                    await _world.SendSystemMessageAsync(client, $"Your {previousItem.Name} falls to the ground!", Color.Yellow);
                }
            }
        }
        else
        {
            // Failed to equip - put back in inventory
            player.Inventory.TryAdd(item, out _);
            await _world.SendSystemMessageAsync(client, "Failed to equip item.", Color.Red);
            return;
        }
        
        // Send inventory and paperdoll updates
        await SendInventoryUpdateAsync(client, player);
        await _world.OpenPaperdollAsync(player);
    }
    
    /// <summary>
    /// Send inventory update to client
    /// </summary>
    private async Task SendInventoryUpdateAsync(ClientConnection client, PlayerEntity player)
    {
        var packet = new InventoryFullPacket();
        
        // Add inventory items
        foreach (var (slot, item) in player.Inventory.GetAllItems())
        {
            if (item.Definition == null) continue;
            
            packet.Items.Add(new InventoryItemData
            {
                Serial = item.Id.Value,
                ItemId = item.Definition.SpriteId,
                Hue = item.Definition.Hue,
                Amount = item.Amount,
                SlotIndex = slot,
                Name = item.Definition.Name
            });
        }
        
        // Add equipment
        foreach (var (layer, item) in player.Equipment.GetAllEquipped())
        {
            if (item.Definition == null) continue;
            
            packet.Equipment.Add(new EquipmentItemData
            {
                Serial = item.Id.Value,
                ItemId = item.Definition.SpriteId,
                GumpId = item.Definition.GumpId,
                Hue = item.Definition.Hue,
                Layer = (byte)layer,
                Name = item.Definition.Name
            });
        }
        
        await client.SendAsync(packet);
    }
    
    /// <summary>
    /// Handle client disconnect
    /// </summary>
    public async Task HandleDisconnectAsync(ClientConnection client)
    {
        if (client.PlayerEntityId != null)
        {
            var player = _world.GetPlayer(client.PlayerEntityId.Value);
            if (player != null)
            {
                // Save character
                var characterData = _dataStore.GetCharacter(client.CharacterId!.Value);
                if (characterData != null)
                {
                    characterData.UpdateFrom(player);
                    await _dataStore.SaveCharacterAsync(characterData);
                }
                
                // Notify other players
                await _world.BroadcastEntityDespawnAsync(player.Id, player.MapId, player.Position);
                
                // Remove from world
                _world.DespawnPlayer(player.Id);
            }
        }
    }
}
