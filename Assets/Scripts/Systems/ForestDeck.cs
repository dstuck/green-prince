using System.Collections.Generic;
using CardFramework;

namespace GreenPrince
{
    public class ForestDeck
    {
        readonly DeckRecipeSO m_ForestRecipe;
        readonly CardCatalog m_Catalog;
        readonly IRandomSource m_Rng;

        public ForestDeck(DeckRecipeSO forestRecipe, CardCatalog catalog, IRandomSource rng)
        {
            m_ForestRecipe = forestRecipe;
            m_Catalog = catalog;
            m_Rng = rng;
        }

        public void FillPool(CardStack target, bool clearTarget = true)
        {
            m_ForestRecipe.BuildStack(m_Catalog, target, clearTarget);

            foreach (var id in WorldState.ForestUnlocks)
                target.Add(m_Catalog.Spawn(id), StackPosition.Bottom);

            target.Shuffle(m_Rng);
        }

        public void ReplenishInto(CardStack adventureDraw)
        {
            var pool = new CardStack("ForestPool");
            FillPool(pool, clearTarget: true);

            while (pool.Count > 0)
                adventureDraw.Add(pool.Draw(), StackPosition.Bottom);

            adventureDraw.Shuffle(m_Rng);
        }
    }
}
