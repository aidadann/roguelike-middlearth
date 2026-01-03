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
    [SerializeField] public int width = 20;
    [SerializeField] public int height = 20;
    [SerializeField] public float cellSize = 1f;

    private TileType[,] tiles;

    private void Awake()
    {
        GenerateEmptyGrid();
        GenerateTestForest();

        GetComponent<WorldTileRenderer>().Render(this);
        //AddTestWalls();
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

    public TileType GetTile(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return TileType.Wall; // treat out-of-bounds as blocked

        return tiles[x, y];
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

    public Vector2 GetSafeSpawnWorldPosition()
    {
        // Start near bottom-left and search outward
        for (int radius = 0; radius < Mathf.Max(width, height); radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2Int gp = new Vector2Int(1 + dx, 1 + dy);

                    if (!IsInBounds(gp))
                        continue;

                    if (tiles[gp.x, gp.y] == TileType.Floor)
                    {
                        return GridToWorld(gp);
                    }
                }
            }
        }

        Debug.LogError("No safe spawn found!");
        return GridToWorld(new Vector2Int(1, 1));
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
    private void GenerateTestForest()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tiles[x, y] = (x == width / 2 || y == height / 2)
                    ? TileType.Wall
                    : TileType.Floor;
            }
        }
    }

}
