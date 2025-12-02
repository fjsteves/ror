# Realm of Reality

A 2.5D isometric MMORPG game engine inspired by Ultima Online, built with C# and .NET 8.0.

## Overview

Realm of Reality is a modern reimplementation of an Ultima Online-style game client and server. It uses genuine UO data files (maps, art, animations) while providing a clean, well-documented codebase suitable for learning or building your own UO-inspired game.

## Project Structure

```
RealmOfReality/
├── Client/               # MonoGame graphical client
│   ├── Assets/           # UO file loaders (art, maps, animations)
│   ├── Engine/           # Core rendering (WorldRenderer, Camera)
│   ├── Game/             # Game state management
│   ├── Gumps/            # UO-style UI windows
│   ├── Network/          # Client networking
│   └── UI/               # UI screens and panels
├── Server/               # Dedicated game server
├── Shared/               # Common library
│   ├── Core/             # Primitives, math, identifiers
│   ├── Entities/         # Entity definitions
│   ├── Network/          # Packet definitions
│   └── World/            # Map and world structures
└── Tools/                # Development utilities
```

## Requirements

### Server
- .NET 8.0 SDK
- Works on Windows, Linux, macOS

### Client
- .NET 8.0 SDK
- MonoGame 3.8.1+
- Windows 10/11 (DirectX) or Linux (OpenGL)
- **UO Data Files** (see below)

### UO Data Files

The client requires Ultima Online data files to render graphics. You need:

**Required:**
- `map0.mul` or `map0LegacyMUL.uop` - Terrain data
- `art.mul` + `artidx.mul` or `artLegacyMUL.uop` - Tile graphics
- `tiledata.mul` - Tile metadata

**Recommended:**
- `texmaps.mul` + `texidx.mul` - Stretched terrain textures
- `staidx0.mul` + `statics0.mul` - Static objects
- `anim.mul` + `anim.idx` - Character animations
- `gumps.mul` + `gumpidx.mul` - UI graphics
- `hues.mul` - Color palettes

Configure the path in `Client/ClientSettings.cs` or set environment variable `UO_DATA_PATH`.

## Building

### Visual Studio 2022

1. Open `RealmOfReality.sln`
2. Restore NuGet packages
3. Build solution (F6)
4. Set startup project and run (F5)

### Command Line

```bash
# Build everything
dotnet build

# Run server
dotnet run --project Server

# Run client
dotnet run --project Client
```

## Running

### Start Server
```bash
cd RealmOfReality
dotnet run --project Server
```
Server starts on port 7775 by default.

### Start Client
```bash
dotnet run --project Client
```

### Login
- Default test account: `admin` / `admin`
- Create a character and enter the world

## Controls

| Key | Action |
|-----|--------|
| W/A/S/D or Arrows | Move |
| Shift + Move | Run |
| Enter | Open/send chat |
| Escape | Close menus |
| I | Inventory |
| C | Character stats |
| M | World map |
| Mouse Scroll | Zoom |

## Architecture

### Isometric Rendering

The game uses UO's 2:1 dimetric ("isometric") projection:

```
Screen coordinates:
  screenX = (tileX - tileY) × 22
  screenY = (tileX + tileY) × 22 - tileZ × 4

Tile dimensions: 44×44 pixel diamonds
Z scale: 4 pixels per altitude unit
```

### Terrain Rendering

Terrain is rendered as textured 3D quads (not SpriteBatch):

1. Each cell uses 4 corner tiles: NW(x,y), NE(x+1,y), SW(x,y+1), SE(x+1,y+1)
2. Each corner provides its own Z height for smooth terrain
3. Texture lookup: `TileData.TextureId` → `texmaps.mul`
4. Fallback: 44×44 art tile from `art.mul`

### File Formats

| File | Purpose |
|------|---------|
| `map*.mul` | 196-byte blocks of 8×8 terrain tiles |
| `staidx*.mul` + `statics*.mul` | Static world objects |
| `tiledata.mul` | Tile flags and TextureId mappings |
| `texmaps.mul` + `texidx.mul` | 64×64 or 128×128 stretched textures |
| `art.mul` + `artidx.mul` | Land tiles (44×44) and static items (RLE) |
| `anim.mul` + `anim.idx` | Character/creature animations |

