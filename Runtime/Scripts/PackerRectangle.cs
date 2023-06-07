/*
 * 	Created by Peter @sHTiF Stefcek
 *
 *	C# port from my Haxe GPU framework Genome2D
 *  https://github.com/pshtif/Genome2D-ContextCommon/blob/c8069c6154026e5f71cdf1fe03f0cc89d6aff05b/src/com/genome2d/utils/GMaxRectPacker.hx
 */

using System.Collections.Generic;
using UnityEngine;

namespace TexturePacker
{
    public class PackerRectangle
    {
        public PackerRectangle next;
        public PackerRectangle previous;

        private PackerRectangle _nextInstance;

        static private PackerRectangle _availableInstance;

        public PackerRectangle() 
        {
        }
        
        static public PackerRectangle Get(int p_x, int p_y, int p_width, int p_height, string p_id = null, Texture2D p_source = null,
            float p_pivotX = 0, float p_pivotY = 0) 
        {
            PackerRectangle instance = _availableInstance;

            if (instance != null)
            {
                _availableInstance = instance._nextInstance;
                instance._nextInstance = null;
            }
            else
            {
                instance = new PackerRectangle();
            }

            instance.x = p_x;
            instance.y = p_y;
            instance.width = p_width;
            instance.height = p_height;
            instance.right = p_x + p_width;
            instance.bottom = p_y + p_height;
            instance.id = p_id;
            instance.source = p_source;
            instance.pivotX = p_pivotX;
            instance.pivotY = p_pivotY;

            return instance;
        }

        public int x = 0;
        public int y = 0;

        public int width = 0;
        public int height = 0;

        public int right = 0;
        public int bottom = 0;

        public string id;

        public string originalSourceName = "";
        public Texture2D source;
        public Rect sourceRect;

        public float pivotX;
        public float pivotY;

        public int padding = 0;
        public int extrude = 0;

        public List<float> userData;

        public void Set(int p_x, int p_y, int p_width, int p_height)
        {
            x = p_x;
            y = p_y;
            width = p_width;
            height = p_height;
            right = p_x + p_width;
            bottom = p_y + p_height;
        }

        public void Dispose()
        {
            next = null;
            previous = null;
            _nextInstance = _availableInstance;
            _availableInstance = this;
            Texture2D.DestroyImmediate(source);
            source = null;
        }

        public void SetPadding(int p_value)
        {
            x -= p_value - padding;
            y -= p_value - padding;
            width += (p_value - padding) * 2;
            height += (p_value - padding) * 2;
            right += p_value - padding;
            bottom += p_value - padding;
            padding = p_value;
        }

        public Rect GetRect()
        {
            return new Rect(x, y, width, height);
        }
        
        public Rect GetUnpaddedRect()
        {
            return new Rect(x+padding, y+padding, width-padding*2, height-padding*2);
        }

        public override string ToString()
        {
            return "[" + id + "] x: " + x + " y: " + y + " w: " + width + " h: " + height + " p: " + padding;
        }
    }
}