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
        static List<WorldFeature> s_Features;
        static Dictionary<CampResourceType, int> s_CampResources;

        public static bool IsInitialized { get; private set; }
        public static int Width { get; private set; }
        public static int Height { get; private set; }

        public static IReadOnlyList<WorldFeature> Features => s_Features;

        public static void Initialize(int width, int height, IRandomSource rng)
        {
            if (IsInitialized) return;

            Width = width;
            Height = height;
            s_Terrain = new TerrainType[width, height];
            s_Explored = new bool[width, height];
            s_Features = new List<WorldFeature>();

            s_CampResources = new Dictionary<CampResourceType, int>();
            foreach (CampResourceType type in Enum.GetValues(typeof(CampResourceType)))
                s_CampResources[type] = 0;

            var campPos = new Vector2Int(0, height / 2);
            PlaceFeatures(width, height, campPos, rng);
            GenerateTerrain(campPos, rng);
            IsInitialized = true;
        }

        public static void Reset()
        {
            s_Terrain = null;
            s_Explored = null;
            s_Features = null;
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

        public static WorldFeature GetFeatureAt(Vector2Int pos)
        {
            foreach (var f in s_Features)
            {
                if (f.Position == pos)
                    return f;
            }
            return null;
        }

        static void GenerateTerrain(Vector2Int campPos, IRandomSource rng)
        {
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                s_Terrain[x, y] = TerrainType.Forest;

            var firstLandmark = FindFirstFeature(WorldFeatureType.Landmark);
            if (firstLandmark != null)
                CarvePath(campPos, firstLandmark.Position, rng);

            int pondCount = (Width * Height) / 18;
            for (int i = 0; i < pondCount; i++)
            {
                int px = rng.Next(0, Width);
                int py = rng.Next(0, Height);
                if (s_Terrain[px, py] == TerrainType.Forest)
                    s_Terrain[px, py] = TerrainType.River;
            }
        }

        static WorldFeature FindFirstFeature(WorldFeatureType type)
        {
            foreach (var f in s_Features)
            {
                if (f.FeatureType == type)
                    return f;
            }
            return null;
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

        static void PlaceFeatures(int width, int height, Vector2Int campPos, IRandomSource rng)
        {
            PlaceLandmarks(width, height, campPos, rng);
            PlaceHazards(width, height, campPos, rng);
        }

        static void PlaceLandmarks(int width, int height, Vector2Int campPos, IRandomSource rng)
        {
            int[][] distanceRanges = { new[] { 6, 9 }, new[] { 12, 15 } };
            var names = new[] { "Ancient Ruin", "Sacred Grove" };

            for (int i = 0; i < 2; i++)
            {
                var pos = PickPosition(width, height, campPos,
                    distanceRanges[i][0], distanceRanges[i][1], rng);
                if (pos.HasValue)
                {
                    s_Features.Add(new WorldFeature(
                        pos.Value, names[i], WorldFeatureType.Landmark,
                        ResourceType.Force, 3));
                }
            }
        }

        static void PlaceHazards(int width, int height, Vector2Int campPos, IRandomSource rng)
        {
            var pos = PickPosition(width, height, campPos, 3, 6, rng);
            if (pos.HasValue)
            {
                s_Features.Add(new WorldFeature(
                    pos.Value, "Goblin Camp", WorldFeatureType.Hazard,
                    ResourceType.Force, 2));
            }
        }

        static Vector2Int? PickPosition(int width, int height, Vector2Int campPos,
            int minDist, int maxDist, IRandomSource rng)
        {
            var candidates = new List<Vector2Int>();
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var pos = new Vector2Int(x, y);
                int dist = Mathf.Abs(x - campPos.x) + Mathf.Abs(y - campPos.y);
                if (dist >= minDist && dist <= maxDist && GetFeatureAt(pos) == null)
                    candidates.Add(pos);
            }

            if (candidates.Count == 0) return null;
            return candidates[rng.Next(0, candidates.Count)];
        }
    }
}
