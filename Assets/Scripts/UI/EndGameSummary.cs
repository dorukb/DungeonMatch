using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using DorkyProductions; // Ensure this matches your Portrait script namespace

namespace UI
{
    public class EndGameSummaryView : MonoBehaviour
    {
        [Header("Canvas Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform portraitContainer;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private Button backToMenuButton;

        [Header("Prefabs")]
        [SerializeField] private GameObject portraitPrefab;

        public Button BackToMenuButton => backToMenuButton;

        public void Initialize()
        {
            // Set initial hidden state
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        public void Show(bool isLocalWin, int rewardAmount)
        {
            gameObject.SetActive(true);
            
            // 1. Clear existing portraits (if any)
            foreach (Transform child in portraitContainer) Destroy(child.gameObject);
            
            // 2. Spawn Portraits
            CreatePortrait("YOU", isLocalWin);
            CreatePortrait("OPPONENT", !isLocalWin);

            // 3. Fade in the Panel
            canvasGroup.DOFade(1, 0.5f).SetUpdate(true);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // 4. Animate Coins
            AnimateCoins(rewardAmount);
        }

        private void CreatePortrait(string playerName, bool isWinner)
        {
            GameObject p = Instantiate(portraitPrefab, portraitContainer);
            PlayerPortraitUI ui = p.GetComponent<PlayerPortraitUI>();
            
            if (ui != null)
            {
                // We manually call the setup here, NOT through an event
                ui.SetupForEndGame(playerName, isWinner);
            }
        }

        private void AnimateCoins(int target)
        {
            int current = 0;
            DOTween.To(() => current, x => {
                current = x;
                coinText.text = current.ToString();
            }, target, 1.5f).SetEase(Ease.OutQuad).SetUpdate(true);
        }
    }
}