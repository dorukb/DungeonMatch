using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace DorkyProductions
{
[CreateAssetMenu(menuName = "Match3/TileDatabase")]
public class TileDatabase : ScriptableObject
{
    public List<TileDefinitionSO> allTileDefinitions;
    public List<SkillDefinitionSO> allSkills;

    // A dictionary for fast lookups (Client/Server runtime)
    private Dictionary<Tile, TileDefinitionSO> _tileLookup;

    public void Initialize()
    {
        _tileLookup = new Dictionary<Tile, TileDefinitionSO>();
        foreach (var def in allTileDefinitions)
        {
            if (def != null)
                _tileLookup[def.type] = def;
        }
    }
    public TileDefinitionSO GetWeightedRandomDefinition(List<TileDefinitionSO> candidates)
    {
        if (candidates.Count == 0) return null;

        int totalWeight = 0;

        // Pass 1: Sum weights directly from the objects
        for (int i = 0; i < candidates.Count; i++)
        {
            totalWeight += candidates[i].spawnWeight;
        }

        if (totalWeight <= 0) return candidates[0];

        // Pass 2: Pick winner
        int randomValue = Random.Range(0, totalWeight);

        for (int i = 0; i < candidates.Count; i++)
        {
            var def = candidates[i];
            randomValue -= def.spawnWeight;
            if (randomValue < 0)
            {
                return def;
            }
        }

        return candidates[candidates.Count - 1];
    }

    // Helper to get a random one from the master list
    public TileDefinitionSO GetRandomDefinition()
    {
        return GetWeightedRandomDefinition(allTileDefinitions);
    }
    // ------------------------------------------------------------------------
    // LOOKUPS & EDITOR HELPERS
    // ------------------------------------------------------------------------

    public TileDefinitionSO GetTileByType(Tile type)
    {
        // Safety check if Initialize wasn't called or we are in Editor mode
        if (_tileLookup == null) Initialize();
        
        _tileLookup.TryGetValue(type, out TileDefinitionSO def);
        return def;
    }

    public SkillDefinitionSO GetSkill(int id)
    {
        return allSkills.Find(t => t.id == id);
    }

    // ------------------------------------------------------------------------
    // EDITOR ONLY: Auto-calculate probabilities
    // ------------------------------------------------------------------------
#if UNITY_EDITOR
    private void OnValidate()
    {
        CalculateProbabilities();
    }

    public void CalculateProbabilities()
    {
        if (allTileDefinitions == null) return;

        float totalWeight = 0;
        foreach (var def in allTileDefinitions)
        {
            if (def != null) totalWeight += def.spawnWeight;
        }

        foreach (var def in allTileDefinitions)
        {
            if (def != null)
            {
                // Calculate %
                float prob = totalWeight > 0 ? (float)def.spawnWeight / totalWeight : 0;
                
                // Only update and set dirty if the value actually changed 
                // (prevents infinite loop with OnValidate)
                if (Mathf.Abs(def.displayProbability - prob) > 0.0001f)
                {
                    def.displayProbability = prob;
                    EditorUtility.SetDirty(def); // Tells Unity this asset changed
                }
            }
        }
    }
#endif
}
}