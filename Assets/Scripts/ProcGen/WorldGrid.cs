using UnityEngine;

public class WorldGrid
{
    public int width;
    public int height;

    private TileType[,] tiles;

    public WorldGrid(int width, int height)
    {
        this.width = width;
        this.height = height;

        tiles = new TileType[width, height];

        InitializeGrid();
    }

    void InitializeGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tiles[x, y] = TileType.Floor;
            }
        }
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public TileType GetTile(int x, int y)
    {
        if (!IsInBounds(x, y))
            return TileType.Wall;

        return tiles[x, y];
    }

    public void SetTile(int x, int y, TileType type)
    {
        if (!IsInBounds(x, y))
            return;

        tiles[x, y] = type;
    }
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x);
        int y = Mathf.RoundToInt(worldPosition.y);
        return new Vector2Int(x, y);
    }

    public Vector3 GridToWorld(int x, int y)
    {
        return new Vector3(x, y, 0f);
    }
}
