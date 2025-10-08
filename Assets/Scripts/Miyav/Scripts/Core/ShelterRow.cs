using System;
using System.Collections.Generic;
using System.Linq;
using DorkyProductions.Algorithms;
using UnityEngine;

namespace DorkyProductions.Core
{
    public class ShelterRow
    {
        public static Action<CatCardData> OnDealtCardToShelterRow;
        private List<CatCardData> _cards = new List<CatCardData>();
        private SubsetSumSolver captureMatchUtility = new SubsetSumSolver();

        public ShelterRow()
        {
            UIMediator.CaptureGroupSelected += RemoveCardsWithCapture;
        }

        ~ShelterRow()
        {
            UIMediator.CaptureGroupSelected -= RemoveCardsWithCapture;
        }
        public void DealCard(CatCardData card)
        {
            _cards.Add(card);
            OnDealtCardToShelterRow?.Invoke(card);
        }

        public void Foster(CatCardData card)
        {
            Debug.Log($"Shelter Row: Foster a {card.value}");
            _cards.Add(card);
        }

        public bool TryGetExactMatch (int value, out Guid matchingCardID)
        {
            // if there is the exact same Value'd card, user must take it. shortcut.
            CatCardData exactMatch = _cards.FirstOrDefault(t => t.value == value);
            if (exactMatch != null)
            {
                matchingCardID = exactMatch.id;
                return true;
            }
            else
            {
                matchingCardID = System.Guid.Empty;
                return false;
            }
        }

        private void RemoveCardsWithCapture(MatchGroup groupToCapture)
        {
            foreach (Guid id in groupToCapture.GetAllCards())
            {
                RemoveCardWithCapture(id);
            }
        }

        public void RemoveCardWithCapture(Guid cardID)
        {
            int idx = _cards.FindIndex(t => t.id == cardID);
            if (idx != -1)
            {
                _cards.RemoveAt(idx);
            }
        }
        public bool HasExactMatch(int value)
        {
            CatCardData exactMatch = _cards.FirstOrDefault(t => t.value == value);
            return exactMatch != null;
        }
        public List<MatchGroup> GetMatchingGroups(int targetSum)
        {
            string debugTxt = "Shelter Row: ";
            int[] cardValues = new int[_cards.Count];
            for (int i = 0; i < _cards.Count; i++)
            {
                cardValues[i] = _cards[i].value;
                debugTxt += _cards[i].value + ",";
            }
            Debug.Log(debugTxt);
            
            // TODO: Clean this up. way too many copies going on. Will lead to fragmentation in a longer game.
            
            var result = captureMatchUtility.GetAllMatches(cardValues, targetSum);
            if (result != null)
            {
                List<MatchGroup> matchGroups = new List<MatchGroup>();
                
                foreach (var group in result)
                {
                    MatchGroup matchingGroup = new MatchGroup();
                    
                    string cardsInGroup = "";
                    foreach (var card in group)
                    {
                        var catCard = _cards[card.idx];
                        cardsInGroup += $"({catCard.value}, {catCard.title}, at idx:{card.idx})";
                        matchingGroup.AddCard(catCard.id);
                    }
                    matchGroups.Add(matchingGroup);
                    Debug.Log("matching group: "+ cardsInGroup);
                    
                }
                return matchGroups;
            }
            else
            {
                Debug.Log("No Matching cards, cant Adopt. perform Foster.");
            }
            return null;
        }
    }
}