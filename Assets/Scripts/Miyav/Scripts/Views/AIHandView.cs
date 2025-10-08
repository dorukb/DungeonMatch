using UnityEngine;

namespace DorkyProductions.Views
{
    public class AIHandView : PlayerHandView
    {
        protected override void CreateNewBasicCard(CatCard catCard)
        {  
            GameObject newCard = Instantiate(CardPrefab, Vector3.zero, Quaternion.identity);
            newCard.gameObject.SetActive(false);
            newCard.name = catCard.data.title;
            var card = newCard.GetComponent<InteractableCard>();
            if (card == null)
            {
                Debug.LogError($"The InteractableCard component cannot be null! check prefab: {CardPrefab.name}");
            }
            
            // Important: This is how AI Cards are NOT interactable.
            // They have the Component (some other classes rely on it atm) but its NOT ACTIVE.
            // TODO: if this is really the only change, just make it a field: "IsCardsInteractable" and set to false for AI.
            // Dont need this class & override at all:)
            card.Initialize(catCard.data, false);
            cards.Add(card);
            EnqueueDrawAnim(newCard);
        }
    }
}