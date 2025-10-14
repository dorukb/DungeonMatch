using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DorkyProductions.Core;
using DorkyProductions.States;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;

namespace DorkyProductions.Views
{
    
public class PlayerHandView : MonoBehaviour, IHandView
{
    [SerializeField] protected GameObject CardPrefab;
    [SerializeField] RectTransform CardContainer;
    [SerializeField] SplineContainer splineContainer;
    [SerializeField] private RectTransform cardDrawPosition;
    [SerializeField] private float drawCardAnimationDuration = 0.3f;
    [SerializeField] private float cardScaleInHand = 1.0f;
    [FormerlySerializedAs("playerModelID")]
    [Range(0,3)]
    [SerializeField] private int ownerPlayer;
    [SerializeField] ShelterRowView shelterRowView;
    
    
    private Queue<GameObject> toBeDrawn = new Queue<GameObject>();
    private bool isProcessingQueue = false;
    
    protected List<InteractableCard> cards = new List<InteractableCard>();
    private Player myPlayer;
    private void Awake()
    {
        GameStartState.NewGameIsStarting += Init;
    }

    private void Init()
    {
        // this.myPlayer = GameMaster.Instance.GetPlayer(ownerPlayer);
        // this.myPlayer.OnPlayerReceivedCatCard += CreateNewBasicCard;
    }

    public bool IsMyPlayer(int playerID)
    {
        return myPlayer.id == playerID;
    }
    
    public InteractableCard RemoveCardFromHand(Guid cardID)
    {
        InteractableCard foundCardVisuals = null;
        
        foreach (var interactableCard in cards)
        {
            if (interactableCard.Data.id == cardID)
            {
                // Debug.Log($"[VIEW] Removed card from hand animation for card: {interactableCard.Data.title}.");
                foundCardVisuals = interactableCard;
                break;
            }
        }

        if (foundCardVisuals == null)
        {
            Debug.LogError($" PlayerHandView Could not find card with ID {cardID} to remove from hand");
            return null;
        }
        
        // Effectively the Spline calculation will ignore this card.
        foundCardVisuals.transform.SetParent(this.transform, true);
        cards.Remove(foundCardVisuals);
        
        PositionCardsInHandWithSpline(0.4f);
        return foundCardVisuals;
    }


    // TODO: Listen for GameIsEnding/Canceled etc event and remove listeners.
    protected virtual void CreateNewBasicCard(CatCard catCard)
    {  
        GameObject newCard = Instantiate(CardPrefab, Vector3.zero, Quaternion.identity);
        newCard.gameObject.SetActive(false);
        newCard.name = catCard.data.title;
        var card = newCard.GetComponent<InteractableCard>();
        if (card == null)
        {
            Debug.LogError($"The InteractableCard component cannot be null! check prefab: {CardPrefab.name}");
        }
        card.Initialize(catCard.data, true);
        cards.Add(card);
        
        EnqueueDrawAnim(newCard);
    }
    public void EnqueueDrawAnim(GameObject newCard)
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
            GameObject cardToDraw = toBeDrawn.Dequeue();
            // Debug.Log($"[VIEW] Draw card: {cardToDraw.name}");
            cardToDraw.gameObject.SetActive(true);
            cardToDraw.transform.SetParent(CardContainer);
            cardToDraw.transform.localPosition = cardDrawPosition.localPosition;
            cardToDraw.transform.localScale = Vector3.one * 0.5f;
            
            PositionCardsInHandWithSpline(drawCardAnimationDuration);

            yield return new WaitForSeconds(drawCardAnimationDuration);
        }

        isProcessingQueue = false;
    }
    private void PositionCardsInHandWithSpline(float animDuration)
    {
        int maxHandSize = 4;
        int cardCount = CardContainer.childCount;
        float cardSpacing = 1f / maxHandSize;
        float firstPos = 0.5f - (cardCount - 1) * cardSpacing / 2;
        Spline spline = splineContainer.Spline;

        for (int i = 0; i < cardCount; i++)
        {
            float p = firstPos + (i * cardSpacing);
            Vector3 splinePos = spline.EvaluatePosition(p);
            Vector3 forward = spline.EvaluateTangent(p);
            Vector3 up = spline.EvaluateUpVector(p);
            Quaternion rotation = Quaternion.LookRotation(up, Vector3.Cross(up, forward).normalized);
            
            var child  = CardContainer.GetChild(i);
            // Debug.Log($"Child {child.name}, position: {child.localPosition}, TargetPos: {splinePos}");
            child.DOLocalMove(splinePos, animDuration);
            child.transform.DOLocalRotateQuaternion(rotation, animDuration);
            child.DOScale(cardScaleInHand, animDuration);
        }
    }

}
}