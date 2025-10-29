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

    public bool IsLocalPlayer;
    
    // TODO: move this to a central constant file.

    private void OnEnable()
    {
        if (IsLocalPlayer)
        {
            UIMediator.OnLocalPlayerHealthUpdated += UpdatePlayerHealth;
        }
        else
        {
            
            UIMediator.OnOpponentPlayerHealthUpdated += UpdatePlayerHealth;
        }
    }

    private void OnDisable()
    {
        if (IsLocalPlayer)
        {
            UIMediator.OnLocalPlayerHealthUpdated -= UpdatePlayerHealth;
        }
        else
        {
            UIMediator.OnOpponentPlayerHealthUpdated -= UpdatePlayerHealth;
        }
    }

    public void SetPlayerNameText(string text)
    {
        playerNameText.text = text;
    }
    public void UpdatePlayerHealth(int currentHealth)
    {
        float fillAmount = (float)currentHealth / (float) NetworkPlayer.PLAYER_STARTING_HEALTH;
        playerHealthBar.fillAmount = fillAmount;
    }
}

}