using UnityEngine;

namespace DorkyProductions
{
    public enum SkillType
    {
        Lightning,
        PhantomMatch,
        PhaseShift,
        StoneGuard,
        SoulReaver,
        ArcaneSweep,
        ArcaneCleave,
    }
    
    [CreateAssetMenu(fileName = "Skill_", menuName = "DungeonMatch/Skills")]
    public class SkillDefinitionSO : ScriptableObject
    {
        public SkillType skillType;
        public string skillName;
        public string description;
        public string callToAction;
        public int id;
        public Sprite icon;
    }
}