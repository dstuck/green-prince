using CardFramework;

namespace GreenPrince
{
    public class LandDeck
    {
        readonly CardStack m_DrawStack;
        readonly DeckRecipeSO m_LandRecipe;
        readonly CardCatalog m_Catalog;
        readonly IRandomSource m_Rng;

        public int Remaining => m_DrawStack.Count;

        public LandDeck(DeckRecipeSO landRecipe, CardCatalog catalog, IRandomSource rng)
        {
            m_LandRecipe = landRecipe;
            m_Catalog = catalog;
            m_Rng = rng;
            m_DrawStack = new CardStack("AdventureDraw");

            Replenish();
        }

        public CardInstance Draw()
        {
            if (m_DrawStack.Count == 0)
                Replenish();
            return m_DrawStack.Draw();
        }

        public void InjectCard(CardDefinitionId id)
        {
            var instance = m_Catalog.Spawn(id);
            m_DrawStack.Add(instance, StackPosition.Top);
        }

        void Replenish()
        {
            m_LandRecipe.BuildStack(m_Catalog, m_DrawStack, clearTarget: false);
            m_DrawStack.Shuffle(m_Rng);
        }
    }
}
