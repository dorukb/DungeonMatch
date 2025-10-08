using System;
using UnityEngine;

namespace DorkyProductions
{    
    // Exactly the same model, pure data version of CatCardBlueprint Scriptable object, can be instantiated multiple times.
    // Each instance has unique Guid.
    public class CatCardData
    {
        public string title { get; private set; }
        public string description { get; private set; } // no description? maybe just flavor text for Hover-over, but not needed atm.
        public Fur type { get; private set; }  // Suit
        public int value { get; private set; } // card number btw 1-12
        public Sprite sprite { get; private set; }
        public Guid id { get; private set; }
        
        public CatCardData(string title, string description, Fur type, int value)
        {
            this.title = title;
            this.description = description;
            this.type = type;
            this.value = value;
        }

        public CatCardData(CatCardBlueprint catCardBlueprint)
        {
            title = catCardBlueprint.title;
            description = catCardBlueprint.description;
            type = catCardBlueprint.type;
            value = catCardBlueprint.value;
            sprite = catCardBlueprint.sprite;
            id = System.Guid.NewGuid();
            // Debug.Log($"Card {title},{type} id: {id}");
        }
    }
}