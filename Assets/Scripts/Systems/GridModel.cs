using System.Collections.Generic;
using CardFramework;
using UnityEngine;

namespace GreenPrince
{
    public class GridModel
    {
        readonly int m_Width;
        readonly int m_Height;
        readonly TileState[,] m_Tiles;

        public int Width => m_Width;
        public int Height => m_Height;

        public Vector2Int CampPosition { get; }

        public GridModel(int width, int height, Vector2Int campPosition)
        {
            m_Width = width;
            m_Height = height;
            m_Tiles = new TileState[width, height];
            CampPosition = campPosition;

            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var tile = new TileState();
                tile.Terrain = WorldState.GetTerrain(x, y);
                tile.IsExplored = WorldState.IsExplored(x, y);
                tile.Landmark = WorldState.GetLandmarkAt(new Vector2Int(x, y));
                m_Tiles[x, y] = tile;
            }

            var camp = m_Tiles[campPosition.x, campPosition.y];
            camp.IsCamp = true;
            camp.IsRevealed = true;
            camp.IsExplored = true;
        }

        public bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < m_Width && pos.y >= 0 && pos.y < m_Height;
        }

        public TileState GetTile(Vector2Int pos) => m_Tiles[pos.x, pos.y];

        public bool IsRevealed(Vector2Int pos) => m_Tiles[pos.x, pos.y].IsRevealed;

        public void RevealTile(Vector2Int pos, CardInstance card = null)
        {
            var tile = m_Tiles[pos.x, pos.y];
            tile.Card = card;
            tile.IsRevealed = true;
            WorldState.MarkExplored(pos.x, pos.y);
        }

        public List<Vector2Int> GetUnrevealedAdjacent(Vector2Int pos)
        {
            var results = new List<Vector2Int>(4);
            Vector2Int[] directions =
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            foreach (var dir in directions)
            {
                var adj = pos + dir;
                if (IsInBounds(adj) && !IsRevealed(adj))
                    results.Add(adj);
            }

            return results;
        }
    }
}
