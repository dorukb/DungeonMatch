using System;
using UnityEngine;

namespace DorkyProductions.UI
{
    
public class UIMediator : MonoBehaviour
{
    // params: <CurrentHealth>
    public static Action<int> OnLocalPlayerHealthUpdated;
    public static Action<int> OnOpponentPlayerHealthUpdated;
    
    // params: <CurrentShield>
    public static Action<int> OnLocalPlayerShieldUpdated;
    public static Action<int> OnOpponentPlayerShieldUpdated;
}

}