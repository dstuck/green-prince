using CardFramework;

namespace GreenPrince
{
    /// <summary>
    /// Passive abilities granted by owned camp cards (summed across the camp deck).
    /// </summary>
    public static class PartyAbilities
    {
        public static int GetIntimidateLevel(CardDefinitionRegistry registry)
        {
            int total = 0;
            foreach (var id in WorldState.GetActiveLoadout())
            {
                if (registry.Get(id) is TileDefinitionSO def && def.IntimidateLevel > 0)
                    total += def.IntimidateLevel;
                else if (id.Value == "tile.masks")
                    total += 1;
            }
            return total;
        }

        public static int GetTinkererLevel(CardDefinitionRegistry registry)
        {
            int total = 0;
            foreach (var id in WorldState.GetActiveLoadout())
            {
                if (registry.Get(id) is TileDefinitionSO def && def.TinkererLevel > 0)
                    total += def.TinkererLevel;
                else if (id.Value == "tile.tinker_bag")
                    total += 1;
            }
            return total;
        }

        public static int GetMagicLevel(CardDefinitionRegistry registry)
        {
            int total = 0;
            foreach (var id in WorldState.GetActiveLoadout())
            {
                if (registry.Get(id) is TileDefinitionSO def && def.MagicLevel > 0)
                    total += def.MagicLevel;
            }
            return total;
        }
    }
}
