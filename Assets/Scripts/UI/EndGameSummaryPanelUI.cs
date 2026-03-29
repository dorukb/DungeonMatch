using System.IO.Ports;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class EndGameSummaryPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject localWinText;
    [SerializeField] private GameObject opponentWinText;
    [SerializeField] private RectTransform coinDisplay;
    [SerializeField] private TextMeshProUGUI rewardCoinAmount;
    [SerializeField] private RectTransform backToMenuButton;

    [SerializeField] private float punchScale = 1.1f;
    private Vector3 _punchScale;
    private float scaleUpDuration = 0.25f;
    private float scalePunchDuration = 0.15f;
    
    private Vector3 menuButtonOriginalScale;
    void Awake()
    {
        _punchScale= new Vector3(punchScale, punchScale, punchScale);
        localWinText.SetActive(false);
        opponentWinText.SetActive(false);
        coinDisplay.gameObject.SetActive(false);
        coinDisplay.transform.localScale = Vector3.zero;
        backToMenuButton.gameObject.SetActive(false);
    }
    public void Show(bool isLocalWin, int betAmount)
    {
        Debug.Log("Show");
        rewardCoinAmount.text = betAmount.ToString();
        coinDisplay.gameObject.SetActive(true);
        backToMenuButton.gameObject.SetActive(true);
        menuButtonOriginalScale = backToMenuButton.localScale;
        backToMenuButton.localScale = Vector3.zero;
        
        Sequence seq = DOTween.Sequence(); 
        if (isLocalWin)
        {
            opponentWinText.SetActive(false);
            localWinText.SetActive(true);
            seq.Append(localWinText.transform.DOScale(Vector3.one, scaleUpDuration));
            seq.Append(localWinText.transform.DOPunchScale(_punchScale, scalePunchDuration));
        }
        else
        {
            localWinText.SetActive(false);
            opponentWinText.SetActive(true);
            seq.Append(opponentWinText.transform.DOScale(Vector3.one, scaleUpDuration));
            seq.Append(opponentWinText.transform.DOPunchScale(_punchScale, scalePunchDuration));
        }
        seq.Append(coinDisplay.transform.DOScale(Vector3.one, scaleUpDuration));
        seq.Append(coinDisplay.DOPunchScale(_punchScale, scalePunchDuration));
        seq.Append(backToMenuButton.DOScale(menuButtonOriginalScale, 0.3f));
        seq.Append(backToMenuButton.DOPunchScale(menuButtonOriginalScale * 1.15f, scalePunchDuration));
    }
}
