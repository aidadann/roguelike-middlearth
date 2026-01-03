using UnityEngine;

public class WorldGrid : MonoBehaviour
{
    public enum TileType
    {
        Empty,
        Floor,
        Wall
    }

    [Header("Grid Settings")]
    [SerializeField] private int width = 20;
    [SerializeField] private int height = 20;
    [SerializeField] private float cellSize = 1f;

    private TileType[,] tiles;

    private void Awake()
    {
        GenerateEmptyGrid();
        AddTestWalls();
    }

    private void GenerateEmptyGrid()
    {
        tiles = new TileType[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tiles[x, y] = TileType.Floor;
            }
        }
    }

    private void AddTestWalls()
    {
        // Vertical wall in the middle
        for (int y = 5; y < height - 5; y++)
        {
            tiles[width / 2, y] = TileType.Wall;
        }

        // Horizontal wall near bottom
        for (int x = 3; x < width - 3; x++)
        {
            tiles[x, 4] = TileType.Wall;
        }
    }

    public Vector2Int WorldToGrid(Vector2 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / cellSize);
        int y = Mathf.FloorToInt(worldPosition.y / cellSize);
        return new Vector2Int(x, y);
    }

    public Vector2 GridToWorld(Vector2Int gridPosition)
    {
        return new Vector2(
            gridPosition.x * cellSize + cellSize * 0.5f,
            gridPosition.y * cellSize + cellSize * 0.5f
        );
    }

    private bool IsInBounds(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < width &&
               gridPos.y >= 0 && gridPos.y < height;
    }

    public bool IsWalkable(Vector2 worldPosition)
    {
        Vector2Int gridPos = WorldToGrid(worldPosition);

        if (!IsInBounds(gridPos))
            return false;

        return tiles[gridPos.x, gridPos.y] == TileType.Floor;
    }

    public Vector2 GetSpawnWorldPosition()
    {
        // Simple deterministic spawn: bottom-left safe area
        Vector2Int spawnGridPos = new Vector2Int(1, 1);

        // Safety check
        if (!IsInBounds(spawnGridPos))
        {
            Debug.LogError("Spawn position out of bounds!");
            return Vector2.zero;
        }

        if (tiles[spawnGridPos.x, spawnGridPos.y] == TileType.Wall)
        {
            Debug.LogError("Spawn position is blocked!");
            return Vector2.zero;
        }

        return GridToWorld(spawnGridPos);
    }

    private void OnDrawGizmos()
    {
        if (tiles == null)
            return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 worldPos = new Vector3(
                    x * cellSize + cellSize * 0.5f,
                    y * cellSize + cellSize * 0.5f,
                    0f
                );

                Gizmos.color = tiles[x, y] == TileType.Wall
                    ? Color.red
                    : Color.green;

                Gizmos.DrawWireCube(worldPos, Vector3.one * cellSize);
            }
        }
    }
}
