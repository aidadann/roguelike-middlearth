using UnityEngine;

public class WorldGrid : MonoBehaviour
{
    public enum TileType
    {
        Empty,
        Floor,
        Wall,
        Decoration,
        CaveEntrance
    }

    [Header("Grid Settings")]
    [SerializeField] public int width = 50;
    [SerializeField] public int height = 50;
    [SerializeField] public float cellSize = 1f;
    [Header("Biome Settings")]
    [SerializeField] private float biomeScale = 0.03f;
    [SerializeField] private float sparseTreeBias = 0.10f; // fewer trees
    [SerializeField] private float denseTreeBias = -0.10f; // more trees
    [Header("River Settings")]
    [SerializeField] private float riverNoiseScale = 0.08f;
    [Header("World Seed")]
    [SerializeField, Tooltip("0 = auto-generate on Play. Any other value = fixed world.")]
    public int worldSeed = 0;

    private TileType[,] tiles;

    private void Awake()
    {
        if (worldSeed == 0)
        {
            worldSeed = Random.Range(1, int.MaxValue);
            Debug.Log($"[WorldGrid] Generated world seed: {worldSeed}");
        }

        GenerateEmptyGrid();
        GenerateForestPerlin();
        PlaceCaveEntrances();
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

    public bool IsInBounds(Vector2Int gridPos)
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
    [SerializeField] private float treeThreshold;
    [SerializeField] private float riverThreshold;

    private void GenerateForestPerlin()
    {
        // --- Resolve seeds ---
        var worldRng = new System.Random(worldSeed);

        int terrainSeed = worldRng.Next();
        int riverSeed   = worldRng.Next();


        var terrainRng = new System.Random(terrainSeed);
        var riverRng = new System.Random(riverSeed);

        // --- Offsets ---
        float terrainOffsetX = terrainRng.Next(-100000, 100000);
        float terrainOffsetY = terrainRng.Next(-100000, 100000);

        float riverOffsetX = riverRng.Next(-100000, 100000);
        float riverOffsetY = riverRng.Next(-100000, 100000);

        // --- Generation ---
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Base terrain noise (forest / grass)
                float nx = (x + terrainOffsetX) * noiseScale;
                float ny = (y + terrainOffsetY) * noiseScale;
                float terrainNoise = Mathf.PerlinNoise(nx, ny);

                // Biome noise (density variation)
                float bx = (x + terrainOffsetX) * biomeScale;
                float by = (y + terrainOffsetY) * biomeScale;
                float biomeNoise = Mathf.PerlinNoise(bx, by);

                float biomeBias = 0f;
                if (biomeNoise < 0.4f)
                    biomeBias = sparseTreeBias;
                else if (biomeNoise > 0.6f)
                    biomeBias = denseTreeBias;

                float adjustedTreeThreshold = treeThreshold + biomeBias;

                // River noise (separate field)
                float rx = (x + riverOffsetX) * riverNoiseScale;
                float ry = (y + riverOffsetY) * riverNoiseScale;
                float riverNoise = Mathf.PerlinNoise(rx, ry);

                // --- Final decision ---
                if (riverNoise <= riverThreshold)
                {
                    tiles[x, y] = TileType.Wall; // River
                }
                else if (terrainNoise >= adjustedTreeThreshold)
                {
                    tiles[x, y] = TileType.Decoration; // Forest area
                }
                else
                {
                    tiles[x, y] = TileType.Floor; // Grass
                }
            }
        }
    }

    [SerializeField] private int caveCount = 3;

    private void PlaceCaveEntrances()
    {
        var rng = new System.Random(worldSeed + 999); // offset to avoid collision
        int placed = 0;
        int attempts = 0;

        while (placed < caveCount && attempts < 5000)
        {
            attempts++;

            int x = rng.Next(1, width - 1);
            int y = rng.Next(1, height - 1);

            if (tiles[x, y] != TileType.Floor)
                continue;

            tiles[x, y] = TileType.CaveEntrance;
            placed++;
        }
    }

    
    [ContextMenu("Generate New World Seed")]
    private void GenerateNewSeed()
    {
        worldSeed = Random.Range(1, int.MaxValue);
        Debug.Log($"[WorldGrid] New world seed generated: {worldSeed}");
    }

}