using CardFramework;
using TMPro;
using UnityEngine;

namespace GreenPrince
{
    public class GridView : MonoBehaviour
    {
        TileView[,] m_TileViews;
        ICardDefinitionRegistry m_Registry;

        public void Initialize(GridModel model, ICardDefinitionRegistry registry)
        {
            m_Registry = registry;
            m_TileViews = new TileView[model.Width, model.Height];

            for (int x = 0; x < model.Width; x++)
            for (int y = 0; y < model.Height; y++)
            {
                var pos = new Vector2Int(x, y);
                var tileGo = CreateTileGameObject(pos);
                var view = tileGo.GetComponent<TileView>();
                view.Initialize(pos);
                m_TileViews[x, y] = view;

                var tileState = model.GetTile(pos);
                UpdateTile(pos, tileState);
            }
        }

        public void UpdateTile(Vector2Int pos, TileState tileState)
        {
            var view = m_TileViews[pos.x, pos.y];

            if (tileState.IsCamp)
            {
                view.ShowCamp();
                return;
            }

            if (!tileState.IsRevealed)
            {
                if (tileState.IsExplored)
                    view.ShowExploredFog(tileState.Terrain, tileState.Landmark);
                else
                    view.ShowUnexplored();
                return;
            }

            TileDefinitionSO def = null;
            TileInstanceState state = null;
            if (tileState.Card != null)
            {
                def = m_Registry.Get(tileState.Card.DefinitionId) as TileDefinitionSO;
                state = tileState.Card.State as TileInstanceState;
            }
            view.ShowRevealed(def, state, tileState.IsVisited, tileState.Terrain, tileState.Landmark);
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x, gridPos.y, 0f);
        }

        GameObject CreateTileGameObject(Vector2Int pos)
        {
            var go = new GameObject($"Tile_{pos.x}_{pos.y}");
            go.transform.SetParent(transform);
            go.transform.localPosition = GridToWorld(pos);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.sortingOrder = 0;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = Vector3.zero;

            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 3f;
            tmp.sortingOrder = 1;
            tmp.rectTransform.sizeDelta = new Vector2(1f, 1f);
            tmp.color = Color.white;

            var challengeGo = new GameObject("ChallengeIndicator");
            challengeGo.transform.SetParent(go.transform);
            challengeGo.transform.localPosition = new Vector3(-0.22f, 0.22f, 0f);

            var challengeSr = challengeGo.AddComponent<SpriteRenderer>();
            challengeSr.sprite = CreateSquareSprite();
            challengeSr.sortingOrder = 2;
            challengeGo.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            var challengeLabelGo = new GameObject("ChallengeLabel");
            challengeLabelGo.transform.SetParent(challengeGo.transform);
            challengeLabelGo.transform.localPosition = Vector3.zero;

            var challengeTmp = challengeLabelGo.AddComponent<TextMeshPro>();
            challengeTmp.alignment = TextAlignmentOptions.Center;
            challengeTmp.fontSize = 5f;
            challengeTmp.sortingOrder = 3;
            challengeTmp.rectTransform.sizeDelta = new Vector2(0.8f, 0.8f);
            challengeTmp.color = Color.white;

            go.AddComponent<TileView>();

            return go;
        }

        static Sprite s_SquareSprite;

        static Sprite CreateSquareSprite()
        {
            if (s_SquareSprite != null) return s_SquareSprite;

            const int size = 32;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % size;
                int y = i / size;
                bool isBorder = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                pixels[i] = isBorder ? new Color(0.1f, 0.1f, 0.1f) : Color.white;
            }

            tex.SetPixels(pixels);
            tex.Apply();

            s_SquareSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return s_SquareSprite;
        }
    }
}
