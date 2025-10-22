using System;
using System.Collections.Generic;
using DorkyProductions.Algorithms;
using DorkyProductions.Core;
using UnityEngine;

namespace DorkyProductions.Effects
{
    public class BasicCaptureEffect : IEffect
    {
        
        // params: <PlayedCardID, PossibleCaptureGroupIDs, playerID>
        public static Action<Guid, List<MatchGroup>, int> PlayCardWithCapture;
        // params: <PlayedCardID, playerID>
        public static Action<Guid, int> PlayCardWithoutCapture;

        public void Execute(Context context)
        {
            var playedCard = context.CurrentCardInPlay.data;
            Debug.Log($"Trying to capture of total value: {playedCard.value}, of type: {playedCard.value}");
            if (context.ShelterRow.TryGetExactMatch(playedCard.value, out Guid matchingCardID))
            {
                Debug.Log($"Exact match. Must capture the card: {matchingCardID}");
                context.ShelterRow.RemoveCardWithCapture(matchingCardID);
                
                var possibleCaptureGroups = new List<MatchGroup>();
                var captureGroup = new MatchGroup();
                captureGroup.AddCard(matchingCardID);
                possibleCaptureGroups.Add(captureGroup);
                
                // Let UI Views handle the visuals.
                PlayCardWithCapture?.Invoke(playedCard.id, possibleCaptureGroups, context.CurrentMiyavPlayer.id);
            }
            else
            {
                var matchingResult = context.ShelterRow.GetMatchingGroups(playedCard.value);
                if (matchingResult != null && matchingResult.Count > 0)
                {
                    Debug.Log($"Group capture found for {playedCard.value}. {matchingResult.Count} many possible groups.");
                    
                    // Important: ShelterRow state is NOT modified here, since we wait for User Input to decide which group to capture.
                    PlayCardWithCapture?.Invoke(playedCard.id, matchingResult, context.CurrentMiyavPlayer.id);
                }
                else
                {
                    Debug.Log($"No matching groups found for {playedCard.value}. FOSTER.");
                    context.ShelterRow.Foster(playedCard);
                    PlayCardWithoutCapture?.Invoke(playedCard.id, context.CurrentMiyavPlayer.id);
                }
            }
            
            // No chain actions. User has played their entire turn at this point.
            context.SignalEndTurn();
        }
    }
}