using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

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
        canopyTilemap.ClearAllTiles();

        for (int x = 0; x < grid.width; x++)
        {
            for (int y = 0; y < grid.height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                var tileType = grid.GetTile(x, y);

                if (tileType == WorldGrid.TileType.Wall)
                    groundTilemap.SetTile(pos, GetRiverTile(grid, x, y));
                else
                    groundTilemap.SetTile(pos, floorTile);
            }
        }
        for (int x = 0; x < grid.width; x++)
        {
            for (int y = 0; y < grid.height; y++)
            {
                if (grid.GetTile(x, y) != WorldGrid.TileType.CaveEntrance || grid.GetTile(x, y - 1) == WorldGrid.TileType.CaveEntrance)
                    continue;

                Vector3Int anchorPos = new Vector3Int(x, y, 0);


                // Render stamp in ROW ORDER (bottom → top)
                foreach (var part in caveEntranceStamp)
                {
                    Vector3Int stampPos = anchorPos + new Vector3Int(
                        part.offset.x,
                        part.offset.y,
                        0
                    );

                    groundTilemap.SetTile(stampPos, part.tile);
                }
            }
        }


        // ---- FOREST PATCH TREES ----
        var patches = FindForestPatches(grid);

        foreach (var patch in patches)
        {
            // Find center of patch
            Vector3 avg = Vector3.zero;
            foreach (var p in patch)
                avg += (Vector3)p;

            avg /= patch.Count;
            Vector3Int treePos = new Vector3Int(
                Mathf.RoundToInt(avg.x),
                Mathf.FloorToInt(avg.y),
                0
            );

            // Place ONE tree
            // Place ONE trunk
            decorationTilemap.SetTile(treePos, treeTrunkTile);

            // Place a wider canopy ABOVE the trunk
            Vector3Int basePos = treePos + Vector3Int.up;

            // Center canopy
            canopyTilemap.SetTile(basePos, treeCanopyTile);

            // Optional width (recommended)
            canopyTilemap.SetTile(basePos + Vector3Int.left, treeCanopyTile);
            canopyTilemap.SetTile(basePos + Vector3Int.right, treeCanopyTile);

            // Optional extra depth (only if you want it bigger)
            canopyTilemap.SetTile(basePos + Vector3Int.up, treeCanopyTile);
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

    public List<List<Vector3Int>> FindForestPatches(WorldGrid grid)
    {
        bool[,] visited = new bool[grid.width, grid.height];
        var patches = new List<List<Vector3Int>>();

        for (int x = 0; x < grid.width; x++)
        {
            for (int y = 0; y < grid.height; y++)
            {
                if (visited[x, y])
                    continue;

                if (grid.GetTile(x, y) != WorldGrid.TileType.Decoration)
                    continue;

                var patch = new List<Vector3Int>();
                FloodFill(grid, x, y, visited, patch);

                if (patch.Count > 0)
                    patches.Add(patch);
            }
        }

        return patches;
    }

    public void FloodFill(
        WorldGrid grid,
        int startX,
        int startY,
        bool[,] visited,
        List<Vector3Int> patch)
    {
        var stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(startX, startY));

        while (stack.Count > 0)
        {
            var p = stack.Pop();
            if (!grid.IsInBounds(p) || visited[p.x, p.y])
                continue;

            if (grid.GetTile(p.x, p.y) != WorldGrid.TileType.Decoration)
                continue;

            visited[p.x, p.y] = true;
            patch.Add(new Vector3Int(p.x, p.y, 0));

            stack.Push(new Vector2Int(p.x + 1, p.y));
            stack.Push(new Vector2Int(p.x - 1, p.y));
            stack.Push(new Vector2Int(p.x, p.y + 1));
            stack.Push(new Vector2Int(p.x, p.y - 1));
        }
    }
    [System.Serializable]
    public struct CaveEntranceStamp
    {
        public Vector2Int offset;
        public TileBase tile;
    }

    [Header("Cave Entrance Stamp (Multi-tile)")]
    [SerializeField] private CaveEntranceStamp[] caveEntranceStamp;


}