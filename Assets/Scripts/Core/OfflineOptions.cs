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
            easyButton.onClick.AddListener(() => SetBotType(BotDifficulty.Easy));
            mediumButton.onClick.AddListener(() => SetBotType(BotDifficulty.Medium));
            hardButton.interactable = false;
            //hardButton.onClick.AddListener(() => SetBotType(BotDifficulty.Hard));
            
            startButton.onClick.AddListener(StartOfflineGame);
            
        }

        public void SetBotType(BotDifficulty type)
        {
            BotBrain.SetDifficulty(type);
            Debug.Log($"Difficulty set to: {type}");
           
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