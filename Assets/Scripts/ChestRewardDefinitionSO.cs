using UnityEngine;

namespace DorkyProductions
{
    [CreateAssetMenu(fileName = "ChestReward_", menuName = "DungeonMatch/ChestReward")]
    public class ChestRewardDefinitionSO : ScriptableObject
    {
        public string skillName;
        public string description;
        public int id;
        public Sprite icon;
    }
}