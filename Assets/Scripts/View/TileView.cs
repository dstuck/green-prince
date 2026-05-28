using TMPro;
using UnityEngine;

namespace GreenPrince
{
    public class TileView : MonoBehaviour
    {
        static readonly Color UnexploredColor = new Color(0.15f, 0.15f, 0.2f);
        static readonly Color CampColor = new Color(0.9f, 0.75f, 0.3f);
        static readonly Color LandmarkColor = new Color(0.85f, 0.65f, 0.9f);
        static readonly Color HazardColor = new Color(0.8f, 0.35f, 0.3f);

        SpriteRenderer m_Renderer;
        TextMeshPro m_Label;
        SpriteRenderer m_ChallengeRenderer;
        TextMeshPro m_ChallengeLabel;
        SpriteRenderer[] m_PickupRenderers;

        public Vector2Int GridPosition { get; private set; }

        public void Initialize(Vector2Int gridPosition)
        {
            GridPosition = gridPosition;

            m_Renderer = GetComponent<SpriteRenderer>();
            m_Label = transform.Find("Label").GetComponent<TextMeshPro>();

            var challengeGo = transform.Find("ChallengeIndicator");
            m_ChallengeRenderer = challengeGo.GetComponent<SpriteRenderer>();
            m_ChallengeLabel = challengeGo.GetComponentInChildren<TextMeshPro>();

            var pickupContainer = transform.Find("PickupContainer");
            m_PickupRenderers = pickupContainer.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

            ShowUnexplored();
        }

        public void ShowUnexplored()
        {
            m_Renderer.color = UnexploredColor;
            m_Label.text = "";
            HideChallenge();
            HidePickups();
        }

        public void ShowExploredFog(TerrainType terrain, WorldFeature feature, System.Collections.Generic.List<WorldPickup> pickups)
        {
            m_Renderer.color = GetTerrainFogColor(terrain);
            HideChallenge();
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
        }

        public void ShowCamp()
        {
            m_Renderer.color = CampColor;
            m_Label.text = "";
            HideChallenge();
            HidePickups();
        }

        public void ShowRevealed(TileDefinitionSO definition, TileInstanceState state,
            bool visited, TerrainType terrain, WorldFeature feature, System.Collections.Generic.List<WorldPickup> pickups)
        {
            m_Renderer.color = GetTerrainColor(terrain);
            ShowPickups(pickups);

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
                return;
            }

            m_Label.text = "";

            if (visited || definition == null)
            {
                HideChallenge();
                return;
            }

            int value = (state != null && state.HasModifiedChallenge)
                ? state.ModifiedChallengeValue
                : definition.ChallengeValue;

            if (value > 0)
                ShowChallenge(ResourceColors.Get(definition.ResourceType), value);
            else
                HideChallenge();
        }

        void ShowChallenge(Color color, int value)
        {
            m_ChallengeRenderer.enabled = true;
            m_ChallengeRenderer.color = color;
            m_ChallengeLabel.text = value.ToString();
            m_ChallengeLabel.enabled = true;
        }

        void HideChallenge()
        {
            m_ChallengeRenderer.enabled = false;
            m_ChallengeLabel.text = "";
            m_ChallengeLabel.enabled = false;
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
