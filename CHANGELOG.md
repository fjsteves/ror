# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added

#### Documentation
- `docs/ARCHITECTURE.md` - Comprehensive engine architecture documentation
  - Rendering pipeline overview
  - Asset loading system
  - UO file format specifications (complete)
  - Isometric projection math
  - Coordinate systems
  - Entity system
  - Network architecture
  - Performance considerations
  - Debugging guide

- `docs/RENDERING.md` - Detailed rendering system documentation
  - Frame render order
  - Terrain quad system (why 3D quads, not SpriteBatch)
  - Vertex construction and UV mapping
  - Texture lookup chain (TextureId vs TileId)
  - Depth sorting algorithm
  - Entity rendering
  - Common bugs and fixes
  - Debug utilities

- `docs/UO_SETUP.md` - Complete UO data file setup guide
  - How to obtain UO data files
  - Required vs optional files
  - Configuration methods
  - Verification steps
  - Troubleshooting guide

- `docs/QUICK_REFERENCE.md` - Developer cheat sheet
  - Isometric formulas
  - Block/cell indexing
  - Texture lookup chain
  - Terrain quad corners
  - Depth sorting
  - File format sizes
  - ARGB1555 conversion
  - Map dimensions
  - Animation directions
  - Critical rules

- `docs/README.md` - Documentation index with quick links

#### WorldRenderer Enhancements
- Added render statistics tracking:
  - `VoidQuadCount` - Terrain quads skipped due to void
  - `RenderedQuadCount` - Terrain quads rendered
  - `RenderedStaticCount` - Statics rendered
  - `RenderedEntityCount` - Entities rendered
  - `TexmapHitCount` - Texmap texture lookups
  - `ArtFallbackCount` - Art texture fallbacks
- Added `GetRenderStats()` method for debugging
- Added `ShowDebugStats` and `ShowTextureIds` options
- Enhanced class documentation with ASCII art diagrams

#### Camera Enhancements
- Added zoom limits (`MIN_ZOOM = 0.25`, `MAX_ZOOM = 4.0`)
- Added `ZoomIn()`, `ZoomOut()`, `ResetZoom()` methods
- Added `SnapToTarget()` for instant camera positioning
- Enhanced documentation explaining coordinate system

#### UOAssetManager Enhancements
- Enhanced class documentation with:
  - Asset types table
  - Loading order explanation
  - Usage examples
  - Critical notes about TextureId

### Changed
- Updated README.md to reference new documentation
- Improved code comments throughout asset loaders and rendering code
- Added CRITICAL warnings about TextureId vs TileId throughout codebase

### Fixed
- (No bug fixes in this refactoring pass - documentation and comments only)

## Previous Versions

This changelog was created as part of the December 2025 refactoring effort.
Previous changes were not tracked in a changelog format.

---

### Version Naming

- **Unreleased**: Work in progress
- **Major.Minor.Patch**: Following semantic versioning
  - Major: Breaking API changes
  - Minor: New features, backwards compatible
  - Patch: Bug fixes, backwards compatible
