using CardFramework;
using TMPro;
using UnityEngine;

namespace GreenPrince
{
    public class TileView : MonoBehaviour
    {
        static readonly Color UnrevealedColor = new Color(0.15f, 0.15f, 0.2f);
        static readonly Color CampColor = new Color(0.9f, 0.75f, 0.3f);

        SpriteRenderer m_Renderer;
        TextMeshPro m_Label;

        public Vector2Int GridPosition { get; private set; }

        public void Initialize(Vector2Int gridPosition)
        {
            GridPosition = gridPosition;

            m_Renderer = GetComponent<SpriteRenderer>();
            m_Label = GetComponentInChildren<TextMeshPro>();

            ShowUnrevealed();
        }

        public void ShowUnrevealed()
        {
            m_Renderer.color = UnrevealedColor;
            m_Label.text = "";
        }

        public void ShowCamp()
        {
            m_Renderer.color = CampColor;
            m_Label.text = "";
        }

        public void ShowRevealed(TileDefinitionSO definition, TileInstanceState state, bool visited)
        {
            m_Renderer.color = GetTileColor(definition.Color);

            if (visited)
            {
                m_Label.text = "";
                return;
            }

            int value = (state != null && state.HasModifiedChallenge)
                ? state.ModifiedChallengeValue
                : definition.ChallengeValue;

            m_Label.text = value > 0 ? value.ToString() : "";
        }

        static Color GetTileColor(TileColor tileColor)
        {
            return tileColor switch
            {
                TileColor.Green => new Color(0.3f, 0.7f, 0.35f),
                TileColor.Red => new Color(0.8f, 0.3f, 0.3f),
                TileColor.Blue => new Color(0.3f, 0.45f, 0.8f),
                _ => Color.white
            };
        }
    }
}
