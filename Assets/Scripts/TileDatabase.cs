using UnityEngine;
using System.Collections.Generic;

namespace DorkyProductions
{
[CreateAssetMenu(fileName = "TileDatabase", menuName = "DungeonMatch/Tile Database")]
public class TileDatabase : ScriptableObject
{
    public List<TileDefinitionSO> allTileDefinitions;

    // A dictionary for fast lookups on the client
    private Dictionary<Tile, TileDefinitionSO> _lookup;

    public void Initialize()
    {
        _lookup = new Dictionary<Tile, TileDefinitionSO>();
        foreach (var tile in allTileDefinitions)
        {
            _lookup[tile.type] = tile;
        }
    }

    // Get the static tile data from its ID
    public TileDefinitionSO GetTileByType(Tile type)
    {
        _lookup.TryGetValue(type, out TileDefinitionSO def);
        return def;
    }

    // Get a random tile type ID for the server to use
    public Tile GetRandomTileTypeID()
    {
        int index = Random.Range(0, allTileDefinitions.Count);
        return allTileDefinitions[index].type;
    }
}
}