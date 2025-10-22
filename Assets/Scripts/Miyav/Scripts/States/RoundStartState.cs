using DorkyProductions.Core;
using UnityEngine;

namespace DorkyProductions.States
{
    public class RoundStartState : IGameState
    {
        // what needs to happen here?
        // Deal cards to players?
        
        public void Enter(Context context)
        {
            Debug.Log("Round Started!!");
            // context.gameMaster.NextPlayer();
            context.TransitionToState(new PlayerTurnState());
        }

        public void Update(Context context)
        {
            // Debug.Log("TODO: Round Started Updated");
        }

        public void Exit(Context context)
        {
        }
    }
}