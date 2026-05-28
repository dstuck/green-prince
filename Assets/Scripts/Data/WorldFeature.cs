using System.Collections.Generic;
using UnityEngine;

namespace GreenPrince
{
    public enum WorldFeatureType
    {
        Landmark,
        Hazard
    }

    public class WorldFeature
    {
        public Vector2Int Position { get; }
        public string Name { get; }
        public WorldFeatureType FeatureType { get; }
        public ResourceType ChallengeType { get; }
        public int ChallengeValue { get; }
        public bool IsOvercome { get; set; }
        public bool RewardsGranted { get; set; }

        public Dictionary<CampResourceType, int> Rewards { get; } = new();

        public bool HasActiveChallenge => ChallengeValue > 0 && !IsOvercome;

        public WorldFeature(Vector2Int position, string name, WorldFeatureType featureType,
            ResourceType challengeType, int challengeValue)
        {
            Position = position;
            Name = name;
            FeatureType = featureType;
            ChallengeType = challengeType;
            ChallengeValue = challengeValue;
        }
    }
}
