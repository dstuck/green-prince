using TMPro;
using UnityEngine;

namespace GreenPrince
{
    public class TileView : MonoBehaviour
    {
        static readonly Color UnexploredColor = new Color(0.15f, 0.15f, 0.2f);
        static readonly Color CampColor = new Color(0.9f, 0.75f, 0.3f);
        static readonly Color LandmarkColor = new Color(0.85f, 0.65f, 0.9f);

        SpriteRenderer m_Renderer;
        TextMeshPro m_Label;
        SpriteRenderer m_ChallengeRenderer;
        TextMeshPro m_ChallengeLabel;

        public Vector2Int GridPosition { get; private set; }

        public void Initialize(Vector2Int gridPosition)
        {
            GridPosition = gridPosition;

            m_Renderer = GetComponent<SpriteRenderer>();
            m_Label = transform.Find("Label").GetComponent<TextMeshPro>();

            var challengeGo = transform.Find("ChallengeIndicator");
            m_ChallengeRenderer = challengeGo.GetComponent<SpriteRenderer>();
            m_ChallengeLabel = challengeGo.GetComponentInChildren<TextMeshPro>();

            ShowUnexplored();
        }

        public void ShowUnexplored()
        {
            m_Renderer.color = UnexploredColor;
            m_Label.text = "";
            HideChallenge();
        }

        public void ShowExploredFog(TerrainType terrain, LandmarkData landmark)
        {
            m_Renderer.color = GetTerrainFogColor(terrain);
            HideChallenge();

            if (landmark != null)
            {
                m_Label.text = "?";
                m_Label.fontSize = 4f;
                m_Label.color = LandmarkColor;
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
        }

        public void ShowRevealed(TileDefinitionSO definition, TileInstanceState state,
            bool visited, TerrainType terrain, LandmarkData landmark)
        {
            m_Renderer.color = GetTerrainColor(terrain);

            if (landmark != null)
            {
                m_Label.text = landmark.Name;
                m_Label.fontSize = 2.5f;
                m_Label.color = visited ? new Color(1f, 1f, 1f, 0.5f) : Color.white;

                if (!visited && landmark.ChallengeValue > 0)
                    ShowChallenge(ResourceColors.Get(landmark.ChallengeType), landmark.ChallengeValue);
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
