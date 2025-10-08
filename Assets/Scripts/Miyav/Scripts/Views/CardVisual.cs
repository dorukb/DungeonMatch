using UnityEngine;

namespace DorkyProductions.Views
{
    public abstract class CardVisual : MonoBehaviour
    {
        public abstract void Setup(Sprite sprite, bool isSpecial);
        public abstract void OnPlayedFromHand();
    }
}