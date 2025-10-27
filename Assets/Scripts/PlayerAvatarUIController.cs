using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerAvatarUIController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI localPlayerNameText;
    [SerializeField]
    private TextMeshProUGUI opponentPlayerNameText;

    [SerializeField]
    private Image localPlayerHealthBar;
    [SerializeField]
    private Image opponentPlayerHealthBar;
    public void SetLocalPlayerNameText(string text)
    {
        localPlayerNameText.text = text;
    }

    public void SetOpponentPlayerNameText(string text)
    {
        opponentPlayerNameText.text = text;
    }

    public void UpdateLocalPlayerHealth(int currentHealth, int maxHealth)
    {
        float fillAmount = (float)currentHealth / (float)maxHealth;
        localPlayerHealthBar.fillAmount = fillAmount;
    }
    public void UpdateOpponentPlayerHealth(int currentHealth, int maxHealth)
    {
        float fillAmount = (float)currentHealth / (float)maxHealth;
        opponentPlayerHealthBar.fillAmount = fillAmount;
    }
}
