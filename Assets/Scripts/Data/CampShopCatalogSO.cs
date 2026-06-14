using System;
using System.Collections.Generic;
using UnityEngine;

namespace GreenPrince
{
    [Serializable]
    public struct CampShopChain
    {
        public CampCardDefinitionSO Root;
        public CampCardDefinitionSO LandmarkRoot;
    }

    public readonly struct VisibleShopOffer
    {
        public int ChainIndex { get; }
        public CampCardDefinitionSO Card { get; }

        public VisibleShopOffer(int chainIndex, CampCardDefinitionSO card)
        {
            ChainIndex = chainIndex;
            Card = card;
        }
    }

    [CreateAssetMenu(fileName = "CampShopCatalog", menuName = "Green Prince/Camp Shop Catalog")]
    public class CampShopCatalogSO : ScriptableObject
    {
        public const int MaxVisibleOffers = 3;
        public const int PrimaryChainCount = 3;

        [SerializeField] CampShopChain[] m_Chains;

        public int ChainCount => m_Chains?.Length ?? 0;

        public CampCardDefinitionSO GetChainLandmarkRoot(int chainIndex)
        {
            if (m_Chains == null || chainIndex < 0 || chainIndex >= m_Chains.Length)
                return null;
            return m_Chains[chainIndex].LandmarkRoot;
        }

        public IReadOnlyList<VisibleShopOffer> GetVisibleOffers()
        {
            var offers = new List<VisibleShopOffer>(MaxVisibleOffers);
            if (m_Chains == null) return offers;

            foreach (int chainIndex in GetEligibleChainOrder())
            {
                if (offers.Count >= MaxVisibleOffers) break;

                var offer = GetCurrentOffer(chainIndex);
                if (offer != null)
                    offers.Add(new VisibleShopOffer(chainIndex, offer));
            }

            return offers;
        }

        public CampCardDefinitionSO GetCurrentOffer(int chainIndex)
        {
            if (m_Chains == null || chainIndex < 0 || chainIndex >= m_Chains.Length)
                return null;

            var chain = m_Chains[chainIndex];

            if (chainIndex >= PrimaryChainCount && !WorldState.IsLandmarkBonusShopUnlocked())
                return null;

            if (WorldState.HasMigrated)
            {
                if (chain.LandmarkRoot == null)
                    return null;
                return WalkChainTier(chain.LandmarkRoot, WorldState.GetLandmarkShopChainProgress(chainIndex));
            }

            if (chain.Root == null)
                return null;

            int progress = WorldState.GetShopChainProgress(chainIndex);
            var offer = GetBaseChainOffer(chain, progress);
            if (offer == null)
                return null;

            if (progress >= 1 && !WorldState.HasShopPurchaseOnOtherPrimaryChain(chainIndex))
                return null;

            return offer;
        }

        IEnumerable<int> GetEligibleChainOrder()
        {
            if (WorldState.HasMigrated)
            {
                for (int i = 0; i < PrimaryChainCount; i++)
                    yield return i;

                if (!WorldState.IsLandmarkBonusShopUnlocked() || m_Chains == null)
                    yield break;

                for (int i = PrimaryChainCount; i < m_Chains.Length; i++)
                    yield return i;
                yield break;
            }

            for (int i = 0; i < PrimaryChainCount; i++)
                yield return i;
        }

        CampCardDefinitionSO GetBaseChainOffer(CampShopChain chain, int purchased)
        {
            var card = chain.Root;
            if (card == null) return null;

            for (int i = 0; i < purchased; i++)
            {
                var next = card.NextTier;
                if (next == null || next == chain.LandmarkRoot || IsAnyLandmarkRoot(next))
                    return null;
                card = next;
            }

            return card;
        }

        bool IsAnyLandmarkRoot(CampCardDefinitionSO card)
        {
            if (card == null || m_Chains == null) return false;
            foreach (var chain in m_Chains)
            {
                if (chain.LandmarkRoot == card)
                    return true;
            }
            return false;
        }

        static CampCardDefinitionSO WalkChainTier(CampCardDefinitionSO root, int purchased)
        {
            var card = root;
            if (card == null) return null;

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

            int count = Mathf.Min(PrimaryChainCount, m_Chains.Length);
            for (int i = 0; i < count; i++)
            {
                var card = m_Chains[i].Root;
                if (card == null) continue;
                technology += card.GetCost(CampResourceType.Technology);
                experience += card.GetCost(CampResourceType.Experience);
                lore += card.GetCost(CampResourceType.Lore);
            }
        }
    }
}
