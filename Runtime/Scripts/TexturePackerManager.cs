/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TexturePacker
{
    public class TexturePackerManager : MonoBehaviour
    {
        public bool packOnStart = true;

        public int maxWidth = 2048;
        public int maxHeight = 2048;

        public Color clearColor = new Color(0, 0, 0, 0);
        public bool clearTexture = true;

        public int padding = 0;

        public bool packDisabled = false;
        public bool removeUnused = false;

        public RawImage previewer;

        private MaxRectPacker _packer;
        private Texture2D _previousAtlas;
        private Texture2D _atlas;
        private List<PackerRectangle> _atlasedRects;
        private Dictionary<PackerRectangle, Sprite> _mappedSprites;

        void Start()
        {
            if (packOnStart)
            {
                Invalidate();
            }
        }

        Image[] EnumerateImages()
        {
            return gameObject.GetComponentsInChildren<Image>();
        }

        List<PackerRectangle> GetUnpackedRects(Image[] p_images, List<PackerRectangle> p_packedRects)
        {
            var unpackedRects = new List<PackerRectangle>();

            foreach (var image in p_images)
            {
                if (!image.enabled && !packDisabled)
                    continue;

                var sprite = image.sprite;
                var texture = sprite.texture;

                if (texture == null)
                {
                    Debug.LogWarning("Packing null textures, avoiding.");
                    continue;
                }

                if (unpackedRects.Exists(p => p.source == texture) || (p_packedRects != null &&
                                                                       p_packedRects.Exists(p =>
                                                                           p.source == texture ||
                                                                           p.originalSourceName == sprite.name)))
                    continue;

                if (!texture.isReadable)
                {
                    Debug.LogWarning("Texture " + texture.name + " is not set as readable.");
                }
                else
                {
                    var rect = new PackerRectangle();
                    rect.width = (int)sprite.rect.width;
                    rect.height = (int)sprite.rect.height;
                    rect.originalSourceName = sprite.name;
                    rect.source = texture;
                    rect.sourceRect = sprite.rect;
                    rect.SetPadding(padding);
                    unpackedRects.Add(rect);
                }
            }

            return unpackedRects;
        }

        List<PackerRectangle> GetUnusedRectangles(Image[] p_images, List<PackerRectangle> p_packedRects)
        {
            var unusedRects = p_packedRects.FindAll(p =>
            {
                foreach (var image in p_images)
                {
                    if (image.sprite.name == p.originalSourceName)
                        return false;
                }

                return true;
            });

            return unusedRects;
        }

        public void Repack(List<PackerRectangle> p_packedRects)
        {
            InitializePacker(true);

            var images = EnumerateImages();
            if (removeUnused)
            {
                var unusedRects = GetUnusedRectangles(images, p_packedRects);
                Debug.Log(unusedRects.Count);
                p_packedRects.RemoveAll(p => unusedRects.Contains(p));
            }

            var unpackedRects = GetUnpackedRects(images, p_packedRects);

            unpackedRects.AddRange(p_packedRects);

            _packer.Clear();
            var status = _packer.PackRectangles(unpackedRects);

            InvalidateAtlas();

            ModifyImages(images);

            Preview();
        }
        
        public void Invalidate()
        {
            InitializePacker(false);

            var images = EnumerateImages();
            var packedRects = _packer.GetRectangles();
            var unpackedRects = GetUnpackedRects(images, packedRects);
            var unusedRects = removeUnused ? GetUnusedRectangles(images, packedRects) : null;

            bool success = true;
            foreach (var rect in unpackedRects)
            {
                if (!_packer.PackRectangle(rect))
                {
                    success = false;
                    break;
                }
            }

            if (success)
            {
                InvalidateAtlas();
                ModifyImages(images);
            }
            else
            {
                _previousAtlas = _atlas;
                _atlas = removeUnused && unusedRects.Count > 0 ? null : _atlas;
                Repack(packedRects);
            }
        }

        void InvalidateAtlas()
        {
            var rects = _packer.GetRectangles();

            _atlasedRects ??= new List<PackerRectangle>();
            _previousAtlas = _atlas;
            if (_atlas == null || _atlas.width != _packer.GetWidth() || _atlas.height != _packer.GetHeight())
            {
                //Debug.Log("Initializing new atlas texture: "+_packer.GetWidth()+" : "+_packer.GetHeight());
                _atlas = new Texture2D(_packer.GetWidth(), _packer.GetHeight(), TextureFormat.RGBA32, false);
                if (clearTexture)
                {
                    // Clear texture it can containt various data depending on hw/drivers.
                    Color[] pixels = Enumerable.Repeat(clearColor, _atlas.width * _atlas.height).ToArray();
                    _atlas.SetPixels(pixels);
                }

                _atlas.name = "TexturePackAtlas";
                _atlasedRects.Clear();
                _mappedSprites.Clear();
            }

            foreach (var rect in rects)
            {

                if (_atlasedRects.Contains(rect))
                    continue;

                _atlasedRects.Add(rect);

                var pixels = rect.source.GetPixels32();

                // Already in atlas we need to tweak a little as GetPixels32 method can't fetch region
                if (rect.sourceRect.x != 0 || rect.sourceRect.y != 0 || rect.sourceRect.width != rect.source.width ||
                    rect.sourceRect.height != rect.source.height)
                {
                    for (int j = 0; j < rect.sourceRect.height; j++)
                    {
                        int start = (int)(rect.sourceRect.x + rect.source.width * (rect.sourceRect.y + j));
                        int end = start + (int)rect.sourceRect.width;
                        // Debug.Log("SOURCE: " + rect.source.width + " : " + rect.source.height);
                        // Debug.Log("REGION: "+ rect.sourceRect);
                        // Debug.Log("RECT: "+rect.width+" : "+rect.height);
                        // Debug.Log("START/END: "+start+" : "+end+" : "+pixels[start..end].Length);
                        // Debug.Log("DESC: " + _atlas.width+" : "+_atlas.height);
                        _atlas.SetPixels32((int)rect.x, (int)rect.y + j, (int)rect.width, 1, pixels[start..end]);
                    }
                }
                else
                {
                    _atlas.SetPixels32((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height, pixels);
                }
            }

            _atlas.Apply();

            if (_previousAtlas != _atlas)
            {
                DestroyImmediate(_previousAtlas);
            }
        }

        void InitializePacker(bool p_forceNew)
        {
            if (_packer == null || p_forceNew)
            {
                _packer = new MaxRectPacker(1, 1, maxWidth, maxHeight, true);
                _atlasedRects = new List<PackerRectangle>();
                _mappedSprites = new Dictionary<PackerRectangle, Sprite>();
            }
        }
        
        void ModifyImages(Image[] p_images)
        {
            var rects = _packer.GetRectangles();
            foreach (var image in p_images)
            {
                var originalSprite = image.sprite;
                var rect = rects.Find(p => p.originalSourceName == originalSprite.name);

                if (rect == null || originalSprite.texture == _atlas)
                    continue;

                var sprite = _mappedSprites.ContainsKey(rect)
                    ? _mappedSprites[rect]
                    : Sprite.Create(_atlas, rect.GetUnpaddedRect(), originalSprite.pivot, originalSprite.pixelsPerUnit,
                        0,
                        SpriteMeshType.FullRect, originalSprite.border);

                sprite.name = originalSprite.name;
                image.sprite = sprite;
            }

            foreach (var rect in rects)
            {
                rect.source = _atlas;
                rect.sourceRect = rect.GetUnpaddedRect();
            }
        }

        void Preview()
        {
            if (previewer == null)
                return;

            previewer.texture = _atlas;
            previewer.SetNativeSize();
        }
    }
}