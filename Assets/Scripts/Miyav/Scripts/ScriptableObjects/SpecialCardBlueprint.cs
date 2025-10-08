using UnityEngine;

namespace DorkyProductions
{
    [CreateAssetMenu(fileName = "NewSpecialCard", menuName = "Cards/Special")]
    public class SpecialCardBlueprint : ScriptableObject
    {
        public string title;
        public string description;
        public SpecialCardType type;
        public Sprite sprite;
    }
}