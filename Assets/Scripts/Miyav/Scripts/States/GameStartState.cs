using System;
using DorkyProductions.Core;
using UnityEngine;

namespace DorkyProductions.States
{
    // a.k.a Setup state.
    public class GameStartState : IGameState
    {
        public static Action NewGameIsStarting;
        private bool verbose = false;
        public void Enter(Context context)
        {
            Debug.Log("A new Game is Starting!");
            NewGameIsStarting?.Invoke();
            
            SetupDecks(context);
            context.gameMaster.DealCardsToAllPlayers(4);
            context.gameMaster.PlaceCardsInTheShelterRow(4);
            context.TransitionToState(new RoundStartState());
        }

        public void Update(Context context) { }

        public void Exit(Context context) { }
        public void SetupDecks(Context context)
        {
            context.DrawPile = SetupDrawDeck();
            context.DrawPile.Shuffle();
            context.SpecialDeck = SetupSpecialDeck();
            context.SpecialDeck.Shuffle();
            Debug.Log("Decks are created and shuffled!.");
        }

        private static CatCard GenerateCatCard(int value)
        {
            return CardFactory.CreateCatCard(CardRegistry.Instance.GetCatCard(value));
        }
        private static SpecialCard GenerateRandomSpecialCard()
        {
            return CardFactory.CreateSpecialCard(CardRegistry.Instance.GetSpecialCard(SpecialCardType.Sphynx));
        }
        private Deck SetupDrawDeck()
        {
            var deck = new Deck();
            for (int i = 0; i <= 11; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    int cardValue = i + 1;
                    deck.AddOnTop(GenerateCatCard(cardValue));
                }
            }

            if (verbose)
            {
                Debug.Log($"Draw deck size: {deck.cards.Count}");
                string debug = "Draw deck has: \n";
                for (int i = 0; i <= 12; i++)
                {
                    int count = deck.cards.FindAll(t => t.data.value == i).Count;
                    debug += $"{count} many {i}'s.\n";
                }
                Debug.Log(debug);
            }
            
            return deck;
        }

        private SpecialDeck SetupSpecialDeck()
        {
            var deck = new SpecialDeck();
            
            // TODO: add all specials.
            // Array specialTypes = Enum.GetValues(typeof(SpecialCardType));
            // int randomIdx = UnityEngine.Random.Range(0, specialTypes.Length);
            // Fur randomType = (SpecialCardType)specialTypes.GetValue(randomIdx);
            deck.AddOnTop(GenerateRandomSpecialCard());

            if (verbose)
            {
                Debug.Log($"Special deck size: {deck.cards.Count}");
                string debug = "Special deck: ";
                foreach (var card in deck.cards)
                {
                    debug += "," + card.data.title;
                }
                Debug.Log(debug);
            }
            return deck;
        }
    }
}