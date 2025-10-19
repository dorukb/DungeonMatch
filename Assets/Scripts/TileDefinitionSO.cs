using UnityEngine;

[CreateAssetMenu(fileName = "Tile_", menuName = "DungeonMatch/Tile")]
public class TileDefinitionSO : ScriptableObject
{
    // We'll use an int as a simple ID.
    // You could make this an enum for clarity.
    public int tileTypeID;
    
    public Sprite tileSprite;
    
    // You can add more static data here later, like:
    // public GameObject popEffectPrefab;
    // public AudioClip popSound;
    public static string ToString(int tileTypeID)
    {
        switch (tileTypeID)
        {
            case 0:
                return "Attack";
            case 1:
                return "Shield";
            case 2:
                return "Cross";
            case 3:
                return "Potion";
            case 4:
                return "Chest";
            default:
                return "Unknown";
        }
    }
}
