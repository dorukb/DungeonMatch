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

    [SerializeField] private GameObject chestDisplay;
    
    [Tooltip("Which player does this UI portrait represent?")]
    [SerializeField]
    public PlayerType displayForPlayer;
    
    private void OnEnable()
    {
        UIMediator.OnPlayerHealthUpdated += UpdatePlayerHealth;
        UIMediator.OnPlayerShieldUpdated += UpdatePlayerShield;
        UIMediator.OnPlayersCrossMultiplierUpdated += UpdateCrossMultiplier;
        UIMediator.OnPlayerChestUpdated += UpdateChestDisplay;
    }

    private void OnDisable()
    {
        UIMediator.OnPlayerHealthUpdated -= UpdatePlayerHealth;
        UIMediator.OnPlayerShieldUpdated -= UpdatePlayerShield;
        UIMediator.OnPlayersCrossMultiplierUpdated -= UpdateCrossMultiplier;
        UIMediator.OnPlayerChestUpdated -= UpdateChestDisplay;
    }

    private void Start()
    {
        crossDisplay.SetActive(false);
        chestDisplay.SetActive(false);
        
        SetPlayerHealthText(RemoteConfigManager.Instance.GetStartingHealth());
        SetPlayerShieldText(RemoteConfigManager.Instance.GetStartingShield());
    }

    public void SetPlayerHealthText(int health)
    {
        playerHealthText.text = health.ToString();
    }
    
    public void SetPlayerShieldText(int shield)
    {
        playerShieldText.text = shield.ToString();
    }
    public void SetPlayerNameText(string text)
    {
        playerNameText.text = text;
    }
    private void UpdatePlayerHealth(PlayerType player, int currentHealth)
    {
        if (player != displayForPlayer) return;


        float fillAmount = (float)currentHealth / RemoteConfigManager.Instance.GetStartingHealth();
        fillAmount = Mathf.Max(0.1f, fillAmount);
        playerHealthBar.fillAmount = fillAmount;
        playerHealthText.text = currentHealth.ToString();
    }
    
    private void UpdatePlayerShield(PlayerType player, int currentShield)
    {
        if (player != displayForPlayer) return; 
        
        float fillAmount = (float)currentShield / RemoteConfigManager.Instance.GetStartingShield();
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
    
    private void UpdateChestDisplay(PlayerType player, bool hasChest)
    {
        if (player != displayForPlayer) return;
        
        chestDisplay.SetActive(hasChest);
    }

    
}

}