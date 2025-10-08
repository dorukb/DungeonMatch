using DorkyProductions.Core;
using UnityEngine;

namespace DorkyProductions
{
    public class EffectHiss : IEffect
    {
        public void Execute(Context context)
        {
            Debug.Log("Force another player to return their two most recently adopted cats to the bottom of the Draw Pile.");
        }
    }
}