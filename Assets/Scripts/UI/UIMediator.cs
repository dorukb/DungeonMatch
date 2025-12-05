using System;
using UnityEngine;

namespace DorkyProductions.UI
{
public enum PlayerType
{
    Local,
    Opponent
}
public class UIMediator : MonoBehaviour
{
    // params: <Player, CurrentHealth>
    public static Action<PlayerType, int> OnPlayerHealthUpdated;
    
    // params: <Player, CurrentShield>
    public static Action<PlayerType, int> OnPlayerShieldUpdated;
        
    // params: <Player, CurrentMultiplier>
    public static Action<PlayerType, float> OnPlayersCrossMultiplierUpdated;

    // params: <Player, TurnStartReason>
    public static Action<PlayerType, TurnStartReason> OnPlayerTurnStarted;
    
    // params: <Player, HasChest>
    public static Action<PlayerType, bool> OnPlayerChestUpdated;
    
    // params: <Player>
    public static Action<PlayerType> OnGameEnded;
}

}