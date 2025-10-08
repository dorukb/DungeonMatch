using System.Collections.Generic;
using DorkyProductions.AI;
using UnityEngine;

namespace DorkyProductions.Core
{
    public class PlayerAI : Player
    {   
        // TODO: Pass in a "Strategy" object to select different AI Logic.
        private DecisionAI _logic;
        private float turnStartTime = 0;
        private float decisionFakeDelay = 2.5f;
        private bool hasPlayed = false;
        public PlayerAI(int id, string displayName = "DumbAI")
        {
            this.id = id;
            this.DisplayName = displayName;
            this._logic = new EasyAI();
        }

        protected override void OnTurnStarted(Context context)
        {
            Debug.Log("Turn Started AI");
            turnStartTime = Time.time;
            hasPlayed = false;
        }

        public override void Update(Context context)
        {
            bool shouldPlay = Time.time > turnStartTime + decisionFakeDelay;
            if (shouldPlay && !hasPlayed)
            {
                // logic will can into PlayCard after a decision is made.
                _logic.SelectMove(context, this);
            }
        }
        public void PlayCard(CatCard card)
        {  
           // TODO: Fake delay here?
           PlayTheCard(card);
        }
        public List<CatCard> GetCatCards()
        {
            return catsInHand;
        }
    }
}