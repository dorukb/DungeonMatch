using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using DorkyProductions.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DorkyProductions.Views
{
    internal class CardView
    {
        public CatCardData data;
        public GameObject cardGO;
        public RowLayout parentRow;
        public Slot slot;
        public CardView(CatCardData data, GameObject cardGO, RowLayout parentRow, Slot slot)
        {
            this.data = data;
            this.cardGO = cardGO;
            this.parentRow = parentRow;
            this.slot = slot;
        }
    }

    [Serializable]
    public class PlayerUIPosition
    {
        public RectTransform pile;
        public RectTransform capture;
        public int ownerPlayerId;
    }
    
    public class ShelterRowView : MonoBehaviour
    {
        [SerializeField] private GameObject CardPrefab;
        [SerializeField] List<RowLayout> rows = new List<RowLayout>();
        
        [SerializeField] private RectTransform cardDrawPosition;
        
        [SerializeField] private List<PlayerUIPosition> playerUIPositions;
        
        [Range(1,8)]
        [SerializeField] private int maxCardsInRow;
    
        [SerializeField] private float cardScaleInShelterRow = 0.6f;
        [SerializeField] private float cardScaleInCapturePosition = 0.7f;
        [SerializeField] private float cardScaleInPile =  0.4f;
        [SerializeField] private float cardDealFromDeckDuration = 0.75f;
        [SerializeField] private float cardGetFromHandDuration = 0.4f;
        [SerializeField] private Quaternion cardRotationInPile = Quaternion.identity;
        
        private List<CardView> cardViews = new List<CardView>();
        private Queue<CardView> toBeDrawn = new Queue<CardView>();   
        
        private bool isProcessingQueue = false;

        private void OnEnable()
        {
            ShelterRow.OnDealtCardToShelterRow += AnimateDealingToShelter;
        }
        private void OnDisable()
        {
            ShelterRow.OnDealtCardToShelterRow -= AnimateDealingToShelter;
        }

        private void Awake()
        {
            foreach (RowLayout row in rows)
            {
                row.SetupSlots(maxCardsInRow);
            }
        }

        public void ShowCapture(InteractableCard playedCard, List<Guid> cardIDsToCapture, int capturingPlayerIdx)
        {
            playedCard.enabled = false;
            playedCard.OnPlayedFromHand();
            
            PlayerUIPosition playerUIPosition = playerUIPositions.Find(t => t.ownerPlayerId == capturingPlayerIdx);
            
            List<GameObject> cardsToCapture = new List<GameObject>();
            foreach (var id in cardIDsToCapture)
            {
                var foundCard = cardViews.FirstOrDefault(t => t.data.id == id);
                if (foundCard != null)
                {
                    foundCard.parentRow.RemoveChild(foundCard.cardGO.transform as RectTransform, foundCard.data.id);
                    foundCard.cardGO.transform.SetParent(this.transform, true);
                    // Debug.Log("found for capture:" + foundCard.cardGO.name);
                    cardsToCapture.Add(foundCard.cardGO);
                    cardViews.Remove(foundCard);
                }
            }

            StartCoroutine(CapturingAnim(playedCard, cardsToCapture, playerUIPosition));
        }

        private IEnumerator CapturingAnim(InteractableCard playedCard, List<GameObject> cardsToCapture, PlayerUIPosition playerUIPosition)
        {
            float playedCardMoveDuration = 0.5f;
            float capturedCardsMoveDuration = 1.0f;
            float allCardsMoveToPileDuration = 0.75f;
            
            // Note: Last one to be added as Child, will be shown ON TOP, due to Canvas internal logic.
            
            // First move Played Card to "Capture Location
            playedCard.transform.SetParent(this.transform, true);
            MoveRotateScaleInLocal(playedCard.transform, playedCardMoveDuration, playerUIPosition.capture.localPosition, Quaternion.identity, cardScaleInCapturePosition);  
            yield return new WaitForSeconds(playedCardMoveDuration);
            
            // Then move captured cards to "Captured Location"
            foreach (var card in cardsToCapture)
            {
                MoveRotateScaleInLocal(card.transform, capturedCardsMoveDuration, playerUIPosition.capture.localPosition, Quaternion.identity, cardScaleInCapturePosition);
            }
            yield return new WaitForSeconds(capturedCardsMoveDuration);
            
            // Lastly, move all of them to the capturing Player's Pile location.
            foreach (var card in cardsToCapture)
            {
                MoveRotateScaleInLocal(card.transform, allCardsMoveToPileDuration, playerUIPosition.pile.localPosition, cardRotationInPile, cardScaleInPile);
            }
            MoveRotateScaleInLocal(playedCard.transform, allCardsMoveToPileDuration, playerUIPosition.pile.localPosition, cardRotationInPile, cardScaleInPile);
            yield return new WaitForSeconds(allCardsMoveToPileDuration);
            
            // TODO: This part will be handled by PlayerPileScript, once it exists.
            // parent them to the pile instead.
            foreach (var card in cardsToCapture)
            {
                card.transform.SetParent(playerUIPosition.pile, true);
            }
            playedCard.transform.SetParent(playerUIPosition.pile, true);
            
            cardsToCapture.Clear();
        }
        public void GetCardFromHand(InteractableCard newCard)
        {
            RowLayout row = GetAvailableRow();
            var slot = row.GetFirstEmptySlot();
            newCard.OnPlayedFromHand();
            
            // To keep track of which cards are currently displayed.
            cardViews.Add(new CardView(newCard.Data, newCard.gameObject, row, slot));
            newCard.enabled = false;
            
            Transform cardTransform = newCard.transform;
            cardTransform.SetParent(row.GetAnchor(), worldPositionStays: true);
            row.AddChild(cardTransform as RectTransform, newCard.Data.id, slot);
            
            MoveCardToShelterRow(cardTransform, cardGetFromHandDuration, slot.GetPosition());
        }

        private void MoveRotateScaleInLocal(Transform t, float duration, Vector2 targetPosition, Quaternion rotation, float scale)
        {
            Sequence s = DOTween.Sequence();
            s.Append(t.DOLocalMove(targetPosition, duration).SetEase(Ease.OutSine));
            s.Insert(0, t.DOLocalRotateQuaternion(rotation, duration).SetEase(Ease.InQuad));
            s.Insert(0, t.DOScale(scale, duration));
            s.Play();
        }
        private void MoveCardToShelterRow(Transform cardTransform, float duration, Vector2 targetPosition)
        {
            float randomTilt = Random.Range(-5f, 5f);
            Quaternion tiltRotation = Quaternion.Euler(0f, 0f, randomTilt);
            // shelter row is supposed to be far away, but we're on Orthographic camera, so scale down.
            MoveRotateScaleInLocal(cardTransform, duration, targetPosition, tiltRotation, cardScaleInShelterRow);
        }
        private void AnimateDealingToShelter(CatCardData cardData)
        {
            RowLayout row = GetAvailableRow();
            var slot = row.GetFirstEmptySlot();
            
            GameObject newCardGO = Instantiate(CardPrefab, Vector3.zero, Quaternion.identity, row.GetAnchor());
            newCardGO.SetActive(false);
            row.AddChild(newCardGO.transform as RectTransform, cardData.id, slot);
            
            // To keep track of which cards are currently displayed.
            var cardView = new CardView(cardData, newCardGO, row, slot);
            cardViews.Add(cardView);
            
            var interactionComponent = newCardGO.GetComponent<InteractableCard>();
            if (interactionComponent != null)
            {
                interactionComponent.enabled = false;
            }
            
            var cardVisual = newCardGO.GetComponent<CardVisual>();
            if (cardVisual == null)
            {
                Debug.LogError($"The card visuals cannot be null! check prefab: {CardPrefab.name}");
            }
            cardVisual.Setup(cardData.sprite, isSpecial: false);
            
            // Actually Draw with animation.
            EnqueueDrawAnim(cardView);
        } 
        private void EnqueueDrawAnim(CardView newCard)
        {
            toBeDrawn.Enqueue(newCard);
            if (!isProcessingQueue)
            {
                StartCoroutine(ProcessQueuedDrawAnimations());
            }
        }

        private IEnumerator ProcessQueuedDrawAnimations()
        {
            isProcessingQueue = true;
            while (toBeDrawn.Count > 0)
            {
                CardView cardView = toBeDrawn.Dequeue();
                // Debug.Log($"[VIEW] Draw card: {cardToDraw.name}");
                cardView.cardGO.SetActive(true);
                cardView.cardGO.transform.localPosition = cardDrawPosition.localPosition;
                cardView.cardGO.transform.localScale = Vector3.one * 0.5f;
                MoveCardToShelterRow(cardView.cardGO.transform, cardDealFromDeckDuration, cardView.slot.GetPosition());
                yield return new WaitForSeconds(cardDealFromDeckDuration);
            }
            isProcessingQueue = false;
        }

        private RowLayout GetAvailableRow()
            {
                foreach (var row in rows)
                {
                    if (row.HasEmptySlot())
                    {
                        return row;
                    }
                }
                Debug.LogError("No rows available!");
                return null;
            }
        }
    }