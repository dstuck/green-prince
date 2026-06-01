using UnityEngine;
using UnityEngine.Serialization;
using CardFramework;

namespace GreenPrince
{
    [CreateAssetMenu(fileName = "TileDefinition", menuName = "Green Prince/Tile Definition")]
    public class TileDefinitionSO : CardDefinitionSO
    {
        [FormerlySerializedAs("m_Color")]
        [SerializeField] ResourceType m_ResourceType;
        [SerializeField] int m_ChallengeValue;
        [SerializeField] string m_TileDisplayName;
        [SerializeField] TileIconType m_IconType;
        [SerializeField] ResourceType m_RewardType;
        [SerializeField] int m_RewardAmount;
        [SerializeField] int m_IntimidateLevel;
        [SerializeField] int m_TinkererLevel;
        [SerializeField] int m_DeckBlankTilesToInject;

        public ResourceType ResourceType => m_ResourceType;
        public int ChallengeValue => m_ChallengeValue;
        public string TileDisplayName => string.IsNullOrEmpty(m_TileDisplayName) ? DisplayName : m_TileDisplayName;
        public string BoardDisplayName => IsShrine ? "Shrine" : TileDisplayName;
        public TileIconType IconType => m_IconType;
        public bool HasReward => m_RewardAmount > 0;
        public ResourceType RewardType => m_RewardType;
        public int RewardAmount => m_RewardAmount;
        public int IntimidateLevel => m_IntimidateLevel;
        public int TinkererLevel => m_TinkererLevel;
        public int DeckBlankTilesToInject =>
            m_DeckBlankTilesToInject > 0 ? m_DeckBlankTilesToInject : IsShrine ? 6 : 0;

        public bool ShowTileLabel =>
            m_IconType != TileIconType.None || HasReward || IsShrine || IsWatchTower;

        public bool IsWatchTower =>
            m_IconType == TileIconType.WatchTower || Id.Value == "tile.watch_tower";

        public bool IsShrine =>
            m_IconType == TileIconType.Shrine || Id.Value == "tile.shrine";

        public TileIconType DisplayIconType
        {
            get
            {
                if (m_IconType != TileIconType.None)
                    return m_IconType;
                if (IsShrine)
                    return TileIconType.Shrine;
                return TileIconType.None;
            }
        }
    }
}
