using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace DorkyProductions
{
[CreateAssetMenu(fileName = "TileDatabase", menuName = "DungeonMatch/Tile Database")]
public class TileDatabase : ScriptableObject
{
    public List<TileDefinitionSO> allTileDefinitions;
    public List<SkillDefinitionSO> allSkills;

    // A dictionary for fast lookups on the client
    private Dictionary<Tile, TileDefinitionSO> _tileLookup;

    public void Initialize()
    {
        _tileLookup = new Dictionary<Tile, TileDefinitionSO>();
        foreach (var tile in allTileDefinitions)
        {
            _tileLookup[tile.type] = tile;
        }
    }

    // Get the static tile data from its ID
    public TileDefinitionSO GetTileByType(Tile type)
    {
        _tileLookup.TryGetValue(type, out TileDefinitionSO def);
        return def;
    }

    public SkillDefinitionSO GetSkill(int id)
    {
        return allSkills.Find(t => t.id == id);
    }
    // Get a random tile type ID for the server to use
    public Tile GetRandomTileTypeID()
    {
        int index = Random.Range(0, allTileDefinitions.Count);
        return allTileDefinitions[index].type;
    }
}
}