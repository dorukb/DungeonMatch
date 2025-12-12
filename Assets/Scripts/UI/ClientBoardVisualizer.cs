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
    
    [Header("AI Delay Event Fields")]
    [SerializeField] private CanvasGroup thinkingUI;
    [SerializeField] private RectTransform thinkingIcon; 
    
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
    
        // Settings for the "Rain" feel
        float staggerPerTile = 0.05f; // How long to wait between each tile falling
    
        for (int i = 0; i < tiles.Count; i++)
        {
            var tile = tiles[i];
            Vector2Int gridPos = GameBoard.GetGridPos(i); // Assuming this returns accurate x,y
        
            // Option B: "Matrix" style rain (Columns fall together, staggered slightly)
            // float delay = gridPos.x * 0.1f + gridPos.y * 0.05f;

            // Option C: Diagonal Wave (Bottom-Left to Top-Right) - RECOMMENDED
            // This prevents upper tiles from visually passing through lower tiles
            float delay = (gridPos.x + gridPos.y) * staggerPerTile; 
            var tween = SpawnVisualTile(tile, gridPos);
            if (tween != null)
            {
                // Instead of Join (simultaneous), we Insert at a specific timestamp
                s.Insert(delay, tween);
            }
        }
    
        return s;
    }
    public Tween SpawnVisualTile(TileState state, Vector2Int gridPos)
    {
        TileDefinitionSO def = tileDatabase.GetTileByType(state.type);
        if (def == null)
        {
            Debug.LogError($"[CLIENT] Spawn, Tile definition not found for type: {state.type}");
            return null;
        }

        // 1. Locally instantiate as a child of the board container
        GameObject tileGO = Instantiate(tileViewPrefab, boardContainer);
        tileGO.name = "Tile"+ state.uniqueID.ToString();
        // 2. Set its starting scale to 0 (so it's invisible)
        // tileGO.transform.localScale = Vector3.zero;
     
        // 2. Setup the RectTransform Spawn Position
        RectTransform rt = tileGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tileViewSize;
        
        Vector2 targetPos = GetAnchoredPosition(gridPos);
        int dropHeightInRows = GameBoard.BoardHeight + 1; 
        Vector2Int startGridPos = new Vector2Int(gridPos.x, gridPos.y + dropHeightInRows);
        Vector2 spawnPos = GetAnchoredPosition(startGridPos);
        rt.anchoredPosition = spawnPos;
        
        TileView tileView = tileGO.GetComponent<TileView>();
        bool useAlternativeSprite = state.isDoubleEffect && (state.type == Tile.Attack || state.type == Tile.Heal);
        tileView.Initialize(def, gridPos, playerInput, useAlternativeSprite);
        
        // 5. Add to our dictionary for tracking
        _visualTiles[state.uniqueID] = tileView;
        
        var spawnAnim = rt.DOAnchorPos(targetPos, SPAWN_DURATION).SetEase(Ease.OutBack, 0.5f);
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
        return tileToMove.RectTransform.DOAnchorPos(newAnchoredPos, duration).SetEase(Ease.OutBack, 0.5f);
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
            if (!_visualTiles.ContainsKey(tileId))
            {
                Debug.LogError($"AnimatePop failed, tile ${tileId} already popped");
                continue;
            }
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
    public Tween AnimateAIDelay(float eDuration)
    {
        Sequence s = DOTween.Sequence();
            
        // 1. Safety Check
        if (eDuration < 0.2f) return s.AppendInterval(eDuration);

        float fadeInTime = 0.25f;
        float fadeOutTime = 0.25f;
            
        // 2. Setup
        s.OnStart(() => {
            // Ensure the parent handles visibility...
            thinkingUI.alpha = 0;
            thinkingUI.gameObject.SetActive(true);
                
            // ...and the child is ready to pop up
            thinkingIcon.localScale = Vector3.one; 
        });

        // 3. Fade In (Parent) & Pop Up (Child)
        // Because Icon is a child, it fades in with the parent automatically.
        s.Append(thinkingUI.DOFade(1f, fadeInTime));
            
        // We animate scale on the child ONLY so the bubble background doesn't wobble
        s.Join(thinkingIcon.DOScale(1.2f, fadeInTime).SetEase(Ease.OutBack));

        // 4. "Breathing" Pulse (Looping on Child)
        s.Append(thinkingIcon.DOScale(1.0f, 0.5f).SetLoops(5, LoopType.Yoyo));

        // 5. Wait for the core duration
        s.AppendInterval(eDuration - fadeInTime - fadeOutTime);

        // 6. Fade Out
        s.AppendCallback(() => thinkingIcon.DOKill()); 
        s.Append(thinkingUI.DOFade(0f, fadeOutTime));
        s.Join(thinkingIcon.DOScale(0f, fadeOutTime).SetEase(Ease.InBack));

        // 7. Cleanup
        s.OnComplete(() => {
            thinkingUI.gameObject.SetActive(false);
        });

        return s;
    }
}
}