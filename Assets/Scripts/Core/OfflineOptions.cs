using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace DorkyProductions
{
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

        private void Start()
        {
            // Listeners modified to play SFX on ANY value change (on or off)
            easyToggle.onValueChanged.AddListener((isOn) => HandleToggleChange(BotDifficulty.Easy, isOn));
            mediumToggle.onValueChanged.AddListener((isOn) => HandleToggleChange(BotDifficulty.Medium, isOn));
            hardToggle.onValueChanged.AddListener((isOn) => HandleToggleChange(BotDifficulty.Hard, isOn));

            hardToggle.interactable = false;
            startButton.onClick.AddListener(StartOfflineGame);
            
            UpdateToggleVisuals();
        }

        // New helper method to keep Start() clean
        private void HandleToggleChange(BotDifficulty difficulty, bool isOn)
        {
            // Play sound for both selecting AND deselecting
            AudioManager.Instance.PlaySFX(SFXType.TapPlayButton);

            // Only update the bot brain if we are turning the difficulty ON
            if (isOn)
            {
                BotBrain.SetDifficulty(difficulty);
                Debug.Log($"Difficulty set to: {difficulty}");
            }

            UpdateToggleVisuals();
            
            // Clear focus so the UI doesn't stay "stuck" in a highlighted state
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
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
                // Only two colors: Selected or Normal
                text.color = toggle.isOn ? selectedColor : normalColor;
                
                // Add boldness to the selected one for extra clarity
                text.fontStyle = toggle.isOn ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        public void StartOfflineGame()
        {
            NetworkRoomManager manager = NetworkManager.singleton as NetworkRoomManager;
            if (manager != null)
            {
                AudioManager.Instance.PlaySFX(SFXType.TapPlayButton);
                manager.isOfflineMode = true;
                manager.minPlayers = 1;
                manager.StartHost();
            }
        }

        public void GoToMainMenu()
        {
            AudioManager.Instance.PlaySFX(SFXType.TapPlayButton);
            SceneManager.LoadScene("MainMenuOffline");
        }
    }
}