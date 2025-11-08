using DorkyProductions.UI;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace DorkyProductions{

public class PlayerPortraitUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI playerNameText;
    [SerializeField]
    private Image playerHealthBar;
    
    [SerializeField]
    private TextMeshProUGUI playerHealthText;
    [SerializeField]
    private Image playerShieldBar;
    [SerializeField]
    private TextMeshProUGUI playerShieldText;

    [SerializeField] private GameObject crossDisplay;
    [SerializeField] private TextMeshProUGUI multiplierText;
    
    [Tooltip("Which player does this UI portrait represent?")]
    [SerializeField]
    public PlayerType displayForPlayer;
    
    // TODO: move this to a central constant file.
    // NO MORE duplicated code!
    private void OnEnable()
    {
        UIMediator.OnPlayerHealthUpdated += UpdatePlayerHealth;
        UIMediator.OnPlayerShieldUpdated += UpdatePlayerShield;
        UIMediator.OnPlayersCrossMultiplierUpdated += UpdateCrossMultiplier;
    }

    private void OnDisable()
    {
        UIMediator.OnPlayerHealthUpdated -= UpdatePlayerHealth;
        UIMediator.OnPlayerShieldUpdated -= UpdatePlayerShield;
        UIMediator.OnPlayersCrossMultiplierUpdated -= UpdateCrossMultiplier;
    }

    private void Start()
    {
        crossDisplay.SetActive(false);
    }

    public void SetPlayerNameText(string text)
    {
        playerNameText.text = text;
    }
    private void UpdatePlayerHealth(PlayerType player, int currentHealth)
    {
        if (player != displayForPlayer) return; 
        
        float fillAmount = (float)currentHealth / (float) NetworkPlayer.PLAYER_STARTING_HEALTH;
        playerHealthBar.fillAmount = fillAmount;
        playerHealthText.text = currentHealth.ToString();
    }
    
    private void UpdatePlayerShield(PlayerType player, int currentShield)
    {
        if (player != displayForPlayer) return; 
        
        float fillAmount = (float)currentShield / (float) NetworkPlayer.PLAYER_STARTING_HEALTH;
        fillAmount = Mathf.Max(0.1f, fillAmount);
        playerShieldBar.fillAmount = fillAmount;
        playerShieldText.text = currentShield.ToString();
    }

    private void UpdateCrossMultiplier(PlayerType player, float currMultiplier)
    {
        if (player != displayForPlayer) return;

        multiplierText.text = $"{currMultiplier:0.##}x";
        bool hasMultiplier = currMultiplier > Mathf.Epsilon;
        crossDisplay.SetActive(hasMultiplier);
  }
    
}

}