using TMPro;
using UnityEngine;

namespace DorkyProductions.UI
{
    public class TurnDisplayUI : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI turnText;
    
        private void OnEnable()
        {
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
                turnText.text = "Opponent is playing... " + (isExtra ? "(Extra!)" : "");
            }
        }

        public void OverwriteTurnText(string text)
        {
            turnText.text = text;
        }
    }

}