### Block Indexing

Map blocks use column-major ordering:
```csharp
int blockIndex = blockX * blockHeight + blockY;  // Column-major
int cellIndex = cellY * 8 + cellX;               // Row-major within block
```

### Networking

- Custom TCP protocol with binary packet serialization
- Opcodes for different message types
- Client-side prediction with server reconciliation

## Key Classes

### Asset Loaders (Client/Assets/)

| Class | Purpose |
|-------|---------|
| `MapLoader` | Loads terrain from map*.mul, handles UOP format |
| `TileDataLoader` | Loads tile metadata, provides TextureId |
| `TexmapLoader` | Loads stretched terrain textures |
| `ArtLoader` | Loads land tiles and static item graphics |
| `AnimLoader` | Loads character/creature animations |
| `GumpLoader` | Loads UI graphics |

### Rendering (Client/Engine/)

| Class | Purpose |
|-------|---------|
| `WorldRenderer` | Main world rendering (terrain, statics, entities) |
| `Camera` | Viewport management and transforms |
| `AssetManager` | Manages loaded assets and placeholders |

### Core Math (Shared/Core/)

| Class | Purpose |
|-------|---------|
| `IsometricMath` | Coordinate conversion utilities |
| `IsometricHelper` | Legacy coordinate helpers |

## Configuration

### Server (Server/config/server.json)
```json
{
  "Port": 7775,
  "MaxConnections": 1000,
  "TickRate": 20
}
```

### Client (Client/ClientSettings.cs)
```csharp
public static string UODataPath => 
    Environment.GetEnvironmentVariable("UO_DATA_PATH") 
    ?? @"C:\Program Files (x86)\Electronic Arts\Ultima Online Classic";
```

## Features

### Implemented ✅
- TCP networking with custom binary protocol
- Account system with SHA-256 password hashing
- Character creation and selection
- Isometric world rendering with UO terrain
- Stretched terrain textures (texmaps)
- Static object rendering
- Entity system (players, NPCs, items)
- Client-side movement prediction
- Chat system (Say, Yell, Whisper, Party, Guild, Global)
- Camera with smooth follow and zoom
- UO-style gump UI system

### Planned 🔲
- Inventory and item management
- Combat system
- Skills and abilities
- NPC AI and pathfinding
- Quest system
- Crafting
- Housing
- PvP system

## Troubleshooting

### Black terrain / no textures
1. Verify `tiledata.mul` is present and loaded
2. Check console for `[TileData]` log messages
3. Ensure `texmaps.mul` and `texidx.mul` exist

### Map not loading
1. Check that map files exist (map0.mul or map0LegacyMUL.uop)
2. Verify UO data path is correct
3. Look for `[Map]` errors in console

### Animations not working
1. Ensure anim.mul and anim.idx are present
2. Check body IDs match UO's animation system
3. Default human body is 400 (male) or 401 (female)

## Documentation

Detailed technical documentation is available in the `docs/` directory:

- **[ARCHITECTURE.md](docs/ARCHITECTURE.md)** - Deep dive into engine architecture, file formats, and implementation patterns
- **[RENDERING.md](docs/RENDERING.md)** - Comprehensive rendering pipeline documentation with debugging tips
- **[UO_SETUP.md](docs/UO_SETUP.md)** - Complete guide for obtaining and configuring UO data files

## References

This project draws from:
- [ClassicUO](https://github.com/ClassicUO/ClassicUO) - Open source UO client (primary reference)
- [Ultima SDK](https://github.com/polserver/polern) - UO file format documentation
- [UO Dev Forums](https://www.uodevhome.com/) - Community resources

## License

MIT License - Feel free to use this as a foundation for your own MMORPG project!

## Credits

Built with:
- [.NET 8.0](https://dotnet.microsoft.com/)
- [MonoGame 3.8.1](https://www.monogame.net/)
- Ultima Online data files © Electronic Arts
