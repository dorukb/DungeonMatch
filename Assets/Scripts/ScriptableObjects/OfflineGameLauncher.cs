using System;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace DorkyProductions
{
    public enum BotDifficulty { None = -1, Easy = 0, Medium = 1, Hard = 2 }

    public class OfflineGameLauncher : MonoBehaviour
    {
        [Header("UI References")]
        public Button startButton;
        
        [Header("Difficulty Buttons")]
        public Button easyButton;
        public Button mediumButton;
        public Button hardButton;

        [Header("Visual Settings")]
        public Color normalColor = Color.white;
        public Color selectedColor = new Color32(238, 152, 12, 255);
        
        [Header("Coin Settings")]
        [SerializeField] private Image coinsImage;
        [SerializeField] private TextMeshProUGUI coinsText;

        // Default to None so they have to pick one to enable the start button
        private BotDifficulty _selectedDifficulty = BotDifficulty.None;

        private void Start()
        {
            // Set up button listeners
            easyButton.onClick.AddListener(() => SelectDifficulty(BotDifficulty.Easy));
            mediumButton.onClick.AddListener(() => SelectDifficulty(BotDifficulty.Medium));
            hardButton.onClick.AddListener(() => SelectDifficulty(BotDifficulty.Hard));

            // Optional: Disable Hard button if it's still locked
            hardButton.interactable = false; 

            startButton.onClick.AddListener(StartOfflineGame);
            
            // Initial State: Start button is disabled until a difficulty is picked
            startButton.interactable = false;
            
            UpdateUI();
        }

        private void SelectDifficulty(BotDifficulty difficulty)
        {
            AudioManager.Instance.PlaySFX(SFXType.TapPlayButton);
            
            _selectedDifficulty = difficulty;
            
            // Enable start button now that a selection exists
            startButton.interactable = true;

            UpdateUI();
        }

        private void UpdateUI()
        {
            UpdateVisuals();
            UpdateCoinDisplay();
        }

        private void UpdateCoinDisplay()
        {
            if (coinsText == null) return;

            if (_selectedDifficulty == BotDifficulty.None)
            {
                coinsText.text = "0";
                return;
            }

            int price = RemoteConfigManager.Instance.GetBetAmountVal((int)_selectedDifficulty);
            coinsText.text = price.ToString();
        }

        private void UpdateVisuals()
        {
            SetButtonStyle(easyButton, _selectedDifficulty == BotDifficulty.Easy);
            SetButtonStyle(mediumButton, _selectedDifficulty == BotDifficulty.Medium);
            SetButtonStyle(hardButton, _selectedDifficulty == BotDifficulty.Hard);
        }

        private void SetButtonStyle(Button button, bool isSelected)
        {
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.color = isSelected ? selectedColor : normalColor;
                text.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        public void StartOfflineGame()
        {
            if (_selectedDifficulty == BotDifficulty.None) return;

            NetworkRoomManager manager = NetworkManager.singleton as NetworkRoomManager;
            if (manager != null)
            {
                AudioManager.Instance.PlaySFX(SFXType.TapPlayButton);
                
                manager.minPlayers = 1;
                // Use the tracked selection
                manager.gameStartConfig = new GameStartConfig(true, false, MapDifficultyToLobbyType(_selectedDifficulty));
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
                case BotDifficulty.Easy: return LobbyType.Beginner;
                case BotDifficulty.Medium: return LobbyType.Intermediate;
                case BotDifficulty.Hard: return LobbyType.Advanced;
                default: return LobbyType.Beginner;
            }
        }
    }
}