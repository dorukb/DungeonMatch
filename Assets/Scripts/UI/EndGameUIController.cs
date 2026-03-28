using UnityEngine;
using DorkyProductions.UI;
using UnityEngine.SceneManagement;
using Mirror; // Only if you are using Mirror

namespace UI
{
    public class EndGameController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ResultDisplayUI resultDisplay; // The sliding text script
        [SerializeField] private EndGameSummaryView summaryView;

        private void Awake()
        {
            // Ensure UI is clean at start
            if (summaryView != null) summaryView.Initialize();
            
            // Listen for the Back Button click
            summaryView.BackToMenuButton.onClick.AddListener(HandleExitToMenu);
        }

        private void OnEnable() => UIMediator.OnGameEnded += StartEndSequence;
        private void OnDisable() => UIMediator.OnGameEnded -= StartEndSequence;

        private void StartEndSequence(PlayerType winner)
        {
            bool isLocalWin = (winner == PlayerType.Local);
            
            // 2. Play the sliding "YOU WIN" banner
            resultDisplay.PlayResultAnimation(isLocalWin, 3.0f, () => {
                
                // 3. Once text is gone, show the final summary
                int coins = isLocalWin ? 150 : 50;
                summaryView.Show(isLocalWin, coins);
            });
        }

        private void HandleExitToMenu()
        {
            // Clean up Mirror Networking before leaving
            if (NetworkManager.singleton != null)
            {
                if (NetworkServer.active && NetworkClient.isConnected)
                {
                    NetworkManager.singleton.StopHost();
                }
                else
                {
                    NetworkManager.singleton.StopClient();
                }
            }

            SceneManager.LoadScene("MainMenu");
        }
    }
}