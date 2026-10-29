using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualDarkStore.Scenarios
{
    /// <summary>
    /// Top-down (X/Z) rasterised route map built on the CPU, usable at runtime (IMGUI) and for PNG export.
    /// Draws the recorded route points of each run; no interpolation beyond straight segments between samples.
    /// </summary>
    public static class RouteMapRenderer
    {
        public static readonly Color ColorA = new Color(0.20f, 0.55f, 1.00f, 1f);
        public static readonly Color ColorB = new Color(1.00f, 0.55f, 0.10f, 1f);

        public static Texture2D Render(
            IReadOnlyList<ScenarioRunResult> runs,
            IReadOnlyList<Color> colors,
            IEnumerable<Vector3> backdropPoints,
            int width = 640,
            int height = 640)
        {
            var all = new List<Vector3>();
            foreach (var r in runs) all.AddRange(r.routePoints);
            var backdrop = new List<Vector3>(backdropPoints ?? new Vector3[0]);
            all.AddRange(backdrop);
            if (all.Count == 0) throw new InvalidOperationException("Nothing to render.");

            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var p in all)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z);
            }
            const float pad = 1.0f;
            minX -= pad; maxX += pad; minZ -= pad; maxZ += pad;
            float scale = Mathf.Min((width - 1) / (maxX - minX), (height - 1) / (maxZ - minZ));

            var pixels = new Color32[width * height];
            var bg = new Color32(18, 22, 32, 255);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;

            Vector2Int ToPx(Vector3 p) => new Vector2Int(
                Mathf.RoundToInt((p.x - minX) * scale),
                Mathf.RoundToInt((p.z - minZ) * scale));

            foreach (var p in backdrop) Dot(pixels, width, height, ToPx(p), 1, new Color32(70, 78, 96, 255));

            for (int r = 0; r < runs.Count; r++)
            {
                Color32 c = colors[r % colors.Count];
                int thickness = r == 0 ? 2 : 1;
                var pts = runs[r].routePoints;
                for (int i = 1; i < pts.Count; i++) Line(pixels, width, height, ToPx(pts[i - 1]), ToPx(pts[i]), thickness, c);
                for (int w = 0; w < runs[r].waypointPositions.Count; w++)
                {
                    string label = runs[r].waypointLabels[w];
                    int radius = label.StartsWith("PICK") ? 5 : 7;
                    Dot(pixels, width, height, ToPx(runs[r].waypointPositions[w]), radius, c);
                    Dot(pixels, width, height, ToPx(runs[r].waypointPositions[w]), radius - 2, new Color32(255, 255, 255, 255));
                }
            }

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "RouteMap", filterMode = FilterMode.Point };
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static void Dot(Color32[] px, int w, int h, Vector2Int c, int radius, Color32 col)
        {
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                    if (x * x + y * y <= radius * radius) Set(px, w, h, c.x + x, c.y + y, col);
        }

        private static void Line(Color32[] px, int w, int h, Vector2Int a, Vector2Int b, int thickness, Color32 col)
        {
            int dx = Mathf.Abs(b.x - a.x), dy = Mathf.Abs(b.y - a.y);
            int sx = a.x < b.x ? 1 : -1, sy = a.y < b.y ? 1 : -1;
            int err = dx - dy, x = a.x, y = a.y;
            while (true)
            {
                Dot(px, w, h, new Vector2Int(x, y), thickness, col);
                if (x == b.x && y == b.y) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }
        }

        private static void Set(Color32[] px, int w, int h, int x, int y, Color32 col)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            px[y * w + x] = col;
        }
    }
}
