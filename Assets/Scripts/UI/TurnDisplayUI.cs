using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace DorkyProductions.UI
{
    public class TurnDisplayUI : MonoBehaviour
    {
        [SerializeField] private GameObject turnTextObject;
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private float turnNotificationDuration = 3.0f;
        [SerializeField] private float offScreenOffset = 1000f; // Distance to left/right
        [SerializeField] private RectTransform turnDisplayParent;
        [SerializeField] private GameObject boardGlow;

        private void Awake()
        {
            boardGlow.SetActive(false);
            turnTextObject.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            UIMediator.OnPlayerTurnStarted += UpdateTurnText;
        }

        private void OnDisable()
        {
            UIMediator.OnPlayerTurnStarted -= UpdateTurnText;
        }
        private void UpdateTurnText(PlayerType player, bool isExtra)
        {
            bool isLocalPlayersTurn = player == PlayerType.Local;
            if (isLocalPlayersTurn)
            {
                turnText.text = "Your Turn " + (isExtra ? "(Extra)" : "");
            }
            else
            {
                turnText.text = "Opponent's Turn" + (isExtra ? "(Extra)" : "");
            }
            
            PlayTurnStartedAnimation(isLocalPlayersTurn);
        }
        private void PlayTurnStartedAnimation(bool isLocalPlayersTurn)
        {
            turnTextObject.gameObject.SetActive(true);
            if (isLocalPlayersTurn)
            {
                boardGlow.gameObject.SetActive(true);
            }
            
            // 1. Calculate the time unit
            float unit = turnNotificationDuration / 3f;

            // 2. Setup initial position (Off-screen left)
            turnDisplayParent.anchoredPosition = new Vector2(-offScreenOffset, turnDisplayParent.anchoredPosition.y);
            turnDisplayParent.localScale = Vector3.one;

            Sequence turnSequence = DOTween.Sequence();

            turnSequence
                // PHASE 1: Appear from left (1 unit)
                .Append(turnDisplayParent.DOAnchorPosX(0, unit / 2.0f).SetEase(Ease.OutBack))
            
                // PHASE 2: Attention Grabber in center (2 units), A slight pulse/scale effect
                .Append(turnDisplayParent.DOScale(1.1f, unit).SetEase(Ease.InOutSine))
                .Append(turnDisplayParent.DOScale(1.0f, unit).SetEase(Ease.InOutSine))
            
                // PHASE 3: Disappear to the right (1 unit)
                .Append(turnDisplayParent.DOAnchorPosX(offScreenOffset, unit / 2.0f).SetEase(Ease.InBack))
            
                .OnComplete(() => {
                    Debug.Log("Turn animation finished.");
                    turnTextObject.gameObject.SetActive(false);
                    if (isLocalPlayersTurn)
                    {
                        boardGlow.gameObject.SetActive(false);
                    }
                });
        }
       
    }
}