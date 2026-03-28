using DG.Tweening;
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
    private TextMeshProUGUI winnerText; 
    [SerializeField] 
    private GameObject winnerGlow; // Optional: A shiny background
    
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
    public UI.PlayerType displayForPlayer;
    
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
    private void UpdatePlayerHealth(UI.PlayerType player, int currentHealth)
    {
        if (player != displayForPlayer) return;


        float fillAmount = (float)currentHealth / RemoteConfigManager.Instance.GetStartingHealth();
        fillAmount = Mathf.Max(0.1f, fillAmount);
        playerHealthBar.fillAmount = fillAmount;
        playerHealthText.text = currentHealth.ToString();
    }
    
    private void UpdatePlayerShield(UI.PlayerType player, int currentShield)
    {
        if (player != displayForPlayer) return; 
        
        float fillAmount = (float)currentShield / RemoteConfigManager.Instance.GetStartingShield();
        fillAmount = Mathf.Max(0.1f, fillAmount);
        playerShieldBar.fillAmount = fillAmount;
        playerShieldText.text = currentShield.ToString();
    }

    private void UpdateCrossMultiplier(UI.PlayerType player, float currMultiplier)
    {
        if (player != displayForPlayer) return;

        multiplierText.text = $"{currMultiplier:0.##}x";
        bool hasMultiplier = currMultiplier > Mathf.Epsilon;
        crossDisplay.SetActive(hasMultiplier);
    }
    
    private void UpdateChestDisplay(UI.PlayerType player, bool hasChest)
    {
        if (player != displayForPlayer) return;
        
        chestDisplay.SetActive(hasChest);
    }
    
    public void SetupForEndGame(string name, bool isWinner)
    {
        // Disable gameplay-only UI
        playerHealthBar.transform.parent.gameObject.SetActive(false); // Assuming bars are in a parent container
        playerShieldBar.transform.parent.gameObject.SetActive(false);
        crossDisplay.SetActive(false);
        chestDisplay.SetActive(false);

        // Set the Name
        playerNameText.text = name;

        // Handle Winner Visuals
        winnerText.gameObject.SetActive(isWinner);
        if (winnerGlow != null) winnerGlow.SetActive(isWinner);

        // Add some "Juice" if they won
        if (isWinner)
        {
            // Simple DOTween pulse for the winner text
            winnerText.transform.DOScale(1.1f, 0.5f).SetLoops(-1, LoopType.Yoyo);
            transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.5f, 5, 1);
        }
    }
    
}

}