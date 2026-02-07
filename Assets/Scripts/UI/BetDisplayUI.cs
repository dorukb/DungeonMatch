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
            UIMediator.OnBetAmountIsGot += UpdateBetUIText;
        }

        private void OnDisable()
        {
            UIMediator.OnBetAmountIsGot -= UpdateBetUIText;
        }
        
        private void UpdateBetUIText(int bet)
        {
            betUIText.text = bet.ToString();
        }
    }
}