using System.Collections.Generic;
using UnityEngine;

namespace Aitugan.Core
{
    /// <summary>
    /// Loads and caches all PNG sprites in Resources/AituganPNG. Each PNG is
    /// imported by Unity as a multi-sprite asset with a single sub-sprite, so
    /// we use Resources.LoadAll to fetch the actual Sprite (not the Texture2D
    /// main asset).
    /// </summary>
    public static class Art
    {
        const string Path = "AituganPNG/";
        static readonly Dictionary<string, Sprite> _cache = new();

        public static Sprite Get(string name)
        {
            if (_cache.TryGetValue(name, out var s) && s != null) return s;
            var loaded = Resources.LoadAll<Sprite>(Path + name);
            if (loaded != null && loaded.Length > 0)
            {
                _cache[name] = loaded[0];
                return loaded[0];
            }
            // fallback: try as a single-sprite asset
            s = Resources.Load<Sprite>(Path + name);
            if (s != null) _cache[name] = s;
            return s;
        }

        public static Sprite AituganFront => Get("Aitugan_Front");
        public static Sprite AituganBack  => Get("Aitugan_back");
        public static Sprite AituganLeft  => Get("Aitugan_Left");
        public static Sprite AituganRight => Get("Aitugan_Right");

        public static Sprite Enemy       => Get("Enemy");
        public static Sprite BigEnemy    => Get("Big_Enemy");
        public static Sprite Allies      => Get("Allies_");
        public static Sprite Arrow       => Get("Arrow_");
        public static Sprite Bow         => Get("Bow");
        public static Sprite Kinzhal     => Get("Kinzhal");
        public static Sprite Horse       => Get("Horse");
        public static Sprite Yurt        => Get("Yurt");
        public static Sprite Ashes       => Get("Ashes");
        public static Sprite Box         => Get("Box");
        public static Sprite Father      => Get("Father");
        public static Sprite Paper       => Get("Paper");

        // Ground / floor tiles live in Resources/Grounds.
        // Filenames: Floor.png, Grass.png, Ground.png, Light_Ground.png
        // (LightGround / lightground also accepted).
        public static Sprite Floor       => GetGround("Floor");
        public static Sprite Grass       => GetGround("Grass");
        public static Sprite Ground      => GetGround("Ground");
        public static Sprite LightGround => GetGround("Light_Ground") ?? GetGround("LightGround") ?? GetGround("light_ground");

        static Sprite GetGround(string name)
        {
            var key = "Grounds/" + name;
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
            var loaded = Resources.LoadAll<Sprite>("Grounds/" + name);
            if (loaded != null && loaded.Length > 0) { _cache[key] = loaded[0]; return loaded[0]; }
            var single = Resources.Load<Sprite>("Grounds/" + name);
            if (single != null) _cache[key] = single;
            return single;
        }
    }
}
