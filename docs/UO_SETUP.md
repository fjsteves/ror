# Ultima Online Data File Setup Guide

This guide explains how to obtain and configure Ultima Online data files for use with Realm of Reality.

## Overview

Realm of Reality uses genuine Ultima Online data files for:
- Terrain graphics and textures
- Static object sprites (walls, trees, furniture)
- Character and creature animations
- UI graphics (gumps)
- Map terrain data

## Obtaining UO Data Files

### Option 1: Existing UO Installation

If you have Ultima Online installed, the data files are typically located at:

**Windows:**
```
C:\Program Files (x86)\Electronic Arts\Ultima Online Classic\
```

**Linux (via Wine/Proton):**
```
~/.wine/drive_c/Program Files (x86)/Electronic Arts/Ultima Online Classic/
```

### Option 2: UO Classic Client Download

1. Download the official UO Classic Client from:
   - https://uo.com/client-download/ (official)
   - Various community mirrors

2. Install or extract the client

3. Note the installation directory

### Option 3: CUO (ClassicUO) Data

ClassicUO can download and manage UO data files:
1. Download ClassicUO from https://www.classicuo.eu/
2. Run the launcher and let it download game data
3. Find the data at: `ClassicUO/Data/`

## Required Files

### Essential (Minimum Required)

| File | Purpose | Size |
|------|---------|------|
| `tiledata.mul` | Tile metadata, TextureId mappings | ~3-4 MB |
| `map0.mul` OR `map0LegacyMUL.uop` | Felucca terrain data | ~25-90 MB |
| `art.mul` + `artidx.mul` OR `artLegacyMUL.uop` | Land and static graphics | ~200 MB |

### Recommended (Full Experience)

| File | Purpose | Size |
|------|---------|------|
| `texmaps.mul` + `texidx.mul` | Stretched terrain textures | ~30 MB |
| `staidx0.mul` + `statics0.mul` | Static world objects | ~90 MB |
| `anim.mul` + `anim.idx` + `anim*.mul` | Character animations | ~300 MB |
| `gumps.mul` + `gumpidx.mul` OR `gumpartLegacyMUL.uop` | UI graphics | ~40 MB |
| `hues.mul` | Color palettes | ~32 KB |
| `fonts*.mul` | Game fonts | ~1 MB |

### Optional (Additional Content)

| File | Purpose |
|------|---------|
| `map1.mul` / `map1LegacyMUL.uop` | Trammel (T2A) |
| `map2.mul` / `map2LegacyMUL.uop` | Ilshenar (Third Dawn) |
| `map3.mul` / `map3LegacyMUL.uop` | Malas (Age of Shadows) |
| `map4.mul` / `map4LegacyMUL.uop` | Tokuno (Samurai Empire) |
| `map5.mul` / `map5LegacyMUL.uop` | Ter Mur (Stygian Abyss) |
| `sound*.mul` | Sound effects |
| `music/*.mp3` | Background music |

## Configuration

### Method 1: Environment Variable (Recommended)

Set the `UO_DATA_PATH` environment variable to point to your UO data directory.

**Windows (Command Prompt):**
```cmd
set UO_DATA_PATH=C:\UOData
dotnet run --project Client
```

**Windows (PowerShell):**
```powershell
$env:UO_DATA_PATH = "C:\UOData"
dotnet run --project Client
```

**Linux/macOS:**
```bash
export UO_DATA_PATH=/path/to/uodata
dotnet run --project Client
```

**Permanent (Windows):**
1. Open System Properties → Advanced → Environment Variables
2. Add new User variable:
   - Name: `UO_DATA_PATH`
   - Value: `C:\Path\To\UOData`

**Permanent (Linux/macOS):**
Add to `~/.bashrc` or `~/.zshrc`:
```bash
export UO_DATA_PATH=/path/to/uodata
```

### Method 2: ClientSettings.cs

Edit `Client/ClientSettings.cs`:

```csharp
public static string UODataPath => 
    Environment.GetEnvironmentVariable("UO_DATA_PATH") 
    ?? @"C:\Your\Custom\Path\Here";
```

