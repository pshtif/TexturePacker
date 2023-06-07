/*
 * 	Created by Peter @sHTiF Stefcek
 *
 *	C# port from my Haxe GPU framework Genome2D
 *  https://github.com/pshtif/Genome2D-ContextCommon/blob/c8069c6154026e5f71cdf1fe03f0cc89d6aff05b/src/com/genome2d/utils/GMaxRectPacker.hx
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TexturePacker
{
    public enum PackerHeuristics
    {
        BOTTOM_LEFT,
        SHORT_SIDE_FIT,
        LONG_SIDE_FIT,
        AREA_FIT
    }

    public enum PackerSort
    {
        NONE,
        ASCENDING,
        DESCENDING
    }
    
    public class MaxRectPacker
    {
        static public int nonValidTextureSizePrecision = 5;
        
        private PackerHeuristics _heuristics = PackerHeuristics.BOTTOM_LEFT;
        
        private PackerRectangle _firstAvailableArea;
        private PackerRectangle _lastAvailableArea;
        
        private PackerRectangle _firstNewArea;
        private PackerRectangle _lastNewArea;
        
        private PackerRectangle _newBoundingArea;
        private PackerRectangle _negativeArea;

        private int _startWidth;
        private int _startHeight;
        private int _maxWidth;
        private int _maxHeight;
        private bool _autoExpand = false;
        private PackerSort _sortOnExpand = PackerSort.DESCENDING;
        private bool _forceValidTextureSizeOnExpand = true;

        private int _width;
        public int GetWidth()
        {
            return _width;
        }

        private int _height;
        public int GetHeight()
        {
            return _height;
        }

        private List<PackerRectangle> _rectangles;
        public List<PackerRectangle> GetRectangles()
        {
            // Clone
            return _rectangles.ToList();
        }

        public MaxRectPacker(int p_width = 1, int p_height = 1, int p_maxWidth = 2048, int p_maxHeight = 2048, bool p_autoExpand = false, PackerHeuristics p_heuristics = PackerHeuristics.BOTTOM_LEFT) 
        {
            if (p_width <= 0 || p_height <= 0) 
                Debug.LogError("Invalid packer size.");
            
            _width = p_width;
            _height = p_height;
            _maxWidth = p_maxWidth;
            _maxHeight = p_maxHeight;
            _autoExpand = p_autoExpand;

            Clear();

            _newBoundingArea = PackerRectangle.Get(0,0,0,0);
            _heuristics = p_heuristics;
        }

        public void PackRectangleFixed(PackerRectangle p_rect)
        {
            AddRectangleFixed(p_rect);
        }

        public bool PackRectangle(PackerRectangle p_rect, int p_padding = 0)
        {
            bool success = AddRectangle(p_rect, p_padding);

            // Removed for now will change API later
            // if (!success && p_expand) 
            // {
            //     List<PackerRectangle> storedRectangles = GetRectangles();
            //     storedRectangles.Add(p_rect);
            //
            //     Clear();
            //     PackRectangles(storedRectangles, p_padding, _sortOnExpand);
            //
            //     success = true;
            // }

            return success;
        }
        
        public bool PackRectangles(List<PackerRectangle> p_rectangles, int p_padding = 0, PackerSort p_sort = PackerSort.DESCENDING)
        {
            if (p_sort != PackerSort.NONE)
            {
                p_rectangles.Sort((p_sort == PackerSort.ASCENDING) ? SortOnHeightAscending : SortOnHeightDescending);
            }
            
            bool success = true;
            List<PackerRectangle> failedRectangles = _autoExpand ? new List<PackerRectangle>() : null;
            for (int i = 0; i<p_rectangles.Count; i++) 
            {
                PackerRectangle rect = p_rectangles[i];
                bool s = AddRectangle(rect, p_padding);
                if (!s && _autoExpand) failedRectangles.Add(p_rectangles[i]);
                success = success&&s;
            }
            
            if (!success && _autoExpand) 
            {
                List<PackerRectangle> storedRectangles = GetRectangles();
                storedRectangles = storedRectangles.Concat(failedRectangles).ToList();

                if (_sortOnExpand != PackerSort.NONE) storedRectangles.Sort(_sortOnExpand == PackerSort.ASCENDING ? SortOnHeightAscending : SortOnHeightDescending);
                
                int minimalArea = GetRectanglesArea(storedRectangles);

                do
                {
                    if ((_width <= _height || _height == _maxHeight) && _width < _maxWidth)
                    {
                        _width = _forceValidTextureSizeOnExpand ? _width * 2 : _width + 1;
                    }
                    else
                    {
                        _height = _forceValidTextureSizeOnExpand ? _height * 2 : _height + 1;
                    }
                } while (_width * _height < minimalArea && (_width < _maxWidth || _height < _maxHeight));

                Clear();
                
                success = AddRectangles(storedRectangles, p_padding);

                while (!success && (_width < _maxWidth || _height < _maxHeight))
                {
                    if ((_width <= _height || _height == _maxHeight) && _width < _maxWidth)
                    {
                        _width = _forceValidTextureSizeOnExpand ? _width * 2 : _width + nonValidTextureSizePrecision;
                    }
                    else
                    {
                        _height = _forceValidTextureSizeOnExpand ? _height*2 : _height+nonValidTextureSizePrecision;
                    }
                    Clear();
                    success = AddRectangles(storedRectangles, p_padding);
                }

                success = _width <= _maxWidth && _height <= _maxHeight;
            }
            
            return success;
        }

        private int GetRectanglesArea(List<PackerRectangle> p_rectangles)
        {
            int area = 0;
            int i = p_rectangles.Count-1;
            while (i>=0)
            {
                area += p_rectangles[i].width * p_rectangles[i].height;
                i--;
            }
            return area;
        }
        
        private bool AddRectangles(List<PackerRectangle> p_rectangles, int p_padding = 0, bool p_force = true)
        {
            bool success = true;
            for (int i = 0; i<p_rectangles.Count; i++) 
            {
                PackerRectangle rect = p_rectangles[i];
                success = success && AddRectangle(rect, p_padding);
                if (!success&&!p_force) return false;
            }			
            return success;
        }
        
        private bool AddRectangle(PackerRectangle p_rect, int p_padding)
        {
            PackerRectangle area = GetAvailableArea(p_rect.width + (p_padding - p_rect.padding) * 2,
                p_rect.height + (p_padding - p_rect.padding) * 2);
            if (area != null)
            {
                p_rect.Set(area.x, area.y, p_rect.width + (p_padding - p_rect.padding) * 2,
                    p_rect.height + (p_padding - p_rect.padding) * 2);
                p_rect.padding = p_padding;

                SplitAvailableAreas(p_rect);
                PushNewAreas();

                if (p_padding != 0) p_rect.SetPadding(0);

                _rectangles.Add(p_rect);
            }
            return area != null;
        }

        private void AddRectangleFixed(PackerRectangle p_rect) 
        {
            SplitAvailableAreas(p_rect);
            PushNewAreas();

            _rectangles.Add(p_rect);
        }
        
        private PackerRectangle CreateNewArea(int p_x, int p_y, int p_width, int p_height)
        {
            bool valid = true;

            PackerRectangle area = _firstNewArea;
            while (area != null) 
            {
                PackerRectangle next = area.next;
                if (!(area.x > p_x || area.y > p_y || area.right < p_x + p_width || area.bottom < p_y + p_height))  
                {
                    valid = false;
                    break;
                } 
                else if (!(area.x < p_x || area.y < p_y || area.right > p_x + p_width ||
                           area.bottom > p_y + p_height))
                {
                    if (area.next != null) area.next.previous = area.previous;
                    else _lastNewArea = area.previous;
                    
                    if (area.previous != null) area.previous.next = area.next;
                    else _firstNewArea = area.next;
                    
                    area.Dispose();
                }
                area = next;
            }
            if (valid) {
                area = PackerRectangle.Get(p_x, p_y, p_width, p_height);
                if (_newBoundingArea.x < p_x) _newBoundingArea.x = p_x;
                if (_newBoundingArea.right > area.right) _newBoundingArea.right = area.right;
                if (_newBoundingArea.y < p_y) _newBoundingArea.y = p_y;
                if (_newBoundingArea.bottom < area.bottom) _newBoundingArea.bottom = area.bottom;

                if (_lastNewArea != null)  {
                    area.previous = _lastNewArea;
                    _lastNewArea.next = area;
                    _lastNewArea = area;
                } else {
                    _lastNewArea = area;
                    _firstNewArea = area;
                }
            } else {
                area = null;
            }

            return area;
        }
        
        private void SplitAvailableAreas(PackerRectangle p_splitter) {
            int sx = p_splitter.x;
            int sy = p_splitter.y;
            int sright = p_splitter.right;
            int sbottom = p_splitter.bottom;

            PackerRectangle area = _firstAvailableArea;
            while (area != null) {
                PackerRectangle next = area.next;
                
                if (!(sx >= area.right || sright <= area.x || sy >= area.bottom || sbottom <= area.y)) {
                    if (sx > area.x) {
                        CreateNewArea(area.x, area.y, sx-area.x, area.height);
                    }
                    if (sright < area.right) {
                        CreateNewArea(sright, area.y, area.right - sright, area.height);
                    }
                    if (sy > area.y) {
                        CreateNewArea(area.x, area.y, area.width, sy - area.y);
                    }
                    if (sbottom < area.bottom) {
                        CreateNewArea(area.x, sbottom, area.width, area.bottom - sbottom);
                    }
                    
                    if (area.next != null) area.next.previous = area.previous;
                    else _lastAvailableArea = area.previous;
                    
                    if (area.previous != null) area.previous.next = area.next;
                    else _firstAvailableArea = area.next;
                    
                    area.Dispose();
                }
                area = next;
            }
        }
        
        private void PushNewAreas()
        {
            while (_firstNewArea != null) {
                PackerRectangle area = _firstNewArea;
                if (area.next != null) {
                    _firstNewArea = area.next;
                    _firstNewArea.previous = null;
                } else {
                    _firstNewArea = null;
                }
                area.previous = null;
                area.next = null;
                
                if (_lastAvailableArea != null) {
                    area.previous = _lastAvailableArea;
                    _lastAvailableArea.next = area;
                    _lastAvailableArea = area;
                } else {
                    _lastAvailableArea = area;
                    _firstAvailableArea = area;
                }
            }
            
            _lastNewArea = null;
            _newBoundingArea.Set(0,0,0,0);
        }

        private PackerRectangle GetAvailableArea(int p_width, int p_height)
        {
            PackerRectangle available = _negativeArea;
            PackerRectangle area;
            int w;
            int h;
            int m1;
            int m2;

            if (_heuristics == PackerHeuristics.BOTTOM_LEFT) 
            {
                area = _firstAvailableArea;
                while (area != null) 
                {
                    if (area.width>=p_width && area.height>=p_height) 
                    {
                        if (area.y < available.y || (area.y == available.y && area.x < available.x)) available = area;
                    }
                    area = area.next;
                }
            } 
            else if (_heuristics == PackerHeuristics.SHORT_SIDE_FIT)
            {
                available.width = _width + 1;
                area = _firstAvailableArea;
                while (area != null) 
                {
                    if (area.width >= p_width && area.height >= p_height) {
                        w = area.width - p_width;
                        h = area.height - p_height;
                        m1 = (w<h) ? w : h;
                        w = available.width - p_width;
                        h = available.height - p_height;
                        m2 = (w<h) ? w : h;
                        if (m1 < m2) available = area;
                    }
                    area = area.next;
                }
            } 
            else if (_heuristics == PackerHeuristics.LONG_SIDE_FIT) 
            {
                available.width = _width+1;
                area = _firstAvailableArea;
                while (area != null) {
                    if (area.width >= p_width && area.height >= p_height) {
                        w = area.width - p_width;
                        h = area.height - p_height;
                        m1 = (w>h) ? w : h;
                        w = available.width - p_width;
                        h = available.height - p_height;
                        m2 = (w>h) ? w : h;
                        if (m1 < m2) available = area;
                    }
                    area = area.next;
                }
            } 
            else if (_heuristics == PackerHeuristics.AREA_FIT)
            {
                available.width = _width+1;
                area = _firstAvailableArea;
                while (area != null) {
                    if (area.width>=p_width && area.height>=p_height) {
                        int a1 = area.width*area.height;
                        int a2 = available.width*available.height;
                        if (a1 < a2 || (a1 == a2 && area.width < available.width)) available = area;
                    }

                    area = area.next;
                }
            }

            return (available != _negativeArea) ? available : null;
        }
        
        public void Clear()
        {
            _rectangles = new List<PackerRectangle>();

            while (_firstAvailableArea != null) {
                PackerRectangle area = _firstAvailableArea;
                _firstAvailableArea = area.next;
                area.Dispose();
            }
            
            _firstAvailableArea = _lastAvailableArea = PackerRectangle.Get(0,0,_width, _height);
            _negativeArea = PackerRectangle.Get(_width+1, _height+1, _width+1, _height+1);
        }

        private int SortOnAreaAscending(PackerRectangle a, PackerRectangle b)
        {
            int aa = a.width*a.height;
            int ba = b.width*b.height;

            if (aa < ba) 
                return -1;
            else if (aa>ba) 
                return 1;
            
            return 0;
        }

        private int SortOnAreaDescending(PackerRectangle a, PackerRectangle b)
        {
            int aa = a.width*a.height;
            int ba = b.width*b.height;
            
            if (aa > ba)
                return -1;
            else if (aa<ba)
                return 1;
            
            return 0;
        }

        private int SortOnHeightAscending(PackerRectangle a, PackerRectangle b)
        {
            if (a.height < b.height)
                return -1;
            else if (a.height>b.height)
                return 1;
            
            return 0;
        }

        private int SortOnHeightDescending(PackerRectangle a, PackerRectangle b)
        {
            if (a.height > b.height)
                return -1;
            else if (a.height<b.height)
                return 1;
            
            return 0;
        }
        
        public void Blit(Texture2D p_textur)
        {
            Vector2Int point = new Vector2Int();
            for (int i=0; i<_rectangles.Count; i++)
            {
                PackerRectangle rect = _rectangles[i];
                point.x = _rectangles[i].x;
                point.y = _rectangles[i].y;
        
                //p_bitmapData.copyPixels(rect.source, rect.source.rect, point);
            }
        }
    }
}