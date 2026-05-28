using UnityEngine;

namespace GreenPrince
{
    public class WorldPickup
    {
        public Vector2Int Position { get; }
        public CampResourceType Type { get; }
        public bool IsCollected { get; set; }

        public WorldPickup(Vector2Int position, CampResourceType type)
        {
            Position = position;
            Type = type;
        }
    }
}

