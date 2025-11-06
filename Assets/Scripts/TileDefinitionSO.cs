using UnityEngine;

namespace DorkyProductions
{

public enum Tile
{
    Unknown = 0,
    Attack,
    Shield,
    Cross,
    Heal,
    Chest,
}
[CreateAssetMenu(fileName = "Tile_", menuName = "DungeonMatch/Tile")]
public class TileDefinitionSO : ScriptableObject
{
    // We'll use an int as a simple ID.
    // You could make this an enum for clarity.
    public Tile type;
    
    public Sprite tileSprite;
    
    // You can add more static data here later, like:
    // public GameObject popEffectPrefab;
    // public AudioClip popSound;
}

}