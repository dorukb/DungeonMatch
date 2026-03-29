using System;
using UnityEngine;
using DorkyProductions.UI;
namespace UI
{
    public class EndGameController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ResultDisplayUI resultDisplay; // The sliding text
        [SerializeField] private EndGameSummaryPanelUI summaryView; // actual results panel

        private void OnEnable() => UIMediator.OnGameEnded += StartEndSequence;
        private void OnDisable() => UIMediator.OnGameEnded -= StartEndSequence;

        private void Start()
        {
            summaryView.gameObject.SetActive(false);
            resultDisplay.gameObject.SetActive(false);
        }

        private void StartEndSequence(PlayerType winner, int betAmount)
        {
            bool isLocalWin = (winner == PlayerType.Local);

            Debug.Log("starting end game anim seq");
            resultDisplay.gameObject.SetActive(true);
            // 2. Play the sliding "YOU WIN" banner
            resultDisplay.PlayResultAnimation(isLocalWin, 3.0f, () => {
                Debug.Log("resultDisplay.PlayResultAnimation finished");
                summaryView.gameObject.SetActive(true);
                summaryView.Show(isLocalWin, betAmount);
            });
        }
    }
}