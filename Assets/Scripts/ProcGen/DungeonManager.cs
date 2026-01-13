using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonManager : MonoBehaviour
{
    [Header("Dungeon Tilemap")]
    [SerializeField] private Tilemap dungeonTilemap;
    [SerializeField] private TileBase dungeonFloorTile;
    [SerializeField] private TileBase dungeonWallTile;
    [SerializeField] private TileBase dungeonExitTile;

    [Header("Overworld Tilemaps")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap decorationTilemap;
    [SerializeField] private Tilemap canopyTilemap;

    [Header("Dungeon Settings")]
    [SerializeField] private int dungeonWidth = 50;
    [SerializeField] private int dungeonHeight = 40;

    private DungeonGrid currentDungeon;
    private Vector2Int exitGridPos;

    public GameWorldState CurrentState { get; private set; } = GameWorldState.Overworld;

    public void EnterDungeon(int seed)
    {
        groundTilemap.gameObject.SetActive(false);
        decorationTilemap.gameObject.SetActive(false);
        canopyTilemap.gameObject.SetActive(false);

        dungeonTilemap.gameObject.SetActive(true);
        dungeonTilemap.ClearAllTiles();

        currentDungeon = DungeonGenerator.Generate(
            dungeonWidth,
            dungeonHeight,
            seed
        );

        RenderDungeon();

        CurrentState = GameWorldState.Dungeon; // reuse Cave state for now
    }

    private void RenderDungeon()
    {
        for (int x = 0; x < currentDungeon.width; x++)
        {
            for (int y = 0; y < currentDungeon.height; y++)
            {
                dungeonTilemap.SetTile(
                    new Vector3Int(x, y, 0),
                    currentDungeon.walls[x, y]
                        ? dungeonWallTile
                        : dungeonFloorTile
                );
            }
        }

        PlaceExit();
    }

    private void PlaceExit()
    {
        for (int y = 1; y < currentDungeon.height - 1; y++)
        {
            for (int x = 1; x < currentDungeon.width - 1; x++)
            {
                if (!currentDungeon.walls[x, y])
                {
                    exitGridPos = new Vector2Int(x, y);
                    dungeonTilemap.SetTile(
                        new Vector3Int(x, y, 0),
                        dungeonExitTile
                    );
                    return;
                }
            }
        }
    }

    public bool IsWalkable(Vector2 worldPos)
    {
        Vector2Int gp = new Vector2Int(
            Mathf.FloorToInt(worldPos.x),
            Mathf.FloorToInt(worldPos.y)
        );

        if (gp.x < 0 || gp.y < 0 ||
            gp.x >= currentDungeon.width ||
            gp.y >= currentDungeon.height)
            return false;

        return !currentDungeon.walls[gp.x, gp.y];
    }

    public bool IsExit(Vector2Int gridPos)
    {
        return gridPos == exitGridPos;
    }

    public Vector3 GetDungeonSpawnPosition()
    {
        return new Vector3(
        exitGridPos.x + 0.5f,
        exitGridPos.y + 1.5f,
        0f);
    }

    public void ExitDungeon()
    {
        dungeonTilemap.gameObject.SetActive(false);
        dungeonTilemap.ClearAllTiles();

        groundTilemap.gameObject.SetActive(true);
        decorationTilemap.gameObject.SetActive(true);
        canopyTilemap.gameObject.SetActive(true);

        CurrentState = GameWorldState.Overworld;
    }
}
