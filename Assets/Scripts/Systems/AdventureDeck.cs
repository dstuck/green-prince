using CardFramework;

namespace GreenPrince
{
    public class AdventureDeck
    {
        readonly CardStack m_DrawStack;
        readonly ForestDeck m_Forest;
        readonly CampDeck m_Camp;
        readonly CardCatalog m_Catalog;
        readonly IRandomSource m_Rng;

        public int Remaining => m_DrawStack.Count;

        public AdventureDeck(ForestDeck forest, CampDeck camp, CardCatalog catalog, IRandomSource rng)
        {
            m_Forest = forest;
            m_Camp = camp;
            m_Catalog = catalog;
            m_Rng = rng;
            m_DrawStack = new CardStack("AdventureDraw");

            InitializeDrawStack();
        }

        public CardInstance Draw()
        {
            if (m_DrawStack.Count == 0)
                m_Forest.ReplenishInto(m_DrawStack);
            return m_DrawStack.Draw();
        }

        public void InjectTiles(CardDefinitionId definitionId, int count)
        {
            if (count <= 0) return;

            for (int i = 0; i < count; i++)
                m_DrawStack.Add(m_Catalog.Spawn(definitionId), StackPosition.Top);

            m_DrawStack.Shuffle(m_Rng);
        }

        void InitializeDrawStack()
        {
            var pool = new CardStack("AdventureInit");
            m_Camp.SpawnIntoStack(m_Catalog, pool);

            var forestPool = new CardStack("ForestInit");
            m_Forest.FillPool(forestPool, clearTarget: true);
            while (forestPool.Count > 0)
                pool.Add(forestPool.Draw(), StackPosition.Top);

            pool.Shuffle(m_Rng);

            while (pool.Count > 0)
                m_DrawStack.Add(pool.Draw(), StackPosition.Top);
        }
    }
}
