// Integration layer connecting ClassicUO asset loaders to MonoGame rendering

using ClassicUO.Assets;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace RealmOfReality.Client.Assets
{
    /// <summary>
    /// Bridge between ClassicUO asset loaders and MonoGame rendering.
    /// Provides cached Texture2D objects created from ClassicUO raw pixel data.
    /// </summary>
    public class UOAssetBridge : IDisposable
    {
        private readonly GraphicsDevice _graphics;
        private readonly UOFileManager _fileManager;
        private bool _isLoaded;

        // Texture caches
        private readonly Dictionary<int, Texture2D?> _landTextureCache = new();
        private readonly Dictionary<int, Texture2D?> _staticTextureCache = new();
        private readonly Dictionary<int, Texture2D?> _gumpTextureCache = new();
        private readonly Dictionary<int, Texture2D?> _texmapCache = new();

        public bool IsLoaded => _isLoaded;
        public UOFileManager FileManager => _fileManager;

        // Direct access to ClassicUO loaders for advanced usage
        public ArtLoader Arts => _fileManager.Arts;
        public MapLoader Maps => _fileManager.Maps;
        public TileDataLoader TileData => _fileManager.TileData;
        public HuesLoader Hues => _fileManager.Hues;
        public GumpsLoader Gumps => _fileManager.Gumps;
        public AnimationsLoader Animations => _fileManager.Animations;
        public TexmapsLoader Texmaps => _fileManager.Texmaps;
        public FontsLoader Fonts => _fileManager.Fonts;

        public UOAssetBridge(GraphicsDevice graphics, string uoDataPath, ClientVersion version = ClientVersion.CV_70331)
        {
            _graphics = graphics;
            _fileManager = new UOFileManager(version, uoDataPath);
        }

        /// <summary>
        /// Load all asset files.
        /// </summary>
        public bool Load(bool useVerdata = false, string language = "ENU")
        {
            try
            {
                _fileManager.Load(useVerdata, language);
                _isLoaded = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UOAssetBridge: Failed to load assets: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load a specific map.
        /// </summary>
        public void LoadMap(int mapIndex, bool useXFiles = false)
        {
            Maps.LoadMap(mapIndex, useXFiles);
        }

        // ============ Art Textures ============

        /// <summary>
        /// Get a land tile texture (IDs 0-16383).
        /// </summary>
        public Texture2D? GetLandTexture(int tileId)
        {
            if (!_isLoaded || tileId < 0 || tileId >= ArtLoader.MAX_LAND_DATA_INDEX_COUNT)
                return null;

            if (_landTextureCache.TryGetValue(tileId, out var cached))
                return cached;

            var artInfo = Arts.GetArt((uint)tileId);
            var texture = CreateTexture(artInfo.Pixels, artInfo.Width, artInfo.Height);
            _landTextureCache[tileId] = texture;
            return texture;
        }

        /// <summary>
        /// Get a static item texture.
        /// </summary>
        public Texture2D? GetStaticTexture(int itemId)
        {
            if (!_isLoaded || itemId < 0)
                return null;

            if (_staticTextureCache.TryGetValue(itemId, out var cached))
                return cached;

            var artInfo = Arts.GetArt((uint)(itemId + ArtLoader.MAX_LAND_DATA_INDEX_COUNT));
            var texture = CreateTexture(artInfo.Pixels, artInfo.Width, artInfo.Height);
            _staticTextureCache[itemId] = texture;
            return texture;
        }

        /// <summary>
        /// Get static item info including dimensions.
        /// </summary>
        public (Texture2D? texture, int width, int height) GetStaticInfo(int itemId)
        {
            if (!_isLoaded || itemId < 0)
                return (null, 0, 0);

            var artInfo = Arts.GetArt((uint)(itemId + ArtLoader.MAX_LAND_DATA_INDEX_COUNT));
            var texture = GetStaticTexture(itemId);
            return (texture, artInfo.Width, artInfo.Height);
        }

        // ============ Gump Textures ============

        /// <summary>
        /// Get a gump texture.
        /// </summary>
        public Texture2D? GetGumpTexture(int gumpId)
        {
            if (!_isLoaded || gumpId < 0)
                return null;

            if (_gumpTextureCache.TryGetValue(gumpId, out var cached))
                return cached;

            var gumpInfo = Gumps.GetGump((uint)gumpId);
            var texture = CreateTexture(gumpInfo.Pixels, gumpInfo.Width, gumpInfo.Height);
            _gumpTextureCache[gumpId] = texture;
            return texture;
        }

        /// <summary>
        /// Get gump info including dimensions.
        /// </summary>
        public (Texture2D? texture, int width, int height) GetGumpInfo(int gumpId)
        {
            if (!_isLoaded || gumpId < 0)
                return (null, 0, 0);

            var gumpInfo = Gumps.GetGump((uint)gumpId);
            var texture = GetGumpTexture(gumpId);
            return (texture, gumpInfo.Width, gumpInfo.Height);
        }

        // ============ Texmap Textures ============

        /// <summary>
        /// Get a terrain texture (64x64 or 128x128).
        /// </summary>
        public Texture2D? GetTexmapTexture(int texmapId)
        {
            if (!_isLoaded || texmapId < 0)
                return null;

            if (_texmapCache.TryGetValue(texmapId, out var cached))
                return cached;

            var texInfo = Texmaps.GetTexmap((uint)texmapId);
            var texture = CreateTexture(texInfo.Pixels, texInfo.Width, texInfo.Height);
            _texmapCache[texmapId] = texture;
            return texture;
        }

        // ============ TileData Access ============

        /// <summary>
        /// Get land tile flags and properties.
        /// </summary>
        public LandTiles GetLandData(int tileId)
        {
            if (!_isLoaded || tileId < 0 || tileId >= TileData.LandData.Length)
                return default;
            return TileData.LandData[tileId];
        }

        /// <summary>
        /// Get static item flags and properties.
        /// </summary>
        public StaticTiles GetStaticData(int itemId)
        {
            if (!_isLoaded || itemId < 0 || itemId >= TileData.StaticData.Length)
                return default;
            return TileData.StaticData[itemId];
        }

        // ============ Hue Application ============

        /// <summary>
        /// Apply a hue to pixel data.
        /// </summary>
        public void ApplyHue(Span<uint> pixels, ushort hue, bool partial = false)
        {
            if (!_isLoaded || hue == 0 || Hues == null)
                return;

            for (int i = 0; i < pixels.Length; i++)
            {
                uint pixel = pixels[i];
                if ((pixel & 0xFF000000) == 0) continue; // Skip transparent

                // Extract RGB
                byte r = (byte)(pixel & 0xFF);
                byte g = (byte)((pixel >> 8) & 0xFF);
                byte b = (byte)((pixel >> 16) & 0xFF);

                // For partial hue, only transform grayscale pixels
                if (partial && !(r == g && g == b))
                    continue;

                // Convert to 16-bit, apply hue, convert back
                ushort color16 = HuesHelper.Color32To16(pixel);
                uint newPixel = Hues.GetColor(color16, hue);
                pixels[i] = newPixel | (pixel & 0xFF000000); // Preserve alpha
            }
        }

        // ============ Helper Methods ============

        private Texture2D? CreateTexture(Span<uint> pixels, int width, int height)
        {
            if (pixels.Length == 0 || width <= 0 || height <= 0)
                return null;

            try
            {
                var texture = new Texture2D(_graphics, width, height);
                texture.SetData(pixels.ToArray());
                return texture;
            }
            catch
            {
                return null;
            }
        }

        // ============ Cache Management ============

        public void ClearCaches()
        {
            foreach (var tex in _landTextureCache.Values)
                tex?.Dispose();
            foreach (var tex in _staticTextureCache.Values)
                tex?.Dispose();
            foreach (var tex in _gumpTextureCache.Values)
                tex?.Dispose();
            foreach (var tex in _texmapCache.Values)
                tex?.Dispose();

            _landTextureCache.Clear();
            _staticTextureCache.Clear();
            _gumpTextureCache.Clear();
            _texmapCache.Clear();
        }

        public void Dispose()
        {
            ClearCaches();
            _fileManager.Dispose();
        }
    }
}
