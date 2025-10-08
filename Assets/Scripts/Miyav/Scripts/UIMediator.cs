using System;
using System.Collections.Generic;
using DorkyProductions.Algorithms;
using DorkyProductions.Effects;
using DorkyProductions.Views;
using UnityEngine;

namespace DorkyProductions
{
    // Responsible for facilitating Communication btw Logic Side and View Side.
    // This will be the class with MOST Dependencies!
    
    public class UIMediator : MonoBehaviour
    {
        [SerializeField] private PlayerHandView playerHandView;
        [SerializeField] private AIHandView AIHandView;
        [SerializeField] private ShelterRowView shelterRowView;
        [SerializeField] private CaptureGroupSelectionView captureGroupSelectionView;
        
        public static Action<MatchGroup> CaptureGroupSelected;
        // add others as they're developed
        
        // Glossary:
        // Playing a card to play area with capture: Adopt
        // Playing a card to play area without capture : Foster
        
        private void OnEnable()
        {
            BasicCaptureEffect.PlayCardWithCapture += OnAdopt;
            BasicCaptureEffect.PlayCardWithoutCapture += OnFoster;
        }

        private void OnDisable()
        {
            BasicCaptureEffect.PlayCardWithCapture -= OnAdopt;
            BasicCaptureEffect.PlayCardWithoutCapture -= OnFoster;
        }

        private void OnFoster(Guid playedCardID, int playerID)
        {
            InteractableCard playedCard = null;
            if (playerHandView.IsMyPlayer(playerID))
            {
                playedCard = playerHandView.RemoveCardFromHand(playedCardID);
            }
            else
            {
                playedCard = AIHandView.RemoveCardFromHand(playedCardID);
            }
            shelterRowView.GetCardFromHand(playedCard);
        }

        private void OnAdopt(Guid playedCardID, List<MatchGroup> matchingGroups, int playerID)
        {
            // Display Group Selection Screen. then continue the flow.
            if (matchingGroups.Count > 1)
            {
                captureGroupSelectionView.Show(playedCardID, matchingGroups, playerID, OnCaptureGroupSelectedCallback);
            }
            else
            {
                OnCaptureGroupSelectedCallback(playedCardID, matchingGroups[0], playerID);
            }
        }

        private void OnCaptureGroupSelectedCallback(Guid playedCardID, MatchGroup matchGroup, int playerID)
        {
            // relevant Logic classes must be notified here of the Selected group, so that capture logic can continue.
            CaptureGroupSelected?.Invoke(matchGroup);
            
            InteractableCard playedCard;
            if (playerHandView.IsMyPlayer(playerID))
            {
                playedCard = playerHandView.RemoveCardFromHand(playedCardID);
            }
            else
            {
                playedCard = AIHandView.RemoveCardFromHand(playedCardID);
            }

            shelterRowView.ShowCapture(playedCard, matchGroup.GetAllCards(), playerID);
            
            // TODO: Add to correct Player Pile.
            // For now, just disable the cards.
            Debug.Log($"Player: {playerID} played {playedCard.Data.value} and captured: {matchGroup}");
            
        }
        
    }
}