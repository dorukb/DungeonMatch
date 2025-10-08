using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DorkyProductions.Core
{
    public class CardRegistry : MonoBehaviour
    {
        public static CardRegistry Instance { get; private set; }

        [SerializeField] 
        private List<SpecialCardBlueprint> specialCards;
        
        [SerializeField]
        private List<CatCardBlueprint> catCards;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else if (Instance != this) // If an instance already exists, destroy this object
            {
                Debug.LogWarning("CardRegistry already exists.Trying to create another one signals something is wrong.");
                Destroy(gameObject);
            }
        }

        public SpecialCardBlueprint GetSpecialCard(SpecialCardType type)
        {
            return specialCards.First(t => t.type == type);
        }

        public CatCardBlueprint GetCatCard(int value)
        {
            return catCards.First(t => t.value == value);
        }
        
        public List<SpecialCardBlueprint> GetSpecialCards()
        {
            return specialCards;
        }

        public List<CatCardBlueprint> GetCatCards()
        {
            return catCards;
        }
        
    }
}