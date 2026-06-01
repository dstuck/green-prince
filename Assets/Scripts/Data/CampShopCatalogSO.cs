using System;
using UnityEngine;

namespace GreenPrince
{
    [Serializable]
    public struct CampShopChain
    {
        public CampCardDefinitionSO Root;
    }

    [CreateAssetMenu(fileName = "CampShopCatalog", menuName = "Green Prince/Camp Shop Catalog")]
    public class CampShopCatalogSO : ScriptableObject
    {
        [SerializeField] CampShopChain[] m_Chains;

        public int ChainCount => m_Chains?.Length ?? 0;

        public CampCardDefinitionSO GetCurrentOffer(int chainIndex)
        {
            if (m_Chains == null || chainIndex < 0 || chainIndex >= m_Chains.Length)
                return null;

            var card = m_Chains[chainIndex].Root;
            if (card == null) return null;

            int purchased = WorldState.GetShopChainProgress(chainIndex);
            for (int i = 0; i < purchased; i++)
            {
                card = card.NextTier;
                if (card == null) return null;
            }
            return card;
        }

        public void GetTier1TotalCosts(out int technology, out int experience, out int lore)
        {
            technology = experience = lore = 0;
            if (m_Chains == null) return;

            foreach (var chain in m_Chains)
            {
                var card = chain.Root;
                if (card == null) continue;
                technology += card.GetCost(CampResourceType.Technology);
                experience += card.GetCost(CampResourceType.Experience);
                lore += card.GetCost(CampResourceType.Lore);
            }
        }
    }
}
