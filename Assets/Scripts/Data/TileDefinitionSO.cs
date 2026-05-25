using UnityEngine;
using CardFramework;

namespace GreenPrince
{
    [CreateAssetMenu(fileName = "TileDefinition", menuName = "Green Prince/Tile Definition")]
    public class TileDefinitionSO : CardDefinitionSO
    {
        [SerializeField] TileColor m_Color;
        [SerializeField] int m_ChallengeValue;

        public TileColor Color => m_Color;
        public int ChallengeValue => m_ChallengeValue;
    }
}
