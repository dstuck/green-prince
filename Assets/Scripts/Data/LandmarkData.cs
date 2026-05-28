using UnityEngine;

namespace GreenPrince
{
    public class LandmarkData
    {
        public Vector2Int Position { get; }
        public string Name { get; }
        public ResourceType ChallengeType { get; }
        public int ChallengeValue { get; }
        public bool IsDiscovered { get; set; }

        public LandmarkData(Vector2Int position, string name,
            ResourceType challengeType, int challengeValue)
        {
            Position = position;
            Name = name;
            ChallengeType = challengeType;
            ChallengeValue = challengeValue;
        }
    }
}
