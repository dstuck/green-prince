using System;
using System.Collections.Generic;
using CardFramework;
using UnityEngine;

namespace GreenPrince
{
    public static class WorldState
    {
        static TerrainType[,] s_Terrain;
        static bool[,] s_Explored;
        static List<LandmarkData> s_Landmarks;
        static Dictionary<CampResourceType, int> s_CampResources;

        public static bool IsInitialized { get; private set; }
        public static int Width { get; private set; }
        public static int Height { get; private set; }

        public static IReadOnlyList<LandmarkData> Landmarks => s_Landmarks;

        public static void Initialize(int width, int height, IRandomSource rng)
        {
            if (IsInitialized) return;

            Width = width;
            Height = height;
            s_Terrain = new TerrainType[width, height];
            s_Explored = new bool[width, height];
            s_Landmarks = new List<LandmarkData>();

            s_CampResources = new Dictionary<CampResourceType, int>();
            foreach (CampResourceType type in Enum.GetValues(typeof(CampResourceType)))
                s_CampResources[type] = 0;

            var campPos = new Vector2Int(0, height / 2);
            PlaceLandmarks(width, height, campPos, rng);
            GenerateTerrain(campPos, rng);
            IsInitialized = true;
        }

        public static void Reset()
        {
            s_Terrain = null;
            s_Explored = null;
            s_Landmarks = null;
            s_CampResources = null;
            IsInitialized = false;
        }

        public static TerrainType GetTerrain(int x, int y) => s_Terrain[x, y];
        public static bool IsExplored(int x, int y) => s_Explored[x, y];

        public static void MarkExplored(int x, int y)
        {
            s_Explored[x, y] = true;
        }

        public static int GetCampResource(CampResourceType type) => s_CampResources[type];

        public static void AddCampResource(CampResourceType type, int amount)
        {
            s_CampResources[type] += amount;
        }

        public static LandmarkData GetLandmarkAt(Vector2Int pos)
        {
            foreach (var lm in s_Landmarks)
            {
                if (lm.Position == pos)
                    return lm;
            }
            return null;
        }

        static void GenerateTerrain(Vector2Int campPos, IRandomSource rng)
        {
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                s_Terrain[x, y] = TerrainType.Forest;

            if (s_Landmarks.Count > 0)
                CarvePath(campPos, s_Landmarks[0].Position, rng);

            int pondCount = (Width * Height) / 18;
            for (int i = 0; i < pondCount; i++)
            {
                int px = rng.Next(0, Width);
                int py = rng.Next(0, Height);
                if (s_Terrain[px, py] == TerrainType.Forest)
                    s_Terrain[px, py] = TerrainType.River;
            }
        }

        static void CarvePath(Vector2Int from, Vector2Int to, IRandomSource rng)
        {
            var cur = from;
            while (cur != to)
            {
                s_Terrain[cur.x, cur.y] = TerrainType.Mountain;

                int dx = to.x - cur.x;
                int dy = to.y - cur.y;

                bool preferX = Mathf.Abs(dx) >= Mathf.Abs(dy);
                if (Mathf.Abs(dx) > 0 && Mathf.Abs(dy) > 0)
                    preferX = rng.Next(0, 3) > 0;

                if (preferX && dx != 0)
                    cur.x += dx > 0 ? 1 : -1;
                else if (dy != 0)
                    cur.y += dy > 0 ? 1 : -1;
                else
                    cur.x += dx > 0 ? 1 : -1;
            }
            s_Terrain[to.x, to.y] = TerrainType.Mountain;
        }

        static void PlaceLandmarks(int width, int height, Vector2Int campPos, IRandomSource rng)
        {
            int[][] distanceRanges = { new[] { 6, 9 }, new[] { 12, 15 } };
            var names = new[] { "Ancient Ruin", "Sacred Grove" };

            for (int i = 0; i < 2; i++)
            {
                int minDist = distanceRanges[i][0];
                int maxDist = distanceRanges[i][1];

                var candidates = new List<Vector2Int>();
                for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    int dist = Mathf.Abs(x - campPos.x) + Mathf.Abs(y - campPos.y);
                    if (dist >= minDist && dist <= maxDist)
                        candidates.Add(new Vector2Int(x, y));
                }

                if (candidates.Count > 0)
                {
                    int idx = rng.Next(0, candidates.Count);
                    s_Landmarks.Add(new LandmarkData(
                        candidates[idx], names[i], ResourceType.Force, 2));
                }
            }
        }
    }
}
