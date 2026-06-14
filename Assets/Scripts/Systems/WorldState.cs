using System;
using System.Collections.Generic;
using CardFramework;
using UnityEngine;

namespace GreenPrince
{
    public static class WorldState
    {
        const int LoadoutMaxSize = 4;
        const int PickupMinDistance = 3;
        const int PickupMaxDistance = 8;
        const int PostMigrationColumnSpan = 10;
        const int FirstLandmarkMinDist = 8;
        const int FirstLandmarkMaxDist = 9;
        const int SecondLandmarkMinDist = 12;
        const int SecondLandmarkMaxDist = 15;
        const int SecondLandmarkMinDistFromFirst = 6;

        static TerrainType[,] s_Terrain;
        static bool[,] s_Explored;
        static List<WorldFeature> s_Features;
        static List<WorldPickup>[,] s_Pickups;
        static Dictionary<CampResourceType, int> s_CampResources;
        static List<CardDefinitionId> s_CampCardIds;
        static List<CardDefinitionId> s_LoadoutCardIds;
        static HashSet<CardDefinitionId> s_ForestUnlocks;
        static int[] s_ShopChainProgress;
        static int[] s_LandmarkShopChainProgress;

        static Vector2Int s_CampPosition;
        static bool s_HasCampPosition;
        static bool s_HasMigrated;
        static bool s_HasSpawnedPostMigrationPickups;
        static bool s_LoadoutNeedsAttention;
        static bool s_CaravanActive;
        static WorldFeatureKind s_CaravanTarget;

        static readonly List<CardDefinitionId> s_LoadoutReadOnlyBuffer = new();

        public static bool IsInitialized { get; private set; }
        public static int Width { get; private set; }
        public static int Height { get; private set; }

        public static IReadOnlyList<WorldFeature> Features => s_Features;
        public static IReadOnlyList<CardDefinitionId> CampCardIds => s_CampCardIds;
        public static IReadOnlyList<CardDefinitionId> LoadoutCardIds => s_LoadoutCardIds;
        public static IReadOnlyCollection<CardDefinitionId> ForestUnlocks => s_ForestUnlocks;
        public static Vector2Int CampPosition =>
            s_HasCampPosition ? s_CampPosition : new Vector2Int(0, Height > 0 ? Height / 2 : 0);
        public static bool HasMigrated => s_HasMigrated;
        public static bool LoadoutNeedsAttention => s_LoadoutNeedsAttention;
        public static bool IsCaravanActive => s_CaravanActive;
        public static WorldFeatureKind CaravanTarget => s_CaravanTarget;

        public static bool CanMoveCamp =>
            (s_CampCardIds?.Count ?? 0) >= LoadoutMaxSize && !s_HasMigrated;

        public static IReadOnlyList<CardDefinitionId> GetActiveLoadout()
        {
            s_LoadoutReadOnlyBuffer.Clear();
            int owned = s_CampCardIds?.Count ?? 0;

            if (s_LoadoutCardIds != null && s_LoadoutCardIds.Count > 0)
                s_LoadoutReadOnlyBuffer.AddRange(s_LoadoutCardIds);
            else if (owned > 0 && owned < LoadoutMaxSize && s_CampCardIds != null)
                s_LoadoutReadOnlyBuffer.AddRange(s_CampCardIds);

            return s_LoadoutReadOnlyBuffer;
        }

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
            if (s_LoadoutCardIds == null)
                s_LoadoutCardIds = new List<CardDefinitionId>();
            if (s_ForestUnlocks == null)
                s_ForestUnlocks = new HashSet<CardDefinitionId>();
            if (s_ShopChainProgress == null)
                s_ShopChainProgress = Array.Empty<int>();

            if (!s_HasCampPosition)
            {
                s_CampPosition = new Vector2Int(0, height / 2);
                s_HasCampPosition = true;
            }

            var campPos = s_CampPosition;
            PlaceFeatures(width, height, campPos, rng);
            PlacePickups(width, height, campPos, rng);
            GenerateTerrain(campPos, rng);
            SyncLoadout();
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
            s_LoadoutCardIds = null;
            s_ForestUnlocks = null;
            s_ShopChainProgress = null;
            s_LandmarkShopChainProgress = null;
            s_HasCampPosition = false;
            s_HasMigrated = false;
            s_HasSpawnedPostMigrationPickups = false;
            s_LoadoutNeedsAttention = false;
            s_CaravanActive = false;
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

        public static void EnsureLandmarkShopChains(int chainCount)
        {
            if (s_LandmarkShopChainProgress == null || s_LandmarkShopChainProgress.Length != chainCount)
            {
                var next = new int[chainCount];
                if (s_LandmarkShopChainProgress != null)
                {
                    int copy = Math.Min(s_LandmarkShopChainProgress.Length, chainCount);
                    Array.Copy(s_LandmarkShopChainProgress, next, copy);
                }
                s_LandmarkShopChainProgress = next;
            }
        }

