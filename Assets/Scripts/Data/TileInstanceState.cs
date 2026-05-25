using CardFramework;

namespace GreenPrince
{
    public class TileInstanceState : ICardInstanceState
    {
        public int ModifiedChallengeValue { get; set; } = -1;

        public bool HasModifiedChallenge => ModifiedChallengeValue >= 0;
    }
}
