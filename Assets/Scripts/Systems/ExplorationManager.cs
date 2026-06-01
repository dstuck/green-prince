using System.Collections;
using CardFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace GreenPrince
{
    public class ExplorationManager : MonoBehaviour
    {
        enum GameState { Exploring, Paused, GameOver, Shopping }

        [Header("Grid Settings")]
        [SerializeField] int m_GridWidth = 20;
        [SerializeField] int m_GridHeight = 7;

        [Header("Resources")]
        [SerializeField] int m_StartFood = 10;
        [SerializeField] int m_StartForce = 5;
        [SerializeField] int m_StartTools = 4;
        [SerializeField] int m_FoodInterval = 5;

        [Header("References")]
        [FormerlySerializedAs("m_LandRecipe")]
        [SerializeField] DeckRecipeSO m_ForestRecipe;
        [SerializeField] TileDefinitionSO[] m_TileDefinitions;
        [SerializeField] TileDefinitionSO m_HelpfulGoblinTile;
        [SerializeField] CampShopCatalogSO m_ShopCatalog;
        [SerializeField] PartyToken m_PartyToken;
        [SerializeField] GridView m_GridView;

        GridModel m_Grid;
        AdventureDeck m_Deck;
        CardPipeline m_Pipeline;
        CardDefinitionRegistry m_Registry;
        AdventureResources m_Resources;
        ResourceHUD m_HUD;
        PauseMenu m_PauseMenu;
        CampShopPopup m_ShopPopup;
        TileDetailsOverlay m_TileDetails;
        GameState m_State = GameState.Exploring;

        void Start()
        {
            if (m_ForestRecipe == null)
            {
                Debug.LogError("ExplorationManager: assign Forest Recipe (was Land Recipe) on the component.");
                return;
            }

            m_Registry = new CardDefinitionRegistry();
            m_Registry.RegisterAll(m_TileDefinitions);
            if (m_HelpfulGoblinTile != null)
                m_Registry.Register(m_HelpfulGoblinTile);

            var catalog = new CardCatalog(m_Registry);
            var rng = new SystemRandomSource();

            WorldState.Initialize(m_GridWidth, m_GridHeight, rng);
            if (m_ShopCatalog != null)
                WorldState.EnsureShopChains(m_ShopCatalog.ChainCount);

            var forest = new ForestDeck(m_ForestRecipe, catalog, rng);
            var camp = new CampDeck();
            m_Deck = new AdventureDeck(forest, camp, catalog, rng);
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
            InitShopPopup();
            InitTileDetails();

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
                m_PauseMenu.QuitRequested -= OnQuitRequested;
            }

            if (m_ShopPopup != null)
                m_ShopPopup.Closed -= OnShopClosed;
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
            m_PauseMenu.QuitRequested += OnQuitRequested;
        }

        void InitShopPopup()
        {
            var shopGo = new GameObject("CampShopPopup");
            m_ShopPopup = shopGo.AddComponent<CampShopPopup>();
            m_ShopPopup.Closed += OnShopClosed;
        }

        void InitTileDetails()
        {
            var go = new GameObject("TileDetailsOverlay");
            m_TileDetails = go.AddComponent<TileDetailsOverlay>();
            m_TileDetails.Initialize(m_GridView, m_Grid, m_Registry);
        }

        void OnPauseRequested()
        {
            if (m_State == GameState.Shopping) return;
            m_State = GameState.Paused;
            m_PartyToken.enabled = false;
        }

        void OnResumeRequested()
        {
            if (m_State == GameState.Shopping) return;
            m_State = GameState.Exploring;
            m_PartyToken.enabled = true;
        }

        void OnGiveUpRequested()
        {
            TriggerGameOver();
        }

        void OnQuitRequested()
        {
            WorldState.Reset();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnShopClosed()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnMoveRequested(Vector2Int direction)
        {
            if (m_State != GameState.Exploring) return;

            var target = m_PartyToken.GridPosition + direction;

            if (!m_Grid.IsInBounds(target)) return;

            var tile = m_Grid.GetTile(target);

            if (!tile.IsCamp && !m_Grid.IsRevealed(target))
                RevealTile(target);

            tile = m_Grid.GetTile(target);

            bool scoutWatchTower = false;

            if (!tile.IsVisited)
            {
                if (!TryPayTileCost(tile))
                    return;

                scoutWatchTower = IsWatchTowerTile(tile);

                tile.IsVisited = true;
                tile.IsExplored = true;

                if (tile.Feature != null && tile.Feature.HasActiveChallenge)
                {
                    tile.Feature.IsOvercome = true;
                    GrantFeatureRewardsIfNeeded(tile.Feature);
                    TryUnlockHelpfulGoblin(tile.Feature);
                }

                GrantTileRewardIfAny(tile);

                if (tile.Card != null)
                    TryApplyShrineDeckBonus(tile);

                if (!tile.IsCamp)
                    CollectPickupsIfAny(target);

                m_GridView.UpdateTile(target, tile);
            }

            var wp = m_GridView.GridToWorld(target);
            m_PartyToken.MoveTo(target, wp);

            RevealAdjacent(target);

            if (scoutWatchTower)
                ApplyWatchTowerScout(target);

            bool survived = m_Resources.RecordStep(m_FoodInterval);
            if (m_Resources.StepTriggeredConsumption)
                m_HUD.FlashSpend(ResourceType.Food);
            if (!survived)
                TriggerGameOver();
        }

        void TryUnlockHelpfulGoblin(WorldFeature feature)
        {
            if (feature.Kind != WorldFeatureKind.GoblinCamp) return;
            if (m_HelpfulGoblinTile == null) return;
            if (WorldState.IsForestUnlocked(m_HelpfulGoblinTile.Id)) return;
            WorldState.UnlockForestCard(m_HelpfulGoblinTile.Id);
        }

        void GrantTileRewardIfAny(TileState tile)
        {
            if (tile.Card == null) return;

            var def = m_Registry.Get(tile.Card.DefinitionId) as TileDefinitionSO;
            if (def == null || !def.HasReward) return;

            m_Resources.Gain(def.RewardType, def.RewardAmount);
            m_HUD.FlashGain(def.RewardType);
        }

        void TryApplyShrineDeckBonus(TileState tile)
        {
            var def = m_Registry.Get(tile.Card.DefinitionId) as TileDefinitionSO;
            if (def == null || def.DeckBlankTilesToInject <= 0) return;

            m_Deck.InjectTiles(new CardDefinitionId("tile.green0"), def.DeckBlankTilesToInject);
        }

        bool IsWatchTowerTile(TileState tile)
        {
            if (tile.Card == null) return false;
            var def = m_Registry.Get(tile.Card.DefinitionId) as TileDefinitionSO;
            return def != null && def.IsWatchTower;
        }

        void ApplyWatchTowerScout(Vector2Int center)
        {
            const int scoutRadius = 2;
            foreach (var pos in m_Grid.GetUnrevealedWithinManhattanDistance(center, scoutRadius))
                RevealTile(pos);
        }

        bool TryPayTileCost(TileState tile)
        {
            ResourceType costType;
            int cost;

            if (tile.Feature != null && tile.Feature.HasActiveChallenge)
            {
                costType = tile.Feature.ChallengeType;
                cost = tile.Feature.ChallengeValue;
            }
            else if (tile.Card != null)
            {
                var def = m_Registry.Get(tile.Card.DefinitionId) as TileDefinitionSO;
                if (def != null)
                {
                    costType = def.ResourceType;
                    cost = GetEffectiveCost(def, tile.Card.State as TileInstanceState);
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }

            if (costType == ResourceType.Force && cost <= PartyAbilities.GetIntimidateLevel(m_Registry))
                return true;

            if (costType == ResourceType.Tools)
                cost = Mathf.Max(0, cost - PartyAbilities.GetTinkererLevel(m_Registry));

            if (cost <= 0) return true;

            if (!m_Resources.CanAfford(costType, cost))
            {
                m_HUD.FlashInsufficient(costType);
                return false;
            }

            m_Resources.Spend(costType, cost);
            m_HUD.FlashSpend(costType);
            return true;
        }

        void GrantFeatureRewardsIfNeeded(WorldFeature feature)
        {
            if (feature.RewardsGranted) return;
            if (feature.Rewards == null || feature.Rewards.Count == 0) return;

            foreach (var kvp in feature.Rewards)
                WorldState.AddCampResource(kvp.Key, kvp.Value);

            feature.RewardsGranted = true;
        }

        void CollectPickupsIfAny(Vector2Int pos)
        {
            if (WorldState.CollectPickupsAt(pos))
                m_GridView.UpdateTile(pos, m_Grid.GetTile(pos));
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
            StartCoroutine(ShowShopAfterDelay(1.5f));
        }

        IEnumerator ShowShopAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            m_State = GameState.Shopping;
            m_ShopPopup.Show(m_ShopCatalog);
        }

        void RevealTile(Vector2Int pos)
        {
            var tile = m_Grid.GetTile(pos);

            if (tile.Feature != null)
            {
                m_Grid.RevealTile(pos);
                m_GridView.UpdateTile(pos, m_Grid.GetTile(pos));
                return;
            }

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
