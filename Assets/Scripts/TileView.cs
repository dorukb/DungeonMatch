using UnityEngine;

// This script is on your tile prefab
public class TileView : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer _spriteRenderer;

    // Called by the ClientBoardVisualizer
    public void Initialize(TileDefinitionSO definition)
    {
        _spriteRenderer.sprite = definition.tileSprite;
    }
}