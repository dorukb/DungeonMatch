using TMPro;
using UnityEngine;

namespace DorkyProductions.UI
{
    public class TurnDisplayUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI turnText;

        private void OnEnable()
        {
            turnText.gameObject.SetActive(false);
            UIMediator.OnPlayerTurnStarted += UpdateTurnText;
        }

        private void OnDisable()
        {
            UIMediator.OnPlayerTurnStarted -= UpdateTurnText;
        }

        private void UpdateTurnText(PlayerType player, bool isExtra)
        {

            if (player == PlayerType.Local)
            {
                turnText.text = "Your Turn " + (isExtra ? "(Extra!)" : "");
            }
            else
            {
                turnText.text = "Opponent's Turn" + (isExtra ? "(Extra!)" : "");
            }

        }

        public void OverwriteTurnText(string text)
        {
            turnText.text = text;

            // If you call OverwriteTurnText, you might also want to schedule a hide call:
            // CancelInvoke(HideFunctionName);
            // turnText.gameObject.SetActive(true);
            // Invoke(HideFunctionName, 3.0f);
        }
    }
}