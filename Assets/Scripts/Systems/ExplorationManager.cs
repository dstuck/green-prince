using CardFramework;
using UnityEngine;

namespace GreenPrince
{
    public class ExplorationManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] int m_GridWidth = 20;
        [SerializeField] int m_GridHeight = 7;

        [Header("References")]
        [SerializeField] DeckRecipeSO m_LandRecipe;
        [SerializeField] TileDefinitionSO[] m_TileDefinitions;
        [SerializeField] PartyToken m_PartyToken;
        [SerializeField] GridView m_GridView;

        GridModel m_Grid;
        LandDeck m_Deck;
        CardPipeline m_Pipeline;
        CardDefinitionRegistry m_Registry;

        void Start()
        {
            m_Registry = new CardDefinitionRegistry();
            m_Registry.RegisterAll(m_TileDefinitions);

            var catalog = new CardCatalog(m_Registry);
            var rng = new SystemRandomSource();
            m_Deck = new LandDeck(m_LandRecipe, catalog, rng);

            m_Pipeline = new CardPipeline();

            var campPos = new Vector2Int(0, m_GridHeight / 2);
            m_Grid = new GridModel(m_GridWidth, m_GridHeight, campPos);

            m_GridView.Initialize(m_Grid, m_Registry);

            var campWorldPos = m_GridView.GridToWorld(campPos);
            m_PartyToken.SetGridPosition(campPos, campWorldPos);

            RevealAdjacent(campPos);

            m_PartyToken.MoveRequested += OnMoveRequested;
        }

        void OnDestroy()
        {
            if (m_PartyToken != null)
                m_PartyToken.MoveRequested -= OnMoveRequested;
        }

        void OnMoveRequested(Vector2Int direction)
        {
            var target = m_PartyToken.GridPosition + direction;

            if (!m_Grid.IsInBounds(target)) return;

            if (!m_Grid.IsRevealed(target))
                RevealTile(target);

            var tile = m_Grid.GetTile(target);
            if (!tile.IsVisited)
            {
                tile.IsVisited = true;
                m_GridView.UpdateTile(target, tile);
            }

            var worldPos = m_GridView.GridToWorld(target);
            m_PartyToken.MoveTo(target, worldPos);

            RevealAdjacent(target);
        }

        void RevealTile(Vector2Int pos)
        {
            var instance = m_Deck.Draw();
            if (instance == null) return;

            m_Pipeline.Process(instance, null);
            m_Grid.RevealTile(pos, instance);
            m_GridView.UpdateTile(pos, m_Grid.GetTile(pos));
        }

        void RevealAdjacent(Vector2Int center)
        {
            var unrevealed = m_Grid.GetUnrevealedAdjacent(center);
            foreach (var pos in unrevealed)
                RevealTile(pos);
        }
    }
}
