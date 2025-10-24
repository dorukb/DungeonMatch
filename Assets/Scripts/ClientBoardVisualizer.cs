using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
namespace DorkyProductions
{
    
public class ClientBoardVisualizer : MonoBehaviour
{
    [SerializeField]
    private TileDatabase tileDatabase; 

    [SerializeField]
    private GameObject tileViewPrefab; // A prefab with a SpriteRenderer and a TileView.cs script

    [SerializeField]
    public PlayerInput playerInput;
    
    [Header("UI Layout (Read by Client)")]
    [Tooltip("The RectTransform that holds the 5x5 grid UI.")]
    public RectTransform boardContainer;

    [Tooltip("The size (width/height) of a single tile's RectTransform.")]
    public Vector2 tileViewSize = new Vector2(100, 100);

    [Tooltip("The space between adjacent tiles.")]
    public Vector2 tileSpacing = new Vector2(10, 10);
    
    [Header("Animation Settings")]
    public const float SWAP_DURATION = 0.3f;
    public const float FALL_DURATION = 0.5f;
    public const float REMOVE_DURATION = 0.5f;
    public const float SPAWN_DURATION = 0.5f;
    
    // This is our client-side lookup to connect a logical tile (by ID)
    // to its visual GameObject.
    private Dictionary<ushort, TileView> _visualTiles = new Dictionary<ushort, TileView>();
    void Start()
    {
        tileDatabase.Initialize();
    }

    public Tween InitBoard(List<TileState> tiles)
    {
        Sequence s = DOTween.Sequence();
        for (int i = 0; i < tiles.Count; i++)
        {
            var tile = tiles[i];
            var tween = SpawnVisualTile(tile, GameBoard.GetGridPos(i));
            if (tween != null)
            {
                s.Join(tween);
            }
        }
        return s;
    }
    public Tween SpawnVisualTile(TileState state, Vector2Int gridPos)
    {
        TileDefinitionSO def = tileDatabase.GetTileByType(state.tileType);
        if (def == null)
        {
            Debug.LogError($"[CLIENT] Spawn, Tile definition not found for type: {state.tileType}");
            return null;
        }

        // 1. Locally instantiate as a child of the board container
        GameObject tileGO = Instantiate(tileViewPrefab, boardContainer);

        // 2. Set its starting scale to 0 (so it's invisible)
        tileGO.transform.localScale = Vector3.zero;
     
        // 2. Setup the RectTransform Spawn Position
        RectTransform rt = tileGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tileViewSize;
        
        Vector2 anchoredPos = GetAnchoredPosition(gridPos);
        rt.anchoredPosition = anchoredPos;
        
        TileView tileView = tileGO.GetComponent<TileView>();
        tileView.Initialize(def, gridPos, playerInput);
        
        // 5. Add to our dictionary for tracking
        _visualTiles[state.uniqueID] = tileView;
        
        var spawnAnim = tileGO.transform.DOScale(1f, SPAWN_DURATION).SetEase(Ease.InQuint);
        return spawnAnim;
    }
    
    public Vector2 GetAnchoredPosition(Vector2Int gridPos)
    {
        Vector2 fullTileSize = tileViewSize + tileSpacing;

        // This calculation centers the grid (0,0) at the container's pivot
        float x = (gridPos.x - (GameBoard.BoardWidth - 1) / 2.0f) * fullTileSize.x;
        float y = (gridPos.y - (GameBoard.BoardHeight - 1) / 2.0f) * fullTileSize.y;

        return new Vector2(x, y);
    }
    public Tween AnimateSwap(ushort firstTileID, ushort secondTileID)
    {
        TileView firstTileView = _visualTiles[firstTileID];
        TileView secondTileView = _visualTiles[secondTileID];
        
        Vector2Int firstGridPos = firstTileView.GridPosition;
        var tween1 = MoveTile(firstTileView, secondTileView.GridPosition, SWAP_DURATION);
        var tween2 = MoveTile(secondTileView, firstGridPos, SWAP_DURATION);
        
        // A sequence of tweens are executed in Parallel, sequence returns when all are finished.
        Sequence s = DOTween.Sequence();
        s.Join(tween1);
        s.Join(tween2);
        
        return s; 
    }

    private Tween MoveTile(TileView tileToMove, Vector2Int newGridPos, float duration)
    {
        Vector2 newAnchoredPos = GetAnchoredPosition(newGridPos);
        tileToMove.GridPosition = newGridPos;
        
        return tileToMove.RectTransform.DOAnchorPos(newAnchoredPos, duration)
            .SetEase(Ease.OutCubic);
    }

    public Tween AnimateFall(ushort tileId, Vector2Int toPos)
    {
        if (_visualTiles.TryGetValue(tileId, out TileView tileView))
        {
            return MoveTile(tileView, toPos, FALL_DURATION);
        }
        else
        {
            Debug.LogError($"AnimateFall failed, tile ${tileId} not found");
            return null;
        }
    }
    // We'll use this for matches
    public Tween AnimatePop(List<ushort> tileIdsToPop)
    {
        Sequence s = DOTween.Sequence();
        foreach (ushort tileId in tileIdsToPop)
        {
            TileView tileView = _visualTiles[tileId];
            var tween = tileView.RectTransform.DOPunchScale(Vector3.one * 0.2f, REMOVE_DURATION, 10, 1);
            s.Join(tween);
        }

        s.onComplete = () =>
        {
            foreach (ushort tileId in tileIdsToPop)
            {
                TileView tileView = _visualTiles[tileId];
                _visualTiles.Remove(tileId);
                Destroy(tileView.gameObject);
            }
        };
        return s;
    }
}
}