        public static int GetLandmarkShopChainProgress(int chainIndex)
        {
            if (s_LandmarkShopChainProgress == null || chainIndex < 0 || chainIndex >= s_LandmarkShopChainProgress.Length)
                return 0;
            return s_LandmarkShopChainProgress[chainIndex];
        }

        public static void AdvanceLandmarkShopChain(int chainIndex)
        {
            if (s_LandmarkShopChainProgress == null || chainIndex < 0 || chainIndex >= s_LandmarkShopChainProgress.Length)
                return;
            s_LandmarkShopChainProgress[chainIndex]++;
        }

        public static void AddCampCard(CardDefinitionId id)
        {
            s_CampCardIds ??= new List<CardDefinitionId>();
            int ownedBefore = s_CampCardIds.Count;
            s_CampCardIds.Add(id);
            SyncLoadout(ownedBefore);
        }

        public static void SyncLoadout(int ownedBefore = -1)
        {
            s_LoadoutCardIds ??= new List<CardDefinitionId>();
            int owned = s_CampCardIds?.Count ?? 0;

            if (owned < LoadoutMaxSize)
            {
                s_LoadoutCardIds.Clear();
                s_LoadoutNeedsAttention = false;
                return;
            }

            if (owned == LoadoutMaxSize)
            {
                s_LoadoutCardIds.Clear();
                s_LoadoutCardIds.AddRange(s_CampCardIds);
                s_LoadoutNeedsAttention = false;
                return;
            }

            TrimInvalidFromLoadout();

            if (ownedBefore >= 0 && ownedBefore < owned)
            {
                var newest = s_CampCardIds[owned - 1];
                if (s_LoadoutCardIds.Count == LoadoutMaxSize && !s_LoadoutCardIds.Contains(newest))
                    s_LoadoutNeedsAttention = true;
            }
        }

        public static bool IsInLoadout(CardDefinitionId id) =>
            s_LoadoutCardIds != null && s_LoadoutCardIds.Contains(id);

        public static bool ToggleLoadoutCard(CardDefinitionId id)
        {
            if (!OwnsCampCard(id)) return false;

            s_LoadoutCardIds ??= new List<CardDefinitionId>();

            if (s_LoadoutCardIds.Contains(id))
            {
                s_LoadoutCardIds.Remove(id);
                return true;
            }

            if (s_LoadoutCardIds.Count >= LoadoutMaxSize) return false;

            s_LoadoutCardIds.Add(id);
            if (s_LoadoutCardIds.Count == LoadoutMaxSize)
                s_LoadoutNeedsAttention = false;
            return true;
        }

        public static bool TrySetLoadoutSelection(IReadOnlyList<CardDefinitionId> ids)
        {
            if (ids == null || ids.Count > LoadoutMaxSize) return false;

            var seen = new HashSet<CardDefinitionId>();
            foreach (var id in ids)
            {
                if (!OwnsCampCard(id) || !seen.Add(id))
                    return false;
            }

            s_LoadoutCardIds ??= new List<CardDefinitionId>();
            s_LoadoutCardIds.Clear();
            s_LoadoutCardIds.AddRange(ids);
            s_LoadoutNeedsAttention = false;
            return true;
        }

        public static void ClearLoadoutAttention() => s_LoadoutNeedsAttention = false;

        public static void BeginCaravan(WorldFeatureKind target)
        {
            s_CaravanActive = true;
            s_CaravanTarget = target;
        }

        public static void EndCaravan() => s_CaravanActive = false;

        public static void SetCampPosition(Vector2Int position)
        {
            s_CampPosition = position;
            s_HasCampPosition = true;
        }

        public static void CompleteMigration(Vector2Int newCampPosition)
        {
            SetCampPosition(newCampPosition);
            s_HasMigrated = true;
            EndCaravan();
            SpawnPostMigrationPickupsIfNeeded();
        }

        public static bool UsesLandmarkShopChain(int chainIndex, CampShopCatalogSO catalog)
        {
            if (!s_HasMigrated || catalog == null) return false;
            if (chainIndex < 0 || chainIndex >= catalog.ChainCount) return false;
            return catalog.GetChainLandmarkRoot(chainIndex) != null;
        }

        public static bool HasShopPurchaseOnOtherPrimaryChain(int chainIndex)
        {
            if (s_ShopChainProgress == null) return false;

            int count = Mathf.Min(PrimaryLandmarkChainCount, s_ShopChainProgress.Length);
            for (int i = 0; i < count; i++)
            {
                if (i == chainIndex) continue;
                if (s_ShopChainProgress[i] > 0)
                    return true;
            }

            return false;
        }

