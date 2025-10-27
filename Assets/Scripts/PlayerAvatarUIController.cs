using UnityEngine;
using TMPro;

public class PlayerAvatarUIController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI localPlayerNameText;
    [SerializeField]
    private TextMeshProUGUI opponentPlayerNameText;


    public void SetLocalPlayerNameText(string text)
    {
        localPlayerNameText.text = text;
    }

    public void SetOpponentPlayerNameText(string text)
    {
        opponentPlayerNameText.text = text;
    }
}
