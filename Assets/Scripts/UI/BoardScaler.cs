using DorkyProductions.UI;
using UnityEngine;

namespace UI
{
    public class BoardScaler : MonoBehaviour
    {
        [Header("Scaling Settings")]
        public Vector3 localPlayerScale = new Vector3(1f, 1f, 1f);
        public Vector3 opponentPlayerScale = new Vector3(0.5f, 0.5f, 1f); // Shrinks to 85%
        public float scaleSpeed = 5f;

        [Header("Visual Feedback")]
        public SpriteRenderer boardBackground; 
        public Color localPlayerColor = Color.white;
        public Color opponentPlayerColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Grayed out
    
        private void OnEnable()
        {
            UIMediator.OnPlayerTurnStarted += UpdateBoardDisplay;
        }

        private void OnDisable()
        {
            UIMediator.OnPlayerTurnStarted -= UpdateBoardDisplay;
        }
        void UpdateBoardDisplay(PlayerType player, bool isExtra)
        {
            // Determine targets
            Vector3 targetScale = (player == PlayerType.Local) ? localPlayerScale : opponentPlayerScale;
            Color targetColor = (player == PlayerType.Opponent) ? localPlayerColor : opponentPlayerColor;

            // Smoothly Scale
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);

            // Smoothly Change Color (Gray out)
            if (boardBackground != null)
            {
                boardBackground.color = Color.Lerp(boardBackground.color, targetColor, Time.deltaTime * scaleSpeed);
            }
        }
    }
}