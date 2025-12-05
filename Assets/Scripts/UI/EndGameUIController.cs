using System;
using UnityEngine;

namespace DorkyProductions.UI
{
    public class EndGameUIController : MonoBehaviour
    {
        [SerializeField] public GameObject winScreen;
        [SerializeField] public GameObject loseScreen;

        private void Awake()
        {
            winScreen.SetActive(false);
            loseScreen.SetActive(false);
        }

        public void OnEnable()
        {
            UIMediator.OnGameEnded += ShowEndGameScreen;
        }

        public void OnDisable()
        {
            UIMediator.OnGameEnded -= ShowEndGameScreen;
        }

        private void ShowEndGameScreen(PlayerType winnerPlayerType)
        {
            if (winnerPlayerType == PlayerType.Local)
            {
                winScreen.SetActive(true);
                loseScreen.SetActive(false);
            }
            else
            {
                loseScreen.SetActive(true);
                winScreen.SetActive(false);
            }
        }
    }

}
