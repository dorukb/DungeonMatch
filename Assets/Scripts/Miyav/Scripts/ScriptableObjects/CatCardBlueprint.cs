using UnityEngine;

namespace DorkyProductions
{
    [CreateAssetMenu(fileName = "NewCatCard", menuName = "Cards/Cat")]
    public class CatCardBlueprint : ScriptableObject
    {
        public string title;
        public string description;
        public Fur type;
        public Sprite sprite;
        
        [Range(1,12)]
        public int value;
        // TODO: Add effects for special cards.
    }
}