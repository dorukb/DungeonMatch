using UnityEngine;
using TMPro;
using DG.Tweening;
using System;

namespace UI
{
    public class ResultDisplayUI : MonoBehaviour
    {
        [SerializeField] private GameObject resultDisplayObject;
        [SerializeField] private RectTransform resultDisplayParent;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private float offScreenOffset = 1200f;
        
        private void Awake()
        {
            resultDisplayObject.gameObject.SetActive(false);
        }
        public void PlayResultAnimation(bool isWin, float duration, Action onComplete)
        {
            // Set text and color based on the bool and Inspector settings
            resultText.text = isWin ? "YOU WIN!" : "OPPONENT WINS :((";
            
            resultDisplayObject.gameObject.SetActive(true);

            // Animation Logic
            resultDisplayParent.anchoredPosition = new Vector2(-offScreenOffset, 0);
            float segment = duration / 4f;

            Sequence s = DOTween.Sequence();
            s.Append(resultDisplayParent.DOAnchorPosX(0, segment).SetEase(Ease.OutBack))
                .Append(resultDisplayParent.DOScale(1.2f, segment).SetEase(Ease.InOutSine))
                .Append(resultDisplayParent.DOScale(1.0f, segment).SetEase(Ease.InOutSine))
                .Append(resultDisplayParent.DOAnchorPosX(offScreenOffset, segment).SetEase(Ease.InBack))
                .OnComplete(() => {
                    gameObject.SetActive(false);
                    onComplete?.Invoke();
                });
        }
    }
}