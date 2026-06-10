using System;
using DorkyProductions;
using DorkyProductions.UI;
using Mirror;
using TMPro;
using UnityEngine;

namespace UI
{
    public class BetDisplayUI : MonoBehaviour 
    {
        [SerializeField] private TextMeshProUGUI betUIText;

        private void OnEnable()
        {
            UIMediator.OnReceivedBetAmount += UpdateBetUIText;
        }

        private void OnDisable()
        {
            UIMediator.OnReceivedBetAmount -= UpdateBetUIText;
        }
        
        private void UpdateBetUIText(int bet)
        {
            betUIText.text = bet.ToString();
        }
    }
}