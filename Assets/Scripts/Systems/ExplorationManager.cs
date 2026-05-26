using System.Collections;
using CardFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GreenPrince
{
    public class ExplorationManager : MonoBehaviour
    {
        enum GameState { Exploring, Paused, GameOver }

        [Header("Grid Settings")]
        [SerializeField] int m_GridWidth = 20;
        [SerializeField] int m_GridHeight = 7;

        [Header("Resources")]
        [SerializeField] int m_StartFood = 10;
        [SerializeField] int m_StartForce = 5;
        [SerializeField] int m_StartTools = 4;
        [SerializeField] int m_FoodInterval = 5;

        [Header("References")]
        [SerializeField] DeckRecipeSO m_LandRecipe;
        [SerializeField] TileDefinitionSO[] m_TileDefinitions;
        [SerializeField] PartyToken m_PartyToken;
        [SerializeField] GridView m_GridView;

        GridModel m_Grid;
        LandDeck m_Deck;
        CardPipeline m_Pipeline;
        CardDefinitionRegistry m_Registry;
        AdventureResources m_Resources;
        ResourceHUD m_HUD;
        PauseMenu m_PauseMenu;
        GameState m_State = GameState.Exploring;

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

            m_Resources = new AdventureResources(m_StartFood, m_StartForce, m_StartTools);
            InitHUD();
            InitPauseMenu();

            m_PartyToken.MoveRequested += OnMoveRequested;
        }

        void OnDestroy()
        {
            if (m_PartyToken != null)
                m_PartyToken.MoveRequested -= OnMoveRequested;

            if (m_PauseMenu != null)
            {
                m_PauseMenu.PauseRequested -= OnPauseRequested;
                m_PauseMenu.ResumeRequested -= OnResumeRequested;
                m_PauseMenu.GiveUpRequested -= OnGiveUpRequested;
            }
        }

        void InitHUD()
        {
            var hudGo = new GameObject("ResourceHUD");
            m_HUD = hudGo.AddComponent<ResourceHUD>();
            m_HUD.Bind(m_Resources);
        }

        void InitPauseMenu()
        {
            var pauseGo = new GameObject("PauseMenu");
            m_PauseMenu = pauseGo.AddComponent<PauseMenu>();
            m_PauseMenu.PauseRequested += OnPauseRequested;
            m_PauseMenu.ResumeRequested += OnResumeRequested;
            m_PauseMenu.GiveUpRequested += OnGiveUpRequested;
        }

        void OnPauseRequested()
        {
            m_State = GameState.Paused;
            m_PartyToken.enabled = false;
        }

        void OnResumeRequested()
        {
            m_State = GameState.Exploring;
            m_PartyToken.enabled = true;
        }

        void OnGiveUpRequested()
        {
            TriggerGameOver();
        }

        void OnMoveRequested(Vector2Int direction)
        {
            if (m_State != GameState.Exploring) return;

            var target = m_PartyToken.GridPosition + direction;

            if (!m_Grid.IsInBounds(target)) return;

            if (!m_Grid.IsRevealed(target))
                RevealTile(target);

            var tile = m_Grid.GetTile(target);

            if (!tile.IsVisited && tile.Card != null)
            {
                var def = m_Registry.Get(tile.Card.DefinitionId) as TileDefinitionSO;
                if (def != null)
                {
                    int cost = GetEffectiveCost(def, tile.Card.State as TileInstanceState);
                    if (!m_Resources.CanAfford(def.ResourceType, cost))
                    {
                        m_HUD.FlashInsufficient(def.ResourceType);
                        return;
                    }

                    m_Resources.Spend(def.ResourceType, cost);
                    if (cost > 0)
                        m_HUD.FlashSpend(def.ResourceType);
                }
            }

            if (!tile.IsVisited)
            {
                tile.IsVisited = true;
                m_GridView.UpdateTile(target, tile);
            }

            var worldPos = m_GridView.GridToWorld(target);
            m_PartyToken.MoveTo(target, worldPos);

            RevealAdjacent(target);

            bool survived = m_Resources.RecordStep(m_FoodInterval);
            if (m_Resources.StepTriggeredConsumption)
                m_HUD.FlashSpend(ResourceType.Food);
            if (!survived)
                TriggerGameOver();
        }

        static int GetEffectiveCost(TileDefinitionSO def, TileInstanceState state)
        {
            return (state != null && state.HasModifiedChallenge)
                ? state.ModifiedChallengeValue
                : def.ChallengeValue;
        }

        void TriggerGameOver()
        {
            m_State = GameState.GameOver;
            m_PartyToken.enabled = false;
            m_PauseMenu.SetInteractable(false);
            m_HUD.ShowReturningToCamp();
            StartCoroutine(RestartAfterDelay(2f));
        }

        IEnumerator RestartAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
