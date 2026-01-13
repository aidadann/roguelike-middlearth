using UnityEngine;
using UnityEngine.Tilemaps;

public class CaveManager : MonoBehaviour
{
    [Header("World Roots")]
    [SerializeField] private GameObject overworldRoot;

    [Header("Cave Tilemap")]
    [SerializeField] private Tilemap caveTilemap;
    [SerializeField] private TileBase caveFloorTile;
    [SerializeField] private TileBase caveWallTile;
    [SerializeField] private TileBase caveExitTile;

    [Header("Overworld Tilemaps")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap decorationTilemap;
    [SerializeField] private Tilemap canopyTilemap;


    [Header("Cave Settings")]
    [SerializeField] private int caveWidth = 40;
    [SerializeField] private int caveHeight = 30;
    private Vector2Int caveExitGridPos;

    public GameWorldState CurrentState { get; private set; } = GameWorldState.Overworld;

    private CaveGrid currentCave;

    public void EnterCave(int caveSeed)
    {
        // Hide overworld
        groundTilemap.gameObject.SetActive(false);
        decorationTilemap.gameObject.SetActive(false);
        canopyTilemap.gameObject.SetActive(false);

        // Show cave
        caveTilemap.gameObject.SetActive(true);
        caveTilemap.ClearAllTiles();

        currentCave = CaveGenerator.Generate(
            caveWidth,
            caveHeight,
            caveSeed
        );

        RenderCave();

        CurrentState = GameWorldState.Cave;
    }


    private void RenderCave()
    {
        for (int x = 0; x < currentCave.width; x++)
        {
            for (int y = 0; y < currentCave.height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                caveTilemap.SetTile(
                    pos,
                    currentCave.walls[x, y]
                        ? caveWallTile
                        : caveFloorTile
                );
            }
        }

        PlaceCaveExitTile();
    }

    private void PlaceCaveExitTile()
    {
        Vector3 spawn = GetCaveSpawnPosition();

        caveExitGridPos = new Vector2Int(
            Mathf.FloorToInt(spawn.x),
            Mathf.FloorToInt(spawn.y) - 1
        );

        caveExitGridPos.x = Mathf.Clamp(caveExitGridPos.x, 1, currentCave.width - 2);
        caveExitGridPos.y = Mathf.Clamp(caveExitGridPos.y, 1, currentCave.height - 2);

        caveTilemap.SetTile(
            new Vector3Int(caveExitGridPos.x, caveExitGridPos.y, 0),
            caveExitTile
        );
    }



        // Place cave exit near spawn
        public bool IsExitTile(Vector2Int gridPos)
        {
            return CurrentState == GameWorldState.Cave &&
                gridPos == caveExitGridPos;
        }

    public Vector3 GetCaveSpawnPosition()
    {
        // Simple spawn: first open tile from bottom
        for (int y = 1; y < currentCave.height - 1; y++)
        {
            for (int x = 1; x < currentCave.width - 1; x++)
            {
                if (!currentCave.walls[x, y])
                    return new Vector3(x + 0.5f, y + 0.5f, 0f);
            }
        }

        return Vector3.zero;
    }
    public bool IsWalkable(Vector2 worldPosition)
    {
        if (CurrentState != GameWorldState.Cave)
            return false;

        Vector2Int gridPos = new Vector2Int(
            Mathf.FloorToInt(worldPosition.x),
            Mathf.FloorToInt(worldPosition.y)
        );

        if (gridPos.x < 0 || gridPos.y < 0 ||
            gridPos.x >= currentCave.width ||
            gridPos.y >= currentCave.height)
            return false;

        return !currentCave.walls[gridPos.x, gridPos.y] || gridPos == caveExitGridPos;
    }

    public void ExitCave()
    {
        // Hide cave
        caveTilemap.gameObject.SetActive(false);
        caveTilemap.ClearAllTiles();

        // Show overworld
        groundTilemap.gameObject.SetActive(true);
        decorationTilemap.gameObject.SetActive(true);
        canopyTilemap.gameObject.SetActive(true);

        CurrentState = GameWorldState.Overworld;
    }


}
