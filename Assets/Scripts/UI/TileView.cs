using UnityEngine;
using UnityEngine.UI;

namespace DorkyProductions
{
    
[RequireComponent(typeof(RectTransform))]
public class TileView : MonoBehaviour
{
    [SerializeField]
    private Image _image;
    
    [SerializeField]
    private Image _selectedFrame;
    public RectTransform RectTransform { get; private set; }
    
    // Store our logical position for input reference
    public Vector2Int GridPosition { get; set; }
    public Tile Type { get; private set; }
    
    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        _selectedFrame.gameObject.SetActive(false);
    }

    // Called by ClientBoardVisualizer when spawned
    public void Initialize(TileDefinitionSO definition, Vector2Int gridPos, bool useAlternativeSprite = false)
    {
        if (useAlternativeSprite)
        {
            _image.sprite = definition.tileSprite2x;
        }
        else
        {
            _image.sprite = definition.tileSprite;
        }
        GridPosition = gridPos;
        Type = definition.type;
    }
    public void SetSelected(bool isSelected)
    {
        _selectedFrame.gameObject.SetActive(isSelected);
    }
}
}