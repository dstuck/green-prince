using System.Collections.Generic;
using CardFramework;

namespace GreenPrince
{
    public class CampDeck
    {
        public IReadOnlyList<CardDefinitionId> CardIds => WorldState.CampCardIds;

        public void SpawnIntoStack(CardCatalog catalog, CardStack target)
        {
            foreach (var id in CardIds)
                target.Add(catalog.Spawn(id), StackPosition.Bottom);
        }
    }
}
