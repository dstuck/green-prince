using UnityEngine.SceneManagement;

namespace GreenPrince
{
    /// <summary>
    /// Single entry point for restarting the game (clears all persistent state and reloads the scene).
    /// </summary>
    public static class GameSession
    {
        public static void RestartGame()
        {
            UiInputFocus.Clear();
            WorldState.Reset();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
