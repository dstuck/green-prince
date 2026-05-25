using CardFramework;

namespace GreenPrince
{
    public class TileState
    {
        public TerrainType Terrain { get; set; } = TerrainType.None;
        public CardInstance Card { get; set; }
        public bool IsRevealed { get; set; }
        public bool IsVisited { get; set; }
        public bool IsCamp { get; set; }
    }
}
