using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System.Text; // For StringBuilder
using DorkyProductions.UI;

namespace DorkyProductions
{
public enum StatType { Health, Shield }
public class MatchEffectsController : MonoBehaviour
{
    [SerializeField] private NumbersPopupView prefab;
    [SerializeField] private RectTransform popupContainer;
    [SerializeField] private int poolSize = 5;

    [Header("Anchors (Popup Appear Locations)")]
    [SerializeField] private RectTransform playerHealthAnchor;
    [SerializeField] private RectTransform playerShieldAnchor;
    [SerializeField] private RectTransform enemyHealthAnchor;
    [SerializeField] private RectTransform enemyShieldAnchor;

    [Header("Visual Settings")]
    [SerializeField] private float floatDistance = 50f;
    [SerializeField] private float duration = 1.0f;
    [SerializeField] private Color healthDmgColor = Color.red;
    [SerializeField] private Color shieldDmgColor = Color.cyan;
    [SerializeField] private Color healColor = Color.green;
    [SerializeField] private Color shieldGainColor = new Color(0, 0.5f, 1f); // Darker Blue
    [SerializeField] private float jitterAmountX = 10f;
    
    private Stack<NumbersPopupView> _pool;
    private StringBuilder _sb = new StringBuilder(16); // Reusable string builder

    private void Awake()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        _pool = new Stack<NumbersPopupView>();

        for (int i = 0; i < poolSize; i++)
        {
            NumbersPopupView instance = Instantiate(prefab, popupContainer);
            instance.gameObject.SetActive(false);
            _pool.Push(instance);
        }
    }

    /// Displays a floating number and returns the Tween for the animation queue.
    public Tween ShowValue(int amount, PlayerType player, StatType stat)
    {
        if (_pool.Count == 0)
        {
            Debug.LogWarning("DamagePopup Pool exhausted! Consider increasing pool size.");
            return null;
        }

        NumbersPopupView popup = _pool.Pop();
        popup.gameObject.SetActive(true);

        RectTransform targetAnchor = GetAnchor(player, stat);
        // We match global position first to snap the popup to the target anchor
        popup.RectTransform.position = targetAnchor.position;
        
        // Reset scale (crucial because PunchScale modifies it)
        popup.RectTransform.localScale = Vector3.one;
        

        // 2. Format Text (Garbage Free)
        _sb.Clear();
        if (amount > 0) _sb.Append('+');
        _sb.Append(amount); 
        popup.TextMesh.SetText(_sb);
        popup.TextMesh.color = GetColor(amount, stat);

        return PlayAnimation(popup);
    }

    private Tween PlayAnimation(NumbersPopupView popup)
    {
        // Reset state
        popup.CanvasGroup.alpha = 1f;
        popup.transform.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();

        // Jitter position slightly so they don't overlap perfectly
        float jitterX = Random.Range(-jitterAmountX, jitterAmountX);
        Vector3 moveOffset = new Vector3(jitterX, floatDistance, 0);

        // A. Punch Scale (Juiciness)
        seq.Join(popup.transform.DOPunchScale(Vector3.one * 0.5f, 0.3f, 10, 1));

        // B. Move Upwards
        seq.Join(popup.transform.DOLocalMove(popup.transform.localPosition + moveOffset, duration)
            .SetEase(Ease.OutCirc));

        // C. Fade Out
        seq.Insert(duration * 0.5f, popup.CanvasGroup.DOFade(0f, duration * 0.5f));

        // D. Cleanup (Return to pool)
        seq.OnComplete(() => ReturnToPool(popup));

        // Important: Return the sequence so the Animation Queue can yield on it
        return seq;
    }

    private void ReturnToPool(NumbersPopupView popup)
    {
        popup.gameObject.SetActive(false);
        _pool.Push(popup);
    }
    private RectTransform GetAnchor(PlayerType unit, StatType stat)
    {
        if (unit == PlayerType.Local)
            return stat == StatType.Health ? playerHealthAnchor : playerShieldAnchor;
        else
            return stat == StatType.Health ? enemyHealthAnchor : enemyShieldAnchor;
    }
    private Color GetColor(int amount, StatType stat)
    {
        if (amount < 0) // Damage
        {
            return stat == StatType.Health ? healthDmgColor : shieldDmgColor;
        }
        else // Healing / Gain
        {
            return stat == StatType.Health ? healColor : shieldGainColor;
        }
    }
}

}