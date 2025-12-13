using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace DorkyProductions
{
    
[RequireComponent(typeof(RectTransform))]
public class TileView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField]
    private Image _image;
    
    [SerializeField]
    private Image _selectedFrame;
    public RectTransform RectTransform { get; private set; }
    
    // Store our logical position for input reference
    public Vector2Int GridPosition { get; set; }
    private HumanPlayerInput _inputManager;
    public Tile Type { get; private set; }
    
    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        _selectedFrame.gameObject.SetActive(false);
    }

    // Called by ClientBoardVisualizer when spawned
    public void Initialize(TileDefinitionSO definition, Vector2Int gridPos, HumanPlayerInput localHumanPlayerInput, bool useAlternativeSprite = false)
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
        _inputManager = localHumanPlayerInput;
        Type = definition.type;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_inputManager != null)
        {
            _inputManager.OnTilePointerDown(this);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_inputManager != null)
        {
            // We need to find the tile the pointer is *over*
            // This is slightly tricky, as eventData.pointerCurrentRaycast
            // is what we want.
            
            TileView endTile = null;
            if (eventData.pointerCurrentRaycast.gameObject != null)
            {
                endTile = eventData.pointerCurrentRaycast.gameObject.GetComponent<TileView>();
            }

            _inputManager.OnTilePointerUp(endTile);
        }
    }

    public void OnSelectedStateChange(bool isSelected)
    {
        _selectedFrame.gameObject.SetActive(isSelected);
    }

}
}