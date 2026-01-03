using UnityEngine;

public class WorldGrid : MonoBehaviour
{
    public enum TileType
    {
        Empty,
        Floor,
        Wall,
        Decoration
    }

    [Header("Grid Settings")]
    [SerializeField] public int width = 20;
    [SerializeField] public int height = 20;
    [SerializeField] public float cellSize = 1f;

    private TileType[,] tiles;

    private void Awake()
    {
        GenerateEmptyGrid();
        GenerateForestPerlin();
        GetComponent<WorldTileRenderer>().Render(this);
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

        TileType tile = tiles[gridPos.x, gridPos.y];

        return tile == TileType.Floor || tile == TileType.Decoration;
    }

    public TileType GetTile(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return TileType.Wall; // treat out-of-bounds as blocked

        return tiles[x, y];
    }

    public bool IsRiver(int x, int y)
    {
        return GetTile(x, y) == TileType.Wall;
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
    
    [SerializeField] private float noiseScale = 0.1f;
    [SerializeField, Range(0f, 1f)] private float treeThreshold = 0.60f;
    [SerializeField, Range(0f, 1f)] private float riverThreshold = 0.30f;
    [SerializeField] private int seed = 0;

    private void GenerateForestPerlin()
    {
        float offsetX = 0f;
        float offsetY = 0f;

        if (seed != 0)
        {
            System.Random rng = new System.Random(seed);
            offsetX = rng.Next(-100000, 100000);
            offsetY = rng.Next(-100000, 100000);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float nx = (x + offsetX) * noiseScale;
                float ny = (y + offsetY) * noiseScale;

                float n = Mathf.PerlinNoise(nx, ny);

                if (n >= treeThreshold)
                {
                    tiles[x, y] = TileType.Decoration; // Trees (passable)
                }
                else if (n <= riverThreshold)
                {
                    tiles[x, y] = TileType.Wall; // River (blocked)
                }
                else
                {
                    tiles[x, y] = TileType.Floor; // Grass
                }
            }
        }
    }

}
