using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TileDatabase", menuName = "DungeonMatch/Tile Database")]
public class TileDatabase : ScriptableObject
{
    public List<TileDefinitionSO> allTileDefinitions;

    // A dictionary for fast lookups on the client
    private Dictionary<int, TileDefinitionSO> _lookup;

    public void Initialize()
    {
        _lookup = new Dictionary<int, TileDefinitionSO>();
        foreach (var tile in allTileDefinitions)
        {
            _lookup[tile.tileTypeID] = tile;
        }
    }

    // Get the static tile data from its ID
    public TileDefinitionSO GetTileByType(int typeID)
    {
        _lookup.TryGetValue(typeID, out TileDefinitionSO def);
        return def;
    }

    // Get a random tile type ID for the server to use
    public int GetRandomTileTypeID()
    {
        int index = Random.Range(0, allTileDefinitions.Count);
        return allTileDefinitions[index].tileTypeID;
    }
}