### Method 3: Launch Parameter

```bash
dotnet run --project Client -- --uo-path /path/to/uodata
```

## File Format Notes

### MUL vs UOP

UO uses two file format generations:

**MUL Format (Classic):**
- Separate `.mul` data files and `.idx` index files
- Simple, direct file access
- Used by older clients

**UOP Format (Modern):**
- Compressed package files (`.uop`)
- Contains multiple data entries
- Better compression
- Used by newer clients

Realm of Reality supports **both formats**. The loaders automatically detect and handle either format.

### Format Detection

```
If artLegacyMUL.uop exists → Use UOP format
Else if art.mul + artidx.mul exist → Use MUL format
Else → Error: No art files found
```

## Verification

After configuring, run the client and check the console output:

```
[TileData] Loading tiledata.mul (3156000 bytes)
[TileData] Format: Classic
[TileData] Loaded 16384 land tiles, 131072 static tiles
[TileData] Sample: LandTile[3] TextureId=32, Name="grass"

[Map] Loading facet 0: 7168×4096 tiles (896×512 blocks)
[Map] Loaded from MUL format
[Map] Statics: Available

[Texmaps] Loaded: texmaps.mul (31457280 bytes), 16384 index entries
[Texmaps] Sample: TextureId[32] offset=0, length=32768, size=128×128

[Art] Loaded from MUL format
[Art] 32768 entries available
```

### Common Error Messages

**"File not found: tiledata.mul"**
- Check UO_DATA_PATH is set correctly
- Verify the file exists in the directory

**"Map load failed"**
- map0.mul or map0LegacyMUL.uop not found
- File may be corrupted

**"No TextureId found in first 1000 entries"**
- tiledata.mul may be corrupted
- Wrong file version

## Troubleshooting

### Black Terrain

1. **Check TileData is loaded:**
   ```
   [TileData] Loaded 16384 land tiles
   ```

2. **Verify TextureId values:**
   ```
   [TileData] Sample: LandTile[3] TextureId=32
   ```
   TextureId should be non-zero for textured tiles.

3. **Check Texmaps loaded:**
   ```
   [Texmaps] Loaded: texmaps.mul
   ```

### Missing Statics

1. **Verify files exist:**
   - `staidx0.mul`
   - `statics0.mul`

2. **Check console:**
   ```
   [Map] Statics: Available
   ```

### Missing Animations

1. **Verify animation files:**
   - `anim.mul` + `anim.idx` (basic)
   - `anim2.mul` + `anim2.idx` (additional)
   - `anim3.mul` + `anim3.idx` (extended)
   - `anim4.mul` + `anim4.idx` (more)
   - `anim5.mul` + `anim5.idx` (High Seas)

2. **Body IDs:**
   - Human male: 400
   - Human female: 401
   - Check AnimLoader console output

## Directory Structure Example

```
UOData/
├── tiledata.mul          # REQUIRED
├── map0.mul              # REQUIRED (or .uop)
├── map0LegacyMUL.uop     # Alternative to map0.mul
├── art.mul               # REQUIRED (or .uop)
├── artidx.mul            # REQUIRED with art.mul
├── artLegacyMUL.uop      # Alternative to art.mul
├── texmaps.mul           # Recommended
├── texidx.mul            # Required with texmaps.mul
├── staidx0.mul           # Recommended
├── statics0.mul          # Recommended
├── anim.mul              # For animations
├── anim.idx              # For animations
├── anim2.mul             # Additional animations
├── anim2.idx
├── gumps.mul             # For UI
├── gumpidx.mul
├── hues.mul              # Color palettes
└── fonts0.mul            # Fonts
```

## Legal Notice

Ultima Online and its data files are property of Electronic Arts Inc.
Realm of Reality does not distribute UO data files.
You must obtain these files legally through:
- An existing UO installation
- The official UO Classic Client download

This project is for educational purposes and personal use.

---

*For additional help, check the project's GitHub Issues or Discussions.*
