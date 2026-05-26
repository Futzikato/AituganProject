using UnityEngine;

namespace Aitugan.Core
{
    /// <summary>
    /// Generates placeholder pixel-art textures and sprites at runtime so the
    /// project is playable without authored art. Every texture is created with
    /// FilterMode.Point to keep a crisp pixel feel.
    /// </summary>
    public static class ProcGfx
    {
        const int PPU = 32;

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        public static Texture2D MakeTex(int w, int h, Color fill)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = fill;
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        public static Sprite MakeSprite(int w, int h, Color fill, string name = null)
        {
            var t = MakeTex(w, h, fill);
            var s = Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PPU);
            if (!string.IsNullOrEmpty(name)) s.name = name;
            return s;
        }

        public static Sprite Solid(Color c, string name = null) => MakeSprite(8, 8, c, name);

        // Aitugan: small humanoid silhouette - dark kaftan, fur cap, braid.
        public static Sprite MakeAitugan()
        {
            int w = 16, h = 24;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            var clear = new Color(0, 0, 0, 0);
            var skin = Hex("#D9B083");
            var hair = Hex("#1B1209");
            var hat = Hex("#3A2418");
            var fur = Hex("#7A5638");
            var kaftan = Hex("#6B2418");
            var belt = Hex("#2A1A10");
            var boot = Hex("#1A1108");

            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = clear;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = clear;
                    // boots
                    if (y < 3 && x >= 5 && x <= 10) c = boot;
                    // legs/lower kaftan
                    else if (y < 9 && x >= 5 && x <= 10) c = kaftan;
                    // belt
                    else if (y == 9 && x >= 4 && x <= 11) c = belt;
                    // upper kaftan
                    else if (y < 16 && x >= 4 && x <= 11) c = kaftan;
                    // braid trailing down back
                    else if (y >= 10 && y <= 17 && x == 4) c = hair;
                    // head
                    else if (y >= 16 && y < 22 && x >= 5 && x <= 10) c = skin;
                    // fur trim of malakhai
                    else if (y == 21 && x >= 4 && x <= 11) c = fur;
                    // hat dome
                    else if (y >= 22 && y < 24 && x >= 5 && x <= 10) c = hat;
                    px[y * w + x] = c;
                }
            t.SetPixels(px);
            t.Apply();
            var s = Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), PPU);
            s.name = "Aitugan";
            return s;
        }

        // Generic Dzungar: lacquered helm, fur shoulder, dark robe.
        public static Sprite MakeDzungar(Color robe, bool shielded = false)
        {
            int w = 16, h = 24;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            var clear = new Color(0, 0, 0, 0);
            var skin = Hex("#C49A6A");
            var helm = Hex("#1F1F22");
            var helmRim = Hex("#5A4022");
            var boot = Hex("#0F0F12");
            var shield = Hex("#3A2A18");
            var shieldBoss = Hex("#A07028");

            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = clear;
                    if (y < 3 && x >= 5 && x <= 10) c = boot;
                    else if (y < 16 && x >= 4 && x <= 11) c = robe;
                    else if (y >= 16 && y < 21 && x >= 5 && x <= 10) c = skin;
                    else if (y == 21 && x >= 4 && x <= 11) c = helmRim;
                    else if (y >= 22 && y < 24 && x >= 5 && x <= 10) c = helm;
                    px[y * w + x] = c;
                }
            if (shielded)
            {
                for (int y = 6; y < 16; y++)
                    for (int x = 0; x < 4; x++)
                    {
                        if (x == 0 || x == 3 || y == 6 || y == 15) px[y * w + x] = helmRim;
                        else px[y * w + x] = shield;
                        if (x == 1 && y == 11) px[y * w + x] = shieldBoss;
                        if (x == 2 && y == 11) px[y * w + x] = shieldBoss;
                    }
            }
            t.SetPixels(px);
            t.Apply();
            var s = Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), PPU);
            s.name = shielded ? "DzungarShield" : "Dzungar";
            return s;
        }

        public static Sprite MakeArrow(Color shaftColor, Color headColor)
        {
            int w = 12, h = 4;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            var clear = new Color(0, 0, 0, 0);
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            for (int x = 0; x < w - 2; x++) px[1 * w + x] = shaftColor;
            for (int x = 0; x < w - 2; x++) px[2 * w + x] = shaftColor;
            // head
            px[1 * w + (w - 2)] = headColor;
            px[2 * w + (w - 2)] = headColor;
            px[1 * w + (w - 1)] = headColor;
            px[2 * w + (w - 1)] = headColor;
            // fletching
            px[0 * w + 0] = headColor;
            px[3 * w + 0] = headColor;
            t.SetPixels(px);
            t.Apply();
            var s = Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), PPU);
            s.name = "Arrow";
            return s;
        }

        public static Sprite MakeCircle(int radius, Color fill, Color edge)
        {
            int d = radius * 2 + 2;
            var t = new Texture2D(d, d, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            var clear = new Color(0, 0, 0, 0);
            var px = new Color[d * d];
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            float r = radius;
            float cx = (d - 1) / 2f, cy = (d - 1) / 2f;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float dd = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dd <= r - 0.5f) px[y * d + x] = fill;
                    else if (dd <= r + 0.5f) px[y * d + x] = edge;
                }
            t.SetPixels(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), PPU);
        }

        public static Sprite MakeRect(int w, int h, Color fill, Color edge)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool border = (x == 0 || x == w - 1 || y == 0 || y == h - 1);
                    px[y * w + x] = border ? edge : fill;
                }
            t.SetPixels(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PPU);
        }

        public static Sprite MakeNoise(int w, int h, Color baseColor, float jitter, int seed)
        {
            var rng = new System.Random(seed);
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++)
            {
                float k = 1f + (float)(rng.NextDouble() - 0.5) * 2f * jitter;
                px[i] = new Color(
                    Mathf.Clamp01(baseColor.r * k),
                    Mathf.Clamp01(baseColor.g * k),
                    Mathf.Clamp01(baseColor.b * k),
                    1f);
            }
            t.SetPixels(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PPU);
        }

        // 9-slice ornamental border texture in oyu-ornek style: a border with
        // small notch motifs. Returned as a regular Sprite for IMGUI box.
        public static Texture2D MakeOyuBorder(int size, Color bg, Color border, Color motif)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            var px = new Color[size * size];
            int b = 4;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = (x < b || x >= size - b || y < b || y >= size - b);
                    px[y * size + x] = isBorder ? border : bg;
                }
            // Notch motif: triangular tabs along each side
            int step = 8;
            for (int x = step; x < size - step; x += step)
            {
                for (int dy = 0; dy < 3; dy++)
                {
                    for (int dx = -dy; dx <= dy; dx++)
                    {
                        if (x + dx >= 0 && x + dx < size)
                        {
                            px[(b + dy) * size + (x + dx)] = motif;
                            px[(size - 1 - b - dy) * size + (x + dx)] = motif;
                        }
                    }
                }
            }
            for (int y = step; y < size - step; y += step)
            {
                for (int dx = 0; dx < 3; dx++)
                {
                    for (int dy = -dx; dy <= dx; dy++)
                    {
                        if (y + dy >= 0 && y + dy < size)
                        {
                            px[(y + dy) * size + (b + dx)] = motif;
                            px[(y + dy) * size + (size - 1 - b - dx)] = motif;
                        }
                    }
                }
            }
            t.SetPixels(px);
            t.Apply();
            return t;
        }
    }
}
