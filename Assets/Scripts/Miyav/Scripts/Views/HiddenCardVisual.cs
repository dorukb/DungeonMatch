using UnityEngine;
using UnityEngine.UI;

namespace DorkyProductions.Views
{
    public class HiddenCardVisual : CardVisual
    {
        [SerializeField] Image image;
        [SerializeField] Sprite hiddenCardSprite;

        private Sprite actualCardImage;
        public override void Setup(Sprite sprite, bool isSpecial)
        {
            // ignore the actual sprite, just show the hidden one.
            image.sprite = hiddenCardSprite;
            image.color = Color.black;
            this.actualCardImage = sprite;
        }

        public override void OnPlayedFromHand()
        {
            // make it visible again.
            image.sprite = actualCardImage;
            image.color = Color.white;
        }
    }
}