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
    
    
    public bool IsLocalPlayer;
    
    // TODO: move this to a central constant file.

    private void OnEnable()
    {
        if (IsLocalPlayer)
        {
            UIMediator.OnLocalPlayerHealthUpdated += UpdatePlayerHealth;
            UIMediator.OnLocalPlayerShieldUpdated += UpdatePlayerShield;
        }
        else
        {
            UIMediator.OnOpponentPlayerHealthUpdated += UpdatePlayerHealth;
            UIMediator.OnOpponentPlayerShieldUpdated += UpdatePlayerShield;
        }
    }

    private void OnDisable()
    {
        if (IsLocalPlayer)
        {
            UIMediator.OnLocalPlayerHealthUpdated -= UpdatePlayerHealth;
            UIMediator.OnLocalPlayerShieldUpdated -= UpdatePlayerShield;
        }
        else
        {
            UIMediator.OnOpponentPlayerHealthUpdated -= UpdatePlayerHealth;
            UIMediator.OnOpponentPlayerShieldUpdated -= UpdatePlayerShield;
        }
    }

    public void SetPlayerNameText(string text)
    {
        playerNameText.text = text;
    }
    private void UpdatePlayerHealth(int currentHealth)
    {
        float fillAmount = (float)currentHealth / (float) NetworkPlayer.PLAYER_STARTING_HEALTH;
        playerHealthBar.fillAmount = fillAmount;
        playerHealthText.text = currentHealth.ToString();
    }
    
    private void UpdatePlayerShield(int currentShield)
    {
        float fillAmount = (float)currentShield / (float) NetworkPlayer.PLAYER_STARTING_HEALTH;
        fillAmount = Mathf.Max(0.1f, fillAmount);
        playerShieldBar.fillAmount = fillAmount;
        playerShieldText.text = currentShield.ToString();
    }
    
}

}