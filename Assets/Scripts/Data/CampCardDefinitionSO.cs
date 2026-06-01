using System.Text;
using UnityEngine;

namespace GreenPrince
{
    [CreateAssetMenu(fileName = "CampCard", menuName = "Green Prince/Camp Card Definition")]
    public class CampCardDefinitionSO : ScriptableObject
    {
        [SerializeField] string m_ShopLabel;
        [SerializeField] string m_DisplayName;
        [TextArea(2, 5)]
        [SerializeField] string m_Description;
        [SerializeField] TileDefinitionSO m_TileToAdd;
        [SerializeField] int m_CostTechnology;
        [SerializeField] int m_CostExperience;
        [SerializeField] int m_CostLore;
        [SerializeField] CampCardDefinitionSO m_NextTier;

        public string ShopLabel => string.IsNullOrEmpty(m_ShopLabel) ? DisplayName : m_ShopLabel;
        public string DisplayName => string.IsNullOrEmpty(m_DisplayName) ? name : m_DisplayName;
        public string Description => m_Description ?? string.Empty;
        public TileDefinitionSO TileToAdd => m_TileToAdd;
        public CampCardDefinitionSO NextTier => m_NextTier;

        public int GetCost(CampResourceType type)
        {
            return type switch
            {
                CampResourceType.Technology => m_CostTechnology,
                CampResourceType.Experience => m_CostExperience,
                CampResourceType.Lore => m_CostLore,
                _ => 0
            };
        }

        public bool CanAfford()
        {
            foreach (CampResourceType type in System.Enum.GetValues(typeof(CampResourceType)))
            {
                if (WorldState.GetCampResource(type) < GetCost(type))
                    return false;
            }
            return true;
        }

        public bool TryPurchase()
        {
            if (!CanAfford() || m_TileToAdd == null) return false;

            foreach (CampResourceType type in System.Enum.GetValues(typeof(CampResourceType)))
                WorldState.AddCampResource(type, -GetCost(type));

            WorldState.AddCampCard(m_TileToAdd.Id);
            return true;
        }

        public string FormatCostLine()
        {
            var sb = new StringBuilder();
            AppendCost(sb, CampResourceType.Technology, m_CostTechnology);
            AppendCost(sb, CampResourceType.Experience, m_CostExperience);
            AppendCost(sb, CampResourceType.Lore, m_CostLore);
            return sb.Length > 0 ? sb.ToString() : "Free";
        }

        static void AppendCost(StringBuilder sb, CampResourceType type, int amount)
        {
            if (amount <= 0) return;
            if (sb.Length > 0) sb.Append("  ·  ");
            sb.Append(amount).Append(' ').Append(type);
        }
    }
}
