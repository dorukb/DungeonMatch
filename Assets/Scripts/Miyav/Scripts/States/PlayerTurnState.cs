using DorkyProductions.Core;
using UnityEngine;

namespace DorkyProductions.States
{
    public class PlayerTurnState : IGameState
    {
        // maybe a counter, or better yet: a list booleans to know which players has taken their turns.
        public void Enter(Context context)
        {
            Debug.Log($"Notifying {context.currentPlayer.DisplayName} to begin their turn.");
            StartPlayerTurn(context.currentPlayer);
            context.currentPlayer.TurnStarted(context);
        }

        public void Update(Context context)
        {
            context.currentPlayer.Update(context);
        }

        // This is called by the Players, if they dont, it will be their turn indefinitely.
        // or ofc until some Timeout duration in a Multiplayer game.
        public void OnPlayerTurnShouldEnd()
        {
            // Disable, return the PlayerStateMachine to "dormant" state.
            // TODO:
            //     if current player is last player
            //         context.TransitionToState(new RoundEndState());
            //     else
            //         context.NextPlayer();
            //         context.TransitionToState(new PlayerTurnState());
        }
        public void Exit(Context context)
        {
            Debug.Log($"Player {context.currentPlayer.DisplayName}'s Turn Ended!");
            context.gameMaster.NextPlayer();
            // any kind of cleanup.
        }
        private void StartPlayerTurn(Player player)
        {
        }
    }
}