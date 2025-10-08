using UnityEngine;

namespace DorkyProductions
{
    // Exactly the same model, pure data version of SpecialCardBlueprint Scriptable object.
    public class SpecialCardData
    {
        public string title { get; private set; }
        public string description { get; private set; }
        public SpecialCardType type { get; private set; }
        public Sprite sprite { get; private set; }

        public SpecialCardData(string title, string description, SpecialCardType type)
        {
            this.title = title;
            this.description = description;
            this.type = type;
        }

        public SpecialCardData(SpecialCardBlueprint specialCardBlueprint)
        {
            title = specialCardBlueprint.title;
            description = specialCardBlueprint.description;
            type = specialCardBlueprint.type;
            sprite = specialCardBlueprint.sprite;
        }
        // TODO: Implement effects for special cards.

    }
}