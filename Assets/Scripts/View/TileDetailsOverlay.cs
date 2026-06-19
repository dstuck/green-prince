using System.Collections.Generic;
using CardFramework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GreenPrince
{
    public class TileDetailsOverlay : MonoBehaviour
    {
        const int MaxLabels = 24;
        const float StaggerGap = 4f;

        GridView m_GridView;
        GridModel m_Grid;
        ICardDefinitionRegistry m_Registry;
        TileDetailContentCatalog m_Content;
        Camera m_Camera;

        readonly List<StandardTooltipWidget> m_LabelPool = new();
        readonly List<PendingLabel> m_Pending = new();
        readonly List<Rect> m_PlacedRects = new();

        struct PendingLabel
        {
            public StandardTooltipWidget Widget;
            public Vector2 BaseScreenPos;
        }

        public void Initialize(GridView gridView, GridModel grid, ICardDefinitionRegistry registry,
            TileDetailContentCatalog content = null)
        {
            m_GridView = gridView;
            m_Grid = grid;
            m_Registry = registry;
            m_Content = content ?? TileDetailContentCatalog.Load();
        }

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            OverlayCanvasUtility.Configure(scaler);

            for (int i = 0; i < MaxLabels; i++)
                m_LabelPool.Add(StandardTooltipWidget.Create(transform));
        }

        void Update()
        {
            if (!IsShiftHeld())
            {
                HideAll();
                return;
            }

            if (m_Camera == null)
                m_Camera = Camera.main;

            if (m_Camera == null || m_GridView == null || m_Grid == null)
            {
                HideAll();
                return;
            }

            m_Pending.Clear();
            m_PlacedRects.Clear();

            TryQueueCampTooltip();

            for (int x = 0; x < m_Grid.Width; x++)
            for (int y = 0; y < m_Grid.Height; y++)
            {
                var pos = new Vector2Int(x, y);
                var tile = m_Grid.GetTile(pos);
                if (!IsSpecialTile(tile)) continue;
                if (!IsOnScreen(pos)) continue;
                if (!TryGetContent(tile, out var content)) continue;

                QueueTooltipAt(pos, content.Name, content.Description);
            }

            m_Pending.Sort((a, b) => a.BaseScreenPos.y.CompareTo(b.BaseScreenPos.y));

            for (int i = 0; i < m_Pending.Count; i++)
            {
                var pending = m_Pending[i];
                var rect = pending.Widget.GetScreenRect(pending.BaseScreenPos);

                for (int pass = 0; pass < 12; pass++)
                {
                    bool moved = false;
                    foreach (var placed in m_PlacedRects)
                    {
                        if (!rect.Overlaps(placed)) continue;
                        rect.y = placed.yMax + StaggerGap;
                        moved = true;
                    }

                    if (!moved) break;
                }

                m_PlacedRects.Add(rect);
                pending.Widget.SetScreenPosition(new Vector2(rect.center.x, rect.y));
                pending.Widget.SetActive(true);
            }

            for (int i = m_Pending.Count; i < m_LabelPool.Count; i++)
                m_LabelPool[i].SetActive(false);
        }

        void TryQueueCampTooltip()
        {
            if (!IsCampOnScreen()) return;

            string title = "Camp";
            string body = BuildCampResourceLine();

            if (m_Content.TryGetUi("camp", out var content))
            {
                if (!string.IsNullOrEmpty(content.Name))
                    title = content.Name;

                if (!string.IsNullOrEmpty(content.Description))
                    body = string.IsNullOrEmpty(body)
                        ? content.Description
                        : $"{content.Description}\n{body}";
            }

            var world = m_GridView.GridToWorld(m_Grid.CampPosition) + new Vector3(0f, 0.4f, 0f);
            QueueTooltip(m_Camera.WorldToScreenPoint(world), title, body);
        }

        static string BuildCampResourceLine()
        {
            return
                $"Technology {WorldState.GetCampResource(CampResourceType.Technology)}   " +
                $"Experience {WorldState.GetCampResource(CampResourceType.Experience)}   " +
                $"Lore {WorldState.GetCampResource(CampResourceType.Lore)}";
        }

        void QueueTooltipAt(Vector2Int gridPos, string title, string body)
        {
            var world = m_GridView.GridToWorld(gridPos) + new Vector3(0f, 0.3f, 0f);
            QueueTooltip(m_Camera.WorldToScreenPoint(world), title, body);
        }

        void QueueTooltip(Vector2 screenPos, string title, string body)
        {
            if (m_Pending.Count >= m_LabelPool.Count) return;

            var widget = m_LabelPool[m_Pending.Count];
            widget.SetContent(title, body);
            m_Pending.Add(new PendingLabel
            {
                Widget = widget,
                BaseScreenPos = screenPos
            });
        }

        bool IsSpecialTile(TileState tile)
        {
            if (tile == null || tile.IsCamp) return false;

            if (tile.Feature != null)
                return tile.IsRevealed;

            if (!tile.IsRevealed || tile.Card == null || m_Registry == null)
                return false;

            return m_Registry.Get(tile.Card.DefinitionId) is TileDefinitionSO def && def.ShowTileLabel;
        }

        bool TryGetContent(TileState tile, out TileDetailContent content)
        {
            if (tile.Feature != null)
            {
                if (m_Content.TryGetFeature(tile.Feature.Kind, out content)
                    && !string.IsNullOrEmpty(content.Description))
                    return true;

                content = default;
                return false;
            }

            if (tile.Card == null || m_Registry == null)
            {
                content = default;
                return false;
            }

            var def = m_Registry.Get(tile.Card.DefinitionId) as TileDefinitionSO;
            if (def == null)
            {
                content = default;
                return false;
            }

            if (m_Content.TryGetTile(def.Id.Value, out content)
                && !string.IsNullOrEmpty(content.Description))
                return true;

            content = default;
            return false;
        }

        bool IsCampOnScreen()
        {
            return IsOnScreen(m_Grid.CampPosition);
        }

        bool IsOnScreen(Vector2Int gridPos)
        {
            var world = m_GridView.GridToWorld(gridPos);
            var screen = m_Camera.WorldToScreenPoint(world);
            return screen.z > 0f
                && screen.x >= 0f && screen.x <= Screen.width
                && screen.y >= 0f && screen.y <= Screen.height;
        }

        void HideAll()
        {
            for (int i = 0; i < m_LabelPool.Count; i++)
                m_LabelPool[i].SetActive(false);
            m_Pending.Clear();
            m_PlacedRects.Clear();
        }

        static bool IsShiftHeld()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
        }
    }
}
