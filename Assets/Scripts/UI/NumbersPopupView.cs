using UnityEngine;
using TMPro;

namespace DorkyProductions.UI
{
    
public class NumbersPopupView : MonoBehaviour
{
    public TextMeshProUGUI TextMesh;
    public CanvasGroup CanvasGroup;
    public RectTransform RectTransform; // Cache RectTransform for performance

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
    }
}
}