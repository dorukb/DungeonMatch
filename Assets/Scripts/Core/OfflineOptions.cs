using System;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace DorkyProductions
{
    public enum BotDifficulty { Easy = 0, Medium = 1, Hard = 2 }
    
    public class OfflineOptions : MonoBehaviour
    {
        [Header("UI References")]
        public Button startButton;
        
        [Header("Difficulty Toggles")]
        public Toggle easyToggle;
        public Toggle mediumToggle;
        public Toggle hardToggle;

        [Header("Visual Settings")]
        public Color normalColor = Color.white;
        public Color selectedColor = new Color32(238, 152, 12, 255);
        
        [Header("Coin Settings")]
        [SerializeField] private Image coinsImage;
        [SerializeField] private TextMeshProUGUI coinsText;

        private BotDifficulty _botDifficulty = BotDifficulty.Medium;
        private void Start()
        {
            easyToggle.onValueChanged.AddListener((isOn) => HandleToggleChange(BotDifficulty.Easy, isOn));
            mediumToggle.onValueChanged.AddListener((isOn) => HandleToggleChange(BotDifficulty.Medium, isOn));
            hardToggle.onValueChanged.AddListener((isOn) => HandleToggleChange(BotDifficulty.Hard, isOn));

            hardToggle.interactable = false;
            startButton.onClick.AddListener(StartOfflineGame);
            
            // Initialize everything
            UpdateToggleVisuals();
            UpdateCoinDisplay(); 
            _botDifficulty = (BotDifficulty)RemoteConfigManager.Instance.GetDefaultBot();     
        }

        private void HandleToggleChange(BotDifficulty difficulty, bool isOn)
        {
            AudioManager.Instance.PlaySFX(SFXType.TapPlayButton);

            if (isOn)
            {
                // BotBrain.SetDifficulty(difficulty);
                Debug.Log($"Difficulty set to: {difficulty}");
            }

            UpdateToggleVisuals();
            UpdateCoinDisplay(); // Update the coin text based on the new selection

            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        private void UpdateCoinDisplay()
        {
            if (coinsText == null) return;

            // 1. Check which toggle is currently ON
            // 2. Decide the value
            int price = 0;

            if (easyToggle.isOn)
            {
                price = RemoteConfigManager.Instance.GetBetAmountVal((int) BotDifficulty.Easy);
            }
            else if (mediumToggle.isOn)
            {
                price = RemoteConfigManager.Instance.GetBetAmountVal((int) BotDifficulty.Medium);
            }
            else if (hardToggle.isOn)
            {
                price = RemoteConfigManager.Instance.GetBetAmountVal((int) BotDifficulty.Hard);
            }
            else price = 0; // If nothing is selected

            // 3. Update the UI
            coinsText.text = price.ToString();
        }

        private void UpdateToggleVisuals()
        {
            SetToggleVisuals(easyToggle);
            SetToggleVisuals(mediumToggle);
            SetToggleVisuals(hardToggle);
        }

        private void SetToggleVisuals(Toggle toggle)
        {
            var text = toggle.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.color = toggle.isOn ? selectedColor : normalColor;
                text.fontStyle = toggle.isOn ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        // Called by: Start button on Offline Scene.
        public void StartOfflineGame()
        {
            NetworkRoomManager manager = NetworkManager.singleton as NetworkRoomManager;
            if (manager != null)
            {
                AudioManager.Instance.PlaySFX(SFXType.TapPlayButton);
                
                manager.minPlayers = 1;
                manager.gameStartConfig = new GameStartConfig(true, MapDifficultyToLobbyType(_botDifficulty));
                manager.StartHost();
            }
            else
            {
                Debug.LogError("NetworkRoomManager not found!");
            }
        }

        public void GoToMainMenu()
        {
            AudioManager.Instance.PlaySFX(SFXType.TapPlayButton);
            SceneManager.LoadScene("MainMenuOffline");
        }

        private LobbyType MapDifficultyToLobbyType(BotDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy:
                    return LobbyType.Beginner;
                case BotDifficulty.Medium:
                    return LobbyType.Intermediate;
                case BotDifficulty.Hard:
                    return LobbyType.Advanced;
                default:
                    return LobbyType.Beginner;
            }
        }
    }
}