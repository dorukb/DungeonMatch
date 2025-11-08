using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;


namespace DorkyProductions
{
    
[RequireComponent(typeof(Image), typeof(RectTransform))]
public class TileView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField]
    private Image _image;
    public RectTransform RectTransform { get; private set; }
    
    // Store our logical position for input reference
    public Vector2Int GridPosition { get; set; }
    private PlayerInput _inputManager;
    private void Awake()
    {
        _image = GetComponent<Image>();
        RectTransform = GetComponent<RectTransform>();
    }

    // Called by ClientBoardVisualizer when spawned
    public void Initialize(TileDefinitionSO definition, Vector2Int gridPos, PlayerInput localPlayerInput, bool useAlternativeSprite = false)
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
        _inputManager = localPlayerInput;
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

}
}