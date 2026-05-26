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

        public ResourceType ResourceType => m_ResourceType;
        public int ChallengeValue => m_ChallengeValue;
    }
}
