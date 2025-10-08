using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DorkyProductions.Views
{
    
public class BasicCardVisual : CardVisual
{
    [SerializeField] Image image;
    [FormerlySerializedAs("shaderController")] [SerializeField] private CardShaderController cardShaderController;
    
    public override void Setup(Sprite sprite, bool isSpecial)
    {
        image.sprite = sprite;
        if (isSpecial)
        {
            cardShaderController.ShowSpecialCardVisuals();
        }
        else
        {
            cardShaderController.ShowRegularCardVisuals();
        }
    }

    public override void OnPlayedFromHand()
    {
        // maybe disable interaction component here?
        // We dont have it though. might need to refactor the prefab setup.
    }
}

}