using DG.Tweening;
using TMPro;
using UnityEngine;

namespace DorkyProductions.UI
{
    public class TurnDisplayUI : MonoBehaviour
    {
        [SerializeField] private GameObject turnTextObject;
        [SerializeField] private TextMeshProUGUI turnText;
        private Tween _hideTimer;
        
        private void OnEnable()
        {
            turnTextObject.gameObject.SetActive(false);
            UIMediator.OnPlayerTurnStarted += UpdateTurnText;
        }

        private void OnDisable()
        {
            UIMediator.OnPlayerTurnStarted -= UpdateTurnText;
            turnTextObject.gameObject.SetActive(false);
            _hideTimer?.Kill();
        }

        private void UpdateTurnText(PlayerType player, bool isExtra)
        {

            if (player == PlayerType.Local)
            {
                turnText.text = "Your Turn " + (isExtra ? "(Extra!)" : "");
            }
            else
            {
                turnText.text = "Opponent's Turn" + (isExtra ? "(Extra!)" : "");
            }
            
            turnTextObject.gameObject.SetActive(true);

            // 2. Kill only the previous timer if it exists
            // This won't affect other tweens on this transform
            _hideTimer?.Kill();

            // 3. Create a new delayed call
            _hideTimer = DOVirtual.DelayedCall(1.0f, () => 
                {
                    turnTextObject.gameObject.SetActive(false);
                })
                .SetLink(gameObject); // Senior Tip: Auto-kills tween if object is destroyed

        }

        public void OverwriteTurnText(string text)
        {
            turnText.text = text;

            // If you call OverwriteTurnText, you might also want to schedule a hide call:
            // CancelInvoke(HideFunctionName);
            // turnText.gameObject.SetActive(true);
            // Invoke(HideFunctionName, 3.0f);
        }
    }
}