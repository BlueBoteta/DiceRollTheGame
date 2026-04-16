using UnityEngine;

public enum TileType { Normal, Combat, Loot, Story, Boss }

public class BoardTile : MonoBehaviour
{
    public int pathIndex;
    public TileType tileType = TileType.Normal;
    public Vector2Int gridPos;
}
