using TMPro;
using UnityEngine;

namespace GreenPrince
{
    public class TileView : MonoBehaviour
    {
        static readonly Color UnexploredColor = new Color(0.15f, 0.15f, 0.2f);
        static readonly Color CampBaselineColor = new Color(0.3f, 0.55f, 0.3f);
        static Sprite s_CampTileSprite;
        static readonly Color LandmarkColor = new Color(0.85f, 0.65f, 0.9f);
        static readonly Color HazardColor = new Color(0.8f, 0.35f, 0.3f);
        static readonly Color CaravanGoalColor = new Color(1f, 0.88f, 0.25f);

        SpriteRenderer m_Renderer;
        Sprite m_DefaultTileSprite;
        Vector3 m_DefaultIconScale;
        bool m_IsCaravanGoal;
        TextMeshPro m_Label;
        SpriteRenderer m_ChallengeRenderer;
        TextMeshPro m_ChallengeLabel;
        SpriteRenderer m_BenefitRenderer;
        TextMeshPro m_BenefitLabel;
        SpriteRenderer m_IconRenderer;
        SpriteRenderer[] m_PickupRenderers;

        public Vector2Int GridPosition { get; private set; }

        public static void SetCampTileSprite(Sprite campTileSprite) => s_CampTileSprite = campTileSprite;

        public void SetCaravanGoalHighlight(bool isGoal) => m_IsCaravanGoal = isGoal;

        public void ShowCaravanGoalUnrevealed()
        {
            UseDefaultTileSprite();
            m_Renderer.color = CaravanGoalColor;
            m_Label.text = "★ GOAL";
            m_Label.fontSize = 2.6f;
            m_Label.color = Color.white;
            HideChallenge();
            HideBenefit();
            HideIcon();
            HidePickups();
        }

        public void Initialize(Vector2Int gridPosition)
        {
            GridPosition = gridPosition;

            m_Renderer = GetComponent<SpriteRenderer>();
            m_DefaultTileSprite = m_Renderer.sprite;
            m_Label = transform.Find("Label").GetComponent<TextMeshPro>();

            var challengeGo = transform.Find("ChallengeIndicator");
            m_ChallengeRenderer = challengeGo.GetComponent<SpriteRenderer>();
            m_ChallengeLabel = challengeGo.GetComponentInChildren<TextMeshPro>();

            var benefitGo = transform.Find("BenefitIndicator");
            m_BenefitRenderer = benefitGo.GetComponent<SpriteRenderer>();
            m_BenefitLabel = benefitGo.GetComponentInChildren<TextMeshPro>();

            m_IconRenderer = transform.Find("TileIcon").GetComponent<SpriteRenderer>();
            m_DefaultIconScale = m_IconRenderer.transform.localScale;

            var pickupContainer = transform.Find("PickupContainer");
            m_PickupRenderers = pickupContainer.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

            ShowUnexplored();
        }

        public void ShowUnexplored()
        {
            UseDefaultTileSprite();
            m_Renderer.color = UnexploredColor;
            m_Label.text = "";
            HideChallenge();
            HideBenefit();
            HideIcon();
            HidePickups();
        }

        public void ShowExploredFog(TerrainType terrain, WorldFeature feature,
            System.Collections.Generic.List<WorldPickup> pickups)
        {
            UseDefaultTileSprite();
            m_Renderer.color = GetTerrainFogColor(terrain);
            HideChallenge();
            HideBenefit();
            HideIcon();
            ShowPickups(pickups);

            if (feature != null)
            {
                m_Label.text = "?";
                m_Label.fontSize = 4f;
                m_Label.color = GetFeatureAccentColor(feature.FeatureType);
            }
            else
            {
                m_Label.text = "";
            }

            ApplyGoalAccent();
        }

        public void ShowCamp()
        {
            UseDefaultTileSprite();
            m_Renderer.color = CampBaselineColor;
            m_Label.text = "";
            HideChallenge();
            HideBenefit();
            HidePickups();

            if (s_CampTileSprite != null)
            {
                m_IconRenderer.sprite = s_CampTileSprite;
                m_IconRenderer.color = Color.white;
                m_IconRenderer.transform.localScale = m_DefaultIconScale * 2.2f;
                m_IconRenderer.enabled = true;
            }
            else
            {
                HideIcon();
            }
        }

