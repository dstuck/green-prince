using System.Collections.Generic;
using CardFramework;

namespace GreenPrince
{
    public class CampDeck
    {
        public void SpawnIntoStack(CardCatalog catalog, CardStack target)
        {
            foreach (var id in WorldState.GetActiveLoadout())
                target.Add(catalog.Spawn(id), StackPosition.Bottom);
        }
    }
}
