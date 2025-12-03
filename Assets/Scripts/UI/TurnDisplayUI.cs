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

        private void UpdateTurnText(PlayerType player, TurnStartReason reason)
        {
            if (player == PlayerType.Local)
            {
                turnText.text = "Your Turn " + (reason != TurnStartReason.TurnOrder ? "(Extra!)" : "");
            }
            else
            {
                turnText.text = "Opponent is playing... " + (reason != TurnStartReason.TurnOrder ? "(Extra!)" : "");
            }
        }

        public void OverwriteTurnText(string text)
        {
            turnText.text = text;
        }
    }

}
