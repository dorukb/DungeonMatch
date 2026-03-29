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

    // params: <Player, IsExtraTurn, IsReasonChest>
    public static Action<PlayerType, bool, bool> OnPlayerTurnStarted;
    
    // params: <Player, HasChest>
    public static Action<PlayerType, bool> OnPlayerChestUpdated;
    
    //params: <SkillId>
    public static Action<int> OnPlayerChestOpened;
    
    //params: <>
    public static Action OnPlayerChestEnded;
    
    // params: <WinnerPlayer, betAmount>
    public static Action<PlayerType, int> OnGameEnded;
    
    // params: <Player>
    public static Action OnGameStarted;
    
    //params: <betAmount>
    public static Action<int> OnBetAmountIsGot;
}

}