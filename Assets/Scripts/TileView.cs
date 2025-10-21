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
    private RectTransform _rectTransform;
    
    // Store our logical position for input reference
    public Vector2Int GridPosition { get; set; }
    private PlayerInput _inputManager;
    private void Awake()
    {
        _image = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();
    }

    // Called by ClientBoardVisualizer when spawned
    public void Initialize(TileDefinitionSO definition, Vector2Int gridPos, PlayerInput localPlayerInput)
    {
        _image.sprite = definition.tileSprite;
        GridPosition = gridPos;
        _inputManager = localPlayerInput;
    }

    // --- Input Event Implementations ---

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

            if (endTile != null)
            {
                _inputManager.OnTilePointerUp(endTile);
            }
        }
    }
    
    // This handles the "drag-to-swap"
    // public void OnPointerEnter(PointerEventData eventData)
    // {
    //     if (_inputManager != null)
    //     {
    //         _inputManager.OnTilePointerEnter(this);
    //     }
    // }
    // --- Animation Methods ---

    // This is our all-purpose movement method
    public void MoveToPosition(Vector2 anchoredPosition, float duration = 0.3f)
    {
        // Use DOTween to animate the anchoredPosition
        _rectTransform.DOAnchorPos(anchoredPosition, duration)
            .SetEase(Ease.OutCubic);
    }
    
    // We'll use this for the "swap back"
    public void AnimateSwapBack(Vector2 originalPosition, float duration = 0.2f)
    {
        _rectTransform.DOAnchorPos(originalPosition, duration)
            .SetEase(Ease.OutSine);
    }

    // We'll use this for matches
    public void AnimatePop(float duration = 0.2f)
    {
        _rectTransform.DOPunchScale(Vector3.one * 0.2f, duration, 10, 1)
            .OnComplete(() => Destroy(gameObject)); // Simple pop and destroy
    }
}
}