using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace DorkyProductions.Core
{
    public abstract class Player
    {
        public string DisplayName { get; protected set; }
        public int id { get; protected set; }
        public bool isMyTurn { get; protected set; }
        
        public Action<CatCard> OnPlayerReceivedCatCard;
        public Action<SpecialCard> OnPlayerReceivedSpecialCard;
        
        protected List<CatCard> catsInHand = new List<CatCard>();
        protected List<SpecialCard> specialsInHand = new List<SpecialCard>();
        
        protected Context lastContext;
        public void ReceiveCatCard(CatCard card)
        {
            // Debug.Log($"{DisplayName} received card {card.data.title}");
            catsInHand.Add(card);
            OnPlayerReceivedCatCard?.Invoke(card);
        }

        protected void PlayTheCard(CatCard card)
        {
            if (!isMyTurn)
            {
                Debug.LogError("Ignore play cat card request, its NOT my turn. This callback shouldnt have triggered in the first place! ");
                return;
            }
            if (card != null)
            {
                Debug.Log($"{DisplayName} is playing card: {card.data.title}");
                // Its our turn, user tapped on a card in our hand. no reason to deny it,
                // except if there is a global condition that blocks the Player.
                lastContext.SetCurrentCardInPlay(card);
                catsInHand.Remove(card);
                card.effect.Execute(lastContext);
            }
            else
            {
                Debug.LogError($"Tapped card: {card.data.title} is NOT in {this.DisplayName}'s hand.");
            }
        }
        protected void PlayTheCard(Guid cardID)
        {  
            if (!isMyTurn)
            {
                Debug.LogError("Ignore play cat card request, its NOT my turn. This callback shouldnt have triggered in the first place! ");
                return;
            }
            CatCard card = GetCardInHand(cardID);
            PlayTheCard(card);
        }
        
        public void ReceiveSpecialCard(SpecialCard card)
        {
            Debug.Log($"{DisplayName} received a Special card {card.data.title}");
            specialsInHand.Add(card);
            OnPlayerReceivedSpecialCard?.Invoke(card);
        }

        public void TurnStarted(Context context)
        {
            isMyTurn = true;
            lastContext = context;
            OnTurnStarted(context);
        }

        public void TurnEnded(Context context)
        {
            isMyTurn = false;
            lastContext = context;
            OnTurnEnded(context);
        }

        public virtual void Update(Context context)
        {
            // No default behavior on Update. subclasses can use as they wish.
        }
        
        protected virtual void OnTurnStarted(Context context){}
        protected virtual void OnTurnEnded(Context context){}
        
        protected CatCard GetCardInHand(Guid cardID)
        {
            CatCard card = catsInHand.FirstOrDefault(t => t.id == cardID);
            return card;
        }
    }
}