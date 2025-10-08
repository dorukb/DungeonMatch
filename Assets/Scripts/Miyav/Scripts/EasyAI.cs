using System;
using System.Collections.Generic;
using DorkyProductions.Core;

namespace DorkyProductions.AI
{
    public class EasyAI : DecisionAI
    {
        public void SelectMove(Context context, PlayerAI controller)
        {
            // Do one of the below. 1st one is the most preferred.
            // 1. Play *exact* match 
            // 2. Play *any* match
            // 3. No match possible: Play *random* card from hand.
            // completely ignores specials.
            if (TryGetExactMatchingCard(context.ShelterRow, controller.GetCatCards(), out var exactMatchCard))
            {
                controller.PlayCard(exactMatchCard);
            }
            else if (TryGetAnyMatchingCard(context.ShelterRow, controller.GetCatCards(), out var anyMatchingCard))
            {
                controller.PlayCard(anyMatchingCard);
            }
            else
            {
                var handCards = controller.GetCatCards();
                int randIdx = new Random().Next(0, handCards.Count);
                controller.PlayCard(handCards[randIdx]);
            }
        }

        // @Summary Returns true if a CatCard that can be used for an Exact match exists, false otherwise.
        // on true, out variable is filled with the matching card.
        // on false it is null.
        private bool TryGetExactMatchingCard(ShelterRow playArea, List<CatCard> myCards, out CatCard exactMatchingCard)
        {
            foreach (var card in myCards)
            {
                if (playArea.HasExactMatch(card.data.value))
                {
                    exactMatchingCard = card;
                    return true;
                }
            }
            exactMatchingCard = null;
            return false;
        } 
        private bool TryGetAnyMatchingCard(ShelterRow playArea, List<CatCard> myCards, out CatCard anyMatchingCard)
        {
            foreach (var card in myCards)
            {
                var matchingGroups = playArea.GetMatchingGroups(card.data.value);
                if (matchingGroups != null && matchingGroups.Count > 0)
                {
                    // we assume a matching group has at least one card. this must be satisfied by the algorithm.
                    anyMatchingCard = card;
                    return true;
                }
            }
            anyMatchingCard = null;
            return false;
        }
    }
}