        public static bool IsLandmarkBonusShopUnlocked()
        {
            if (!s_HasMigrated || s_LandmarkShopChainProgress == null)
                return false;

            int count = Mathf.Min(PrimaryLandmarkChainCount, s_LandmarkShopChainProgress.Length);
            for (int i = 0; i < count; i++)
            {
                if (s_LandmarkShopChainProgress[i] > 0)
                    return true;
            }

            return false;
        }

        const int PrimaryLandmarkChainCount = 3;

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

        public static WorldFeature FindFeature(WorldFeatureKind kind)
        {
            foreach (var f in s_Features)
            {
                if (f.Kind == kind)
                    return f;
            }
            return null;
        }

        public static bool TryGetCaravanGoalPosition(out Vector2Int position)
        {
            position = default;
            if (!s_CaravanActive) return false;
            var feature = FindFeature(s_CaravanTarget);
            if (feature == null) return false;
            position = feature.Position;
            return true;
        }

        static bool OwnsCampCard(CardDefinitionId id) =>
            s_CampCardIds != null && s_CampCardIds.Contains(id);

        static void TrimInvalidFromLoadout()
        {
            for (int i = s_LoadoutCardIds.Count - 1; i >= 0; i--)
            {
                if (!OwnsCampCard(s_LoadoutCardIds[i]))
                    s_LoadoutCardIds.RemoveAt(i);
            }
        }

        static void GenerateTerrain(Vector2Int campPos, IRandomSource rng)
        {
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                s_Terrain[x, y] = TerrainType.Forest;

            var firstLandmark = FindFeature(WorldFeatureKind.FirstLandmark);
            if (firstLandmark != null)
                CarvePath(campPos, firstLandmark.Position, rng);

            var secondLandmark = FindFeature(WorldFeatureKind.SecondLandmark);
            if (firstLandmark != null && secondLandmark != null)
                CarveBrokenPath(firstLandmark.Position, secondLandmark.Position, rng);

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

                cur = StepToward(cur, to, rng);
            }
            s_Terrain[to.x, to.y] = TerrainType.Mountain;
        }

        /// <summary>
        /// Partial abandoned segment from first toward second landmark: 3 cells, gap, 1 cell, not connected.
        /// </summary>
        static void CarveBrokenPath(Vector2Int from, Vector2Int to, IRandomSource rng)
        {
            var cur = from;
            if (cur == to) return;

            for (int i = 0; i < 3; i++)
            {
                cur = StepToward(cur, to, rng);
                if (cur == to) return;
                s_Terrain[cur.x, cur.y] = TerrainType.Mountain;
            }

            cur = StepToward(cur, to, rng);
            if (cur == to) return;

            cur = StepToward(cur, to, rng);
            if (cur == to) return;
            s_Terrain[cur.x, cur.y] = TerrainType.Mountain;
        }

        static Vector2Int StepToward(Vector2Int cur, Vector2Int to, IRandomSource rng)
        {
            int dx = to.x - cur.x;
            int dy = to.y - cur.y;

            bool preferX = Mathf.Abs(dx) >= Mathf.Abs(dy);
            if (Mathf.Abs(dx) > 0 && Mathf.Abs(dy) > 0)
                preferX = rng.Next(0, 3) > 0;

            if (preferX && dx != 0)
                return new Vector2Int(cur.x + (dx > 0 ? 1 : -1), cur.y);
            if (dy != 0)
                return new Vector2Int(cur.x, cur.y + (dy > 0 ? 1 : -1));
            return new Vector2Int(cur.x + (dx > 0 ? 1 : -1), cur.y);
        }

        static void PlaceFeatures(int width, int height, Vector2Int campPos, IRandomSource rng)
        {
            PlaceLandmarks(width, height, campPos, rng);
            PlaceHazards(width, height, campPos, rng);
        }

        static void PlaceLandmarks(int width, int height, Vector2Int campPos, IRandomSource rng)
        {
            var firstPos = PickLandmarkPosition(width, height, campPos,
                FirstLandmarkMinDist, FirstLandmarkMaxDist, campPos.x + 1, rng);
            if (!firstPos.HasValue) return;

            AddLandmark(firstPos.Value, "Ancient Ruin", WorldFeatureKind.FirstLandmark);

            var secondPos = PickSecondLandmarkPosition(width, height, campPos, firstPos.Value, rng);
            if (secondPos.HasValue)
                AddLandmark(secondPos.Value, "Sacred Grove", WorldFeatureKind.SecondLandmark);
        }

        static void AddLandmark(Vector2Int pos, string name, WorldFeatureKind kind)
        {
            var feature = new WorldFeature(
                pos, name, kind, WorldFeatureType.Landmark,
                ResourceType.Force, 3);
            feature.Rewards[CampResourceType.Technology] = 1;
            feature.Rewards[CampResourceType.Experience] = 1;
            feature.Rewards[CampResourceType.Lore] = 1;
            s_Features.Add(feature);
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
                PickupMinDistance, PickupMaxDistance, allowDuplicateTypesOnTile: false);
            PlacePickupType(width, height, campPos, rng, CampResourceType.Experience, count: 1,
                PickupMinDistance, PickupMaxDistance, allowDuplicateTypesOnTile: false);
            PlacePickupType(width, height, campPos, rng, CampResourceType.Lore, count: 1,
                PickupMinDistance, PickupMaxDistance, allowDuplicateTypesOnTile: false);
        }

        public static void SpawnPostMigrationPickupsIfNeeded()
        {
            if (s_HasSpawnedPostMigrationPickups) return;

            var second = FindFeature(WorldFeatureKind.SecondLandmark);
            if (second == null) return;

            var first = FindFeature(WorldFeatureKind.FirstLandmark);
            int anchorX = first != null
                ? Mathf.Max(first.Position.x, second.Position.x)
                : second.Position.x;

            int startX = anchorX + 1;
            int endX = Mathf.Min(Width - 1, anchorX + PostMigrationColumnSpan);
            if (startX > endX) return;

            int columnCount = endX - startX + 1;
            var rng = new SystemRandomSource();
            SpawnPostMigrationType(CampResourceType.Technology, 3, startX, endX, columnCount, rng);
            SpawnPostMigrationType(CampResourceType.Experience, 3, startX, endX, columnCount, rng);
            SpawnPostMigrationType(CampResourceType.Lore, 3, startX, endX, columnCount, rng);

            s_HasSpawnedPostMigrationPickups = true;
        }

        static void SpawnPostMigrationType(CampResourceType type, int count,
            int startX, int endX, int columnCount, IRandomSource rng)
        {
            for (int i = 0; i < count; i++)
            {
                int col = startX + (i * columnCount / count);
                col = Mathf.Clamp(col, startX, endX);

                int attempts = 0;
                while (attempts < 40)
                {
                    attempts++;
                    int y = rng.Next(0, Height);
                    var pos = new Vector2Int(col, y);
                    if (GetFeatureAt(pos) != null) continue;

                    AddPickupAt(pos, type);
                    break;
                }
            }
        }

        static void AddPickupAt(Vector2Int pos, CampResourceType type)
        {
            var list = s_Pickups[pos.x, pos.y];
            if (list == null)
            {
                list = new List<WorldPickup>();
                s_Pickups[pos.x, pos.y] = list;
            }
            list.Add(new WorldPickup(pos, type));
        }

        static void PlacePickupType(int width, int height, Vector2Int campPos, IRandomSource rng,
            CampResourceType type, int count, int minDist, int maxDist, bool allowDuplicateTypesOnTile)
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

                if (!allowDuplicateTypesOnTile)
                {
                    bool hasSame = false;
                    foreach (var p in list)
                    {
                        if (p.Type == type) { hasSame = true; break; }
                    }
                    if (hasSame) continue;
                }

                list.Add(new WorldPickup(pos.Value, type));
                placed++;
            }
        }

        static Vector2Int? PickLandmarkPosition(int width, int height, Vector2Int origin,
            int minDist, int maxDist, int minX, IRandomSource rng)
        {
            var candidates = new List<Vector2Int>();
            for (int x = minX; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var pos = new Vector2Int(x, y);
                int dist = Mathf.Abs(x - origin.x) + Mathf.Abs(y - origin.y);
                if (dist >= minDist && dist <= maxDist && GetFeatureAt(pos) == null)
                    candidates.Add(pos);
            }

            if (candidates.Count == 0) return null;
            return candidates[rng.Next(0, candidates.Count)];
        }

        static Vector2Int? PickSecondLandmarkPosition(int width, int height, Vector2Int campPos,
            Vector2Int firstPos, IRandomSource rng)
        {
            int minX = firstPos.x + 1;
            var candidates = new List<Vector2Int>();
            for (int x = minX; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var pos = new Vector2Int(x, y);
                int distFromCamp = Mathf.Abs(x - campPos.x) + Mathf.Abs(y - campPos.y);
                int distFromFirst = Mathf.Abs(x - firstPos.x) + Mathf.Abs(y - firstPos.y);
                if (distFromCamp >= SecondLandmarkMinDist && distFromCamp <= SecondLandmarkMaxDist
                    && distFromFirst >= SecondLandmarkMinDistFromFirst
                    && GetFeatureAt(pos) == null)
                    candidates.Add(pos);
            }

            if (candidates.Count == 0) return null;
            return candidates[rng.Next(0, candidates.Count)];
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
