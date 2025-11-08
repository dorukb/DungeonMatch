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

        private void UpdateTurnText(PlayerType player, bool extraTurn)
        {
            if (player == PlayerType.Local)
            {
                turnText.text = "Your Turn " + (extraTurn ? "(Extra!)" : "");
            }
            else
            {
                turnText.text = "Opponent is playing... " + (extraTurn ? "(Extra!)" : "");
            }
        }
    }

}
