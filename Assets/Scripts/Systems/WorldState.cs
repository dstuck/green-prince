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
        static List<WorldPickup>[,] s_Pickups;
        static Dictionary<CampResourceType, int> s_CampResources;
        static List<CardDefinitionId> s_CampCardIds;
        static HashSet<CardDefinitionId> s_ForestUnlocks;
        static int[] s_ShopChainProgress;

        const int PickupMinDistance = 3;
        const int PickupMaxDistance = 8;

        public static bool IsInitialized { get; private set; }
        public static int Width { get; private set; }
        public static int Height { get; private set; }

        public static IReadOnlyList<WorldFeature> Features => s_Features;
        public static IReadOnlyList<CardDefinitionId> CampCardIds => s_CampCardIds;
        public static IReadOnlyCollection<CardDefinitionId> ForestUnlocks => s_ForestUnlocks;

        public static void Initialize(int width, int height, IRandomSource rng)
        {
            if (IsInitialized) return;

            Width = width;
            Height = height;
            s_Terrain = new TerrainType[width, height];
            s_Explored = new bool[width, height];
            s_Features = new List<WorldFeature>();
            s_Pickups = new List<WorldPickup>[width, height];

            if (s_CampResources == null)
            {
                s_CampResources = new Dictionary<CampResourceType, int>();
                foreach (CampResourceType type in Enum.GetValues(typeof(CampResourceType)))
                    s_CampResources[type] = 0;
            }

            if (s_CampCardIds == null)
                s_CampCardIds = new List<CardDefinitionId>();
            if (s_ForestUnlocks == null)
                s_ForestUnlocks = new HashSet<CardDefinitionId>();
            if (s_ShopChainProgress == null)
                s_ShopChainProgress = Array.Empty<int>();

            var campPos = new Vector2Int(0, height / 2);
            PlaceFeatures(width, height, campPos, rng);
            PlacePickups(width, height, campPos, rng);
            GenerateTerrain(campPos, rng);
            IsInitialized = true;
        }

        public static void Reset()
        {
            s_Terrain = null;
            s_Explored = null;
            s_Features = null;
            s_Pickups = null;
            s_CampResources = null;
            s_CampCardIds = null;
            s_ForestUnlocks = null;
            s_ShopChainProgress = null;
            IsInitialized = false;
        }

        public static void EnsureShopChains(int chainCount)
        {
            if (s_ShopChainProgress == null || s_ShopChainProgress.Length != chainCount)
            {
                var next = new int[chainCount];
                if (s_ShopChainProgress != null)
                {
                    int copy = Math.Min(s_ShopChainProgress.Length, chainCount);
                    Array.Copy(s_ShopChainProgress, next, copy);
                }
                s_ShopChainProgress = next;
            }
        }

        public static int GetShopChainProgress(int chainIndex)
        {
            if (s_ShopChainProgress == null || chainIndex < 0 || chainIndex >= s_ShopChainProgress.Length)
                return 0;
            return s_ShopChainProgress[chainIndex];
        }

        public static void AdvanceShopChain(int chainIndex)
        {
            if (s_ShopChainProgress == null || chainIndex < 0 || chainIndex >= s_ShopChainProgress.Length)
                return;
            s_ShopChainProgress[chainIndex]++;
        }

        public static void AddCampCard(CardDefinitionId id)
        {
            s_CampCardIds ??= new List<CardDefinitionId>();
            s_CampCardIds.Add(id);
        }

        public static void UnlockForestCard(CardDefinitionId id)
        {
            s_ForestUnlocks ??= new HashSet<CardDefinitionId>();
            s_ForestUnlocks.Add(id);
        }

        public static bool IsForestUnlocked(CardDefinitionId id) =>
            s_ForestUnlocks != null && s_ForestUnlocks.Contains(id);

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

        public static List<WorldPickup> GetPickupsAt(Vector2Int pos)
        {
            return s_Pickups[pos.x, pos.y];
        }

        public static bool CollectPickupsAt(Vector2Int pos)
        {
            var list = s_Pickups[pos.x, pos.y];
            if (list == null || list.Count == 0) return false;

            bool collectedAny = false;
            foreach (var p in list)
            {
                if (p.IsCollected) continue;
                p.IsCollected = true;
                AddCampResource(p.Type, 1);
                collectedAny = true;
            }
            return collectedAny;
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

            var firstLandmark = FindFeature(WorldFeatureKind.FirstLandmark);
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

        static WorldFeature FindFeature(WorldFeatureKind kind)
        {
            foreach (var f in s_Features)
            {
                if (f.Kind == kind)
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
            var kinds = new[] { WorldFeatureKind.FirstLandmark, WorldFeatureKind.SecondLandmark };

            for (int i = 0; i < 2; i++)
            {
                var pos = PickPosition(width, height, campPos,
                    distanceRanges[i][0], distanceRanges[i][1], rng);
                if (pos.HasValue)
                {
                    var feature = new WorldFeature(
                        pos.Value, names[i], kinds[i], WorldFeatureType.Landmark,
                        ResourceType.Force, 3);
                    feature.Rewards[CampResourceType.Technology] = 1;
                    feature.Rewards[CampResourceType.Experience] = 1;
                    feature.Rewards[CampResourceType.Lore] = 1;
                    s_Features.Add(feature);
                }
            }
        }

        static void PlaceHazards(int width, int height, Vector2Int campPos, IRandomSource rng)
        {
            var pos = PickPosition(width, height, campPos, 3, 6, rng);
            if (pos.HasValue)
            {
                var feature = new WorldFeature(
                    pos.Value, "Goblin Camp", WorldFeatureKind.GoblinCamp,
                    WorldFeatureType.Hazard, ResourceType.Force, 2);
                feature.Rewards[CampResourceType.Experience] = 1;
                feature.Rewards[CampResourceType.Lore] = 1;
                s_Features.Add(feature);
            }
        }

        static void PlacePickups(int width, int height, Vector2Int campPos, IRandomSource rng)
        {
            PlacePickupType(width, height, campPos, rng, CampResourceType.Technology, count: 2,
                PickupMinDistance, PickupMaxDistance);
            PlacePickupType(width, height, campPos, rng, CampResourceType.Experience, count: 1,
                PickupMinDistance, PickupMaxDistance);
            PlacePickupType(width, height, campPos, rng, CampResourceType.Lore, count: 1,
                PickupMinDistance, PickupMaxDistance);
        }

        static void PlacePickupType(int width, int height, Vector2Int campPos, IRandomSource rng,
            CampResourceType type, int count, int minDist, int maxDist)
        {
            int attempts = 0;
            int placed = 0;
            while (placed < count && attempts < 200)
            {
                attempts++;
                var pos = PickPosition(width, height, campPos, minDist, maxDist, rng);
                if (!pos.HasValue) break;

                if (GetFeatureAt(pos.Value) != null) continue;

                var list = s_Pickups[pos.Value.x, pos.Value.y];
                if (list == null)
                {
                    list = new List<WorldPickup>();
                    s_Pickups[pos.Value.x, pos.Value.y] = list;
                }

                bool hasSame = false;
                foreach (var p in list)
                {
                    if (p.Type == type) { hasSame = true; break; }
                }
                if (hasSame) continue;

                list.Add(new WorldPickup(pos.Value, type));
                placed++;
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
