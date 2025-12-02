# Documentation

Welcome to the Realm of Reality documentation. This directory contains technical
documentation for understanding and working with the game engine.

## Documents

| Document | Description |
|----------|-------------|
| [ARCHITECTURE.md](ARCHITECTURE.md) | Deep dive into engine architecture, file formats, coordinate systems, and implementation patterns |
| [RENDERING.md](RENDERING.md) | Detailed rendering pipeline documentation including terrain quads, texture loading, and debugging |
| [UO_SETUP.md](UO_SETUP.md) | Complete guide for obtaining and configuring Ultima Online data files |
| [QUICK_REFERENCE.md](QUICK_REFERENCE.md) | Developer cheat sheet with formulas, constants, and critical patterns |

## Quick Links

### Getting Started

1. Read [UO_SETUP.md](UO_SETUP.md) to configure UO data files
2. Review [QUICK_REFERENCE.md](QUICK_REFERENCE.md) for key formulas
3. Dive into [ARCHITECTURE.md](ARCHITECTURE.md) for system design

### Debugging Terrain Issues

See the "Common Bugs and Fixes" section in [RENDERING.md](RENDERING.md):
- Black/missing terrain
- Terrain gaps/seams
- Z-fighting (flickering)
- Sprites at wrong positions

### Understanding File Formats

See [ARCHITECTURE.md](ARCHITECTURE.md) for complete specifications:
- tiledata.mul (tile metadata)
- map*.mul (terrain data)
- texmaps.mul (stretched textures)
- art.mul (land/static graphics)
- statics*.mul (world objects)

### Critical Rules

From [QUICK_REFERENCE.md](QUICK_REFERENCE.md):

1. **NEVER** use SpriteBatch for terrain - use 3D quads
2. **NEVER** pass TileId to Texmaps - use TextureId from TileData
3. **ALWAYS** use column-major block indexing
4. **ALWAYS** check bounds with `x < Width-1` (not `<=`)
5. **ALWAYS** sort terrain quads back-to-front

## External Resources

- [ClassicUO](https://github.com/ClassicUO/ClassicUO) - Reference implementation
- [UO Guide](https://uo.stratics.com/) - Game mechanics reference
- [MonoGame](https://www.monogame.net/documentation/) - Framework documentation
