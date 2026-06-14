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
        [SerializeField] int m_FoodInterval = 4;

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
        CardCatalog m_Catalog;
        AdventureResources m_Resources;
        ResourceHUD m_HUD;
        PauseMenu m_PauseMenu;
        CampShopPopup m_ShopPopup;
        TileDetailsOverlay m_TileDetails;
        GameState m_State = GameState.Exploring;
        bool m_IsCaravanRun;

        void Start()
        {
            if (m_ForestRecipe == null)
            {
                Debug.LogError("ExplorationManager: assign Forest Recipe (was Land Recipe) on the component.");
                return;
            }

            m_FoodInterval = 4;

            m_Registry = new CardDefinitionRegistry();
            m_Registry.RegisterAll(m_TileDefinitions);
            foreach (var tileDef in m_TileDefinitions)
            {
                if (tileDef?.DeckInjectTile == null) continue;
                m_Registry.Register(tileDef.DeckInjectTile);
            }

            if (m_HelpfulGoblinTile != null)
                m_Registry.Register(m_HelpfulGoblinTile);

            var catalog = new CardCatalog(m_Registry);
            m_Catalog = catalog;
            var rng = new SystemRandomSource();

            WorldState.Initialize(m_GridWidth, m_GridHeight, rng);
            if (m_ShopCatalog != null)
                WorldState.EnsureShopChains(m_ShopCatalog.ChainCount);

            var forest = new ForestDeck(m_ForestRecipe, catalog, rng);
            var camp = new CampDeck();
            m_Deck = new AdventureDeck(forest, camp, catalog, rng);
            m_Pipeline = new CardPipeline();

            m_IsCaravanRun = WorldState.IsCaravanActive;
            var campPos = WorldState.CampPosition;
            m_Grid = new GridModel(m_GridWidth, m_GridHeight, campPos);

            m_GridView.Initialize(m_Grid, m_Registry);

            var campWorldPos = m_GridView.GridToWorld(campPos);
            m_PartyToken.SetGridPosition(campPos, campWorldPos);
            m_PartyToken.SetCampAppearance(m_IsCaravanRun);
            ApplyCaravanGoalPresentation();

            RevealAdjacent(campPos);

            m_Resources = new AdventureResources(m_StartFood, m_StartForce, m_StartTools);
            int magicLevel = PartyAbilities.GetMagicLevel(m_Registry);
            if (magicLevel > 0)
                m_Resources.GainMagic(magicLevel);
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
            {
                m_ShopPopup.Closed -= OnShopClosed;
                m_ShopPopup.MoveCampRequested -= OnMoveCampRequested;
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
            m_PauseMenu.QuitRequested += OnQuitRequested;
        }

        void InitShopPopup()
        {
            var shopGo = new GameObject("CampShopPopup");
            m_ShopPopup = shopGo.AddComponent<CampShopPopup>();
            m_ShopPopup.Initialize(m_Registry);
            m_ShopPopup.Closed += OnShopClosed;
            m_ShopPopup.MoveCampRequested += OnMoveCampRequested;
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
            m_PartyToken.SetAcceptingMoveInput(false);
        }

        void OnResumeRequested()
        {
            if (m_State == GameState.Shopping) return;
            m_State = GameState.Exploring;
            m_PartyToken.SetAcceptingMoveInput(true);
        }

        void OnGiveUpRequested()
        {
            if (m_IsCaravanRun)
                TriggerCaravanFailure();
            else
                TriggerGameOver();
        }

        void OnQuitRequested()
        {
            GameSession.RestartGame();
        }

        void OnShopClosed()
        {
            WorldState.EndCaravan();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnMoveCampRequested()
        {
            WorldState.BeginCaravan(WorldFeatureKind.FirstLandmark);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void ApplyCaravanGoalPresentation()
        {
            if (m_IsCaravanRun && WorldState.TryGetCaravanGoalPosition(out var goal))
            {
                m_GridView.SetCaravanGoal(goal);
                m_GridView.UpdateTile(goal, m_Grid.GetTile(goal));
            }
            else
            {
                m_GridView.SetCaravanGoal(null);
            }
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
            int setAllResourcesAfterStep = 0;

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
                {
                    TryApplyShrineDeckBonus(tile);
                    setAllResourcesAfterStep = TryApplyTileVisitEffects(tile, target);
                }

                if (!tile.IsCamp)
                    CollectPickupsIfAny(target);

                m_GridView.UpdateTile(target, tile);
            }

            var wp = m_GridView.GridToWorld(target);
            m_PartyToken.MoveTo(target, wp);

            RevealAdjacent(target);

            if (scoutWatchTower)
                ApplyWatchTowerScout(target);

            if (m_IsCaravanRun && TryBeginCaravanComplete(target))
                return;

            bool survived = m_Resources.RecordStep(m_FoodInterval);
            if (m_Resources.StepTriggeredConsumption)
                m_HUD.FlashSpend(ResourceType.Food);

            if (setAllResourcesAfterStep > 0)
            {
                m_Resources.SetAllResourcesTo(setAllResourcesAfterStep);
                m_HUD.FlashAllResourcesSet();
            }

            if (!survived)
            {
                if (m_IsCaravanRun)
                    TriggerCaravanFailure();
                else
                    TriggerGameOver();
            }
        }

        bool TryBeginCaravanComplete(Vector2Int pos)
        {
            if (!m_IsCaravanRun) return false;

            if (!WorldState.TryGetCaravanGoalPosition(out var goalPos) || pos != goalPos)
                return false;

            m_State = GameState.GameOver;
            m_PartyToken.SetAcceptingMoveInput(false);
            m_PauseMenu.SetInteractable(false);
            m_HUD.ShowReturningToCamp();
            StartCoroutine(CompleteCaravanAfterTokenArrives(pos));
            return true;
        }

        IEnumerator CompleteCaravanAfterTokenArrives(Vector2Int pos)
        {
            yield return m_PartyToken.WaitUntilArrived();

            var tile = m_Grid.GetTile(pos);
            m_GridView.UpdateTile(pos, tile);

            WorldState.CompleteMigration(pos);
            m_Grid.RelocateCamp(pos);
            m_IsCaravanRun = false;
            m_PartyToken.SetCampAppearance(false);
            m_GridView.SetCaravanGoal(null);

            yield return null;

            WorldState.EndCaravan();
            m_State = GameState.Shopping;
            m_ShopPopup.Show(m_ShopCatalog);
        }

        void TriggerCaravanFailure()
        {
            GameSession.RestartGame();
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

        /// <returns>Resource total to set after the movement food step, or 0 if none.</returns>
        int TryApplyTileVisitEffects(TileState tile, Vector2Int pos)
        {
            var def = m_Registry.Get(tile.Card.DefinitionId) as TileDefinitionSO;
            if (def == null) return 0;

            if (def.DeckInjectTile != null && def.DeckInjectCount > 0)
            {
                m_Deck.InjectTiles(def.DeckInjectTile.Id, def.DeckInjectCount);
                m_HUD.ShowStatusMessage(
                    $"{def.DeckInjectCount} {def.DeckInjectTile.TileDisplayName} added to deck");
            }

            if (def.BlankNeighborsOnVisit)
                ApplyBlankNeighbors(pos);

            if (def.MagicLevel > 0)
            {
                m_Resources.GainMagic(def.MagicLevel);
                m_HUD.FlashMagicGain();
            }

            if (def.SetAllResourcesToOnVisit > 0)
                return def.SetAllResourcesToOnVisit;

            return 0;
        }

        void ApplyBlankNeighbors(Vector2Int center)
        {
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                var neighborPos = center + new Vector2Int(dx, dy);
                if (!m_Grid.IsInBounds(neighborPos)) continue;

                var neighbor = m_Grid.GetTile(neighborPos);
                if (neighbor.IsCamp) continue;

                neighbor.Card = m_Catalog.Spawn(new CardDefinitionId("tile.green0"));
                neighbor.IsRevealed = true;
                neighbor.IsExplored = true;
                m_GridView.UpdateTile(neighborPos, neighbor);
            }
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

            if (m_Resources.TrySpendMagic())
                return true;

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
            m_PartyToken.SetAcceptingMoveInput(false);
            m_PauseMenu.SetInteractable(false);
            m_HUD.ShowReturningToCamp();
            StartCoroutine(FinishRunAndOpenShop());
        }

        IEnumerator FinishRunAndOpenShop()
        {
            yield return m_PartyToken.WaitUntilArrived();

            var pos = m_PartyToken.GridPosition;
            m_GridView.UpdateTile(pos, m_Grid.GetTile(pos));

            yield return null;

            WorldState.EndCaravan();
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
