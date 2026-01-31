using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DorkyProductions
{
    public class OfflineOptions : MonoBehaviour
    {
        [Header("UI References")]
        public Button startButton;
        public Button easyButton;
        public Button mediumButton;
        public Button hardButton;
        
        
        private void Start()
        {
            // Reset button listeners
            easyButton.onClick.AddListener(() => SetLevel(0));
            mediumButton.onClick.AddListener(() => SetLevel(1));
            hardButton.interactable = false;
            //hardButton.onClick.AddListener(() => SetLevel(2));
            
            startButton.onClick.AddListener(StartOfflineGame);
            
        }

        public void SetLevel(int level)
        {
            BotBrain.SetDifficulty(level);
            Debug.Log($"Difficulty set to: {level}");
           
            // Add visual feedback here (e.g., changing button colors)
            AudioManager.Instance.PlaySFX(SFXType.TapPlayButton);
            
            
            
        }

        public void StartOfflineGame()
        {
            NetworkRoomManager manager = NetworkManager.singleton as NetworkRoomManager;

            if (manager != null)
            {
                AudioManager.Instance.PlaySFX(SFXType.TapPlayButton);
                
                // Mirror Configuration for Offline
                manager.isOfflineMode = true;
                manager.minPlayers = 1;

                // This triggers the transition to the Room (Lobby) Scene
                manager.StartHost();
            }
            else
            {
                Debug.LogError("NetworkRoomManager not found in the scene!");
            }
        }
        
        public void GoToMainMenu()
        {
            AudioManager.Instance.PlaySFX(SFXType.TapPlayButton);
            SceneManager.LoadScene("MainMenuOffline");
        }
    }
}