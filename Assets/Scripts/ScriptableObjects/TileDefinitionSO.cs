using Unity.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; // Required for the "OnValidate" fix
#endif
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

[CreateAssetMenu(menuName = "Match3/TileDefinition")]
public class TileDefinitionSO : ScriptableObject
{
    public Tile type;

    [Header("Visuals")]
    public Sprite tileSprite;
    public Sprite tileSprite2x;

    [Header("Special Rules")]
    public bool supportsDoubleEffect;
    
    [Header("Spawn Settings")]
    [Range(0f, 1f)]
    [Tooltip("0.2 = 20% chance")]
    public float doubleEffectChance = 0.2f;
    [Tooltip("Higher number = higher chance to spawn.")]
    [Min(0)] 
    public int spawnWeight = 10;

    // This is read-only information calculated by the Database
    [Tooltip("Calculated by the TileDatabase based on total weights.")]
    [ReadOnly] // Custom attribute recommended, or just don't edit manually
    public float displayProbability; 
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // When we change a value here, find the Database and tell it to recalculate
        // This ensures the displayProbability updates instantly.
        string[] guids = AssetDatabase.FindAssets("t:TileDatabase");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TileDatabase db = AssetDatabase.LoadAssetAtPath<TileDatabase>(path);
            if (db != null)
            {
                db.CalculateProbabilities();
            }
        }
    }
#endif
}
}