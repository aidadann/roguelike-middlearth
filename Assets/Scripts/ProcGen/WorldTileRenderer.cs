using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldTileRenderer : MonoBehaviour
{
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap decorationTilemap;
    [SerializeField] private Tilemap canopyTilemap;
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase wallTile;
    [SerializeField] private TileBase decorationTile;
    [SerializeField] private TileBase treeTrunkTile;
    [SerializeField] private TileBase treeCanopyTile;

    [Header("River Tiles")]
    [SerializeField] private TileBase riverCenter;
    [SerializeField] private TileBase riverHorizontal;
    [SerializeField] private TileBase riverVertical;

    [SerializeField] private TileBase riverEdgeTop;
    [SerializeField] private TileBase riverEdgeBottom;
    [SerializeField] private TileBase riverEdgeLeft;
    [SerializeField] private TileBase riverEdgeRight;

    [SerializeField] private TileBase riverCornerTL;
    [SerializeField] private TileBase riverCornerTR;
    [SerializeField] private TileBase riverCornerBL;
    [SerializeField] private TileBase riverCornerBR;


    public void Render(WorldGrid grid)
    {
        groundTilemap.ClearAllTiles();
        decorationTilemap.ClearAllTiles();

        for (int x = 0; x < grid.width; x++)
        {
            for (int y = 0; y < grid.height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                var tileType = grid.GetTile(x, y);

                // Always place ground
                groundTilemap.SetTile(pos, floorTile);

                // Overlay decoration or river
                switch (tileType)
                {
                    case WorldGrid.TileType.Wall:
                        groundTilemap.SetTile(pos, GetRiverTile(grid, x, y));
                        break;

                    case WorldGrid.TileType.Decoration:
                    {
                        // 1. ALWAYS draw ground underneath
                        groundTilemap.SetTile(pos, floorTile);

                        // 2. Decide if THIS tile gets a trunk
                        //    (sparse trunks, clustered naturally)
                        bool placeTrunk = ((x + y) % 3 == 0);

                        if (placeTrunk)
                        {
                            // Place trunk at this tile
                            decorationTilemap.SetTile(pos, treeTrunkTile);

                            // Place canopy ABOVE the trunk
                            Vector3Int canopyPos = new Vector3Int(x, y + 1, 0);
                            canopyTilemap.SetTile(canopyPos, treeCanopyTile);
                        }
                        else
                        {
                            // No trunk, but still part of forest → canopy only
                            Vector3Int canopyPos = new Vector3Int(x, y, 0);
                            canopyTilemap.SetTile(canopyPos, treeCanopyTile);
                        }

                        break;
                    }

                }
            }
        }
    }

    private TileBase GetRiverTile(WorldGrid grid, int x, int y)
    {
        bool up    = grid.IsRiver(x, y + 1);
        bool down  = grid.IsRiver(x, y - 1);
        bool left  = grid.IsRiver(x - 1, y);
        bool right = grid.IsRiver(x + 1, y);

        int count =
            (up ? 1 : 0) +
            (down ? 1 : 0) +
            (left ? 1 : 0) +
            (right ? 1 : 0);

        // 4 neighbors → center
        if (count == 4)
            return riverCenter;

        if (count == 0)
            return riverCenter;

        // Straights
        if (left && right && !up && !down)
            return riverHorizontal;

        if (up && down && !left && !right)
            return riverVertical;

        // Corners
        if (up && right && !down && !left)
            return riverCornerBL;

        if (up && left && !down && !right)
            return riverCornerBR;

        if (down && right && !up && !left)
            return riverCornerTL;

        if (down && left && !up && !right)
            return riverCornerTR;


        // End caps
        if (up && !down && left && right)
            return riverEdgeBottom;

        if (down && !up && left && right)
            return riverEdgeTop;

        if (left && !right && up && down)
            return riverEdgeRight;

        if (right && !left && up && down)
            return riverEdgeLeft;

        // Fallbacks
        if (count >= 3)
            return riverCenter;

        return riverCenter;
    }
}