        public void ShowRevealed(TileDefinitionSO definition, TileInstanceState state,
            bool visited, TerrainType terrain, WorldFeature feature,
            System.Collections.Generic.List<WorldPickup> pickups)
        {
            UseDefaultTileSprite();
            m_Renderer.color = GetTerrainColor(terrain);
            ShowPickups(pickups);
            HideIcon();

            if (feature != null)
            {
                m_Label.text = feature.Name;
                m_Label.fontSize = 2.5f;
                m_Label.color = visited
                    ? new Color(1f, 1f, 1f, 0.5f)
                    : GetFeatureAccentColor(feature.FeatureType);

                if (!visited && feature.HasActiveChallenge)
                    ShowChallenge(ResourceColors.Get(feature.ChallengeType), feature.ChallengeValue);
                else
                    HideChallenge();
                HideBenefit();
                ApplyGoalAccent();
                return;
            }

            if (definition != null && definition.DisplayIconType != TileIconType.None)
                ShowIcon(definition.DisplayIconType);

            m_Label.text = definition != null && definition.ShowTileLabel
                ? definition.BoardDisplayName
                : "";

            if (visited || definition == null)
            {
                HideChallenge();
                HideBenefit();
                return;
            }

            int value = (state != null && state.HasModifiedChallenge)
                ? state.ModifiedChallengeValue
                : definition.ChallengeValue;

            if (value > 0)
                ShowChallenge(ResourceColors.Get(definition.ResourceType), value);
            else
                HideChallenge();

            if (definition.HasReward)
                ShowBenefit(ResourceColors.Get(definition.RewardType), definition.RewardAmount);
            else
                HideBenefit();

            ApplyGoalAccent();
        }

        void UseDefaultTileSprite()
        {
            if (m_DefaultTileSprite != null)
                m_Renderer.sprite = m_DefaultTileSprite;
        }

        void ApplyGoalAccent()
        {
            if (!m_IsCaravanGoal) return;
            m_Renderer.color = CaravanGoalColor;
            m_Label.text = string.IsNullOrEmpty(m_Label.text) ? "★ GOAL" : $"{m_Label.text}\n★ GOAL";
            m_Label.fontSize = Mathf.Max(m_Label.fontSize, 2.4f);
            m_Label.color = Color.white;
        }

        void ShowChallenge(Color color, int value)
        {
            m_ChallengeRenderer.enabled = true;
            m_ChallengeRenderer.color = color;
            m_ChallengeLabel.text = $"-{value}";
            m_ChallengeLabel.enabled = true;
        }

        void HideChallenge()
        {
            m_ChallengeRenderer.enabled = false;
            m_ChallengeLabel.text = "";
            m_ChallengeLabel.enabled = false;
        }

        void ShowBenefit(Color color, int value)
        {
            m_BenefitRenderer.enabled = true;
            m_BenefitRenderer.color = color;
            m_BenefitLabel.text = $"+{value}";
            m_BenefitLabel.enabled = true;
        }

        void HideBenefit()
        {
            m_BenefitRenderer.enabled = false;
            m_BenefitLabel.text = "";
            m_BenefitLabel.enabled = false;
        }

        void ShowIcon(TileIconType iconType)
        {
            var sprite = TileIconSprites.Get(iconType);
            if (sprite == null) return;
            m_IconRenderer.sprite = sprite;
            m_IconRenderer.enabled = true;
        }

        void HideIcon()
        {
            m_IconRenderer.enabled = false;
            m_IconRenderer.transform.localScale = m_DefaultIconScale;
        }

        void ShowPickups(System.Collections.Generic.List<WorldPickup> pickups)
        {
            if (m_PickupRenderers == null || m_PickupRenderers.Length == 0) return;

            for (int i = 0; i < m_PickupRenderers.Length; i++)
                m_PickupRenderers[i].enabled = false;

            if (pickups == null || pickups.Count == 0) return;

            int shown = 0;
            foreach (var p in pickups)
            {
                if (p == null || p.IsCollected) continue;
                if (shown >= m_PickupRenderers.Length) break;
                var sr = m_PickupRenderers[shown++];
                sr.color = CampResourceColors.Get(p.Type);
                sr.enabled = true;
            }
        }

        void HidePickups()
        {
            if (m_PickupRenderers == null) return;
            for (int i = 0; i < m_PickupRenderers.Length; i++)
                m_PickupRenderers[i].enabled = false;
        }

        static Color GetFeatureAccentColor(WorldFeatureType type)
        {
            return type switch
            {
                WorldFeatureType.Landmark => LandmarkColor,
                WorldFeatureType.Hazard   => HazardColor,
                _                         => Color.white,
            };
        }

        static Color GetTerrainColor(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Forest   => new Color(0.3f, 0.55f, 0.3f),
                TerrainType.River    => new Color(0.3f, 0.45f, 0.7f),
                TerrainType.Mountain => new Color(0.55f, 0.48f, 0.4f),
                _                    => new Color(0.4f, 0.4f, 0.4f),
            };
        }

        static Color GetTerrainFogColor(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Forest   => new Color(0.2f, 0.32f, 0.22f),
                TerrainType.River    => new Color(0.2f, 0.25f, 0.38f),
                TerrainType.Mountain => new Color(0.3f, 0.28f, 0.25f),
                _                    => new Color(0.35f, 0.35f, 0.4f),
            };
        }
    }
}
