using UnityEngine;

// This script goes on your Tile Prefab.
public class Tile : MonoBehaviour
{
    public TileType type;
    public int x;
    public int y;

    [SerializeField] private SpriteRenderer renderer;
    // A simple way to visually distinguish tiles.
    // In a real game, you would use sprites.
    public void SetTile(TileType newType, int newX, int newY)
    {
        this.type = newType;
        this.x = newX;
        this.y = newY;

        // Update visual representation based on type
        renderer.color = GetColorForType(newType);
    }

    private Color GetColorForType(TileType tileType)
    {
        switch (tileType)
        {
            case TileType.Red: return Color.red;
            case TileType.Green: return Color.green;
            case TileType.Blue: return Color.blue;
            case TileType.Yellow: return Color.yellow;
            case TileType.Purple: return new Color(0.5f, 0, 0.5f);
            case TileType.Empty: return Color.clear;
            default: return Color.white;
        }
    }
}
