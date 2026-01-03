using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldTileRenderer : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase wallTile;

    public void Render(WorldGrid grid)
    {
        tilemap.ClearAllTiles();

        for (int x = 0; x < grid.width; x++)
        {
            for (int y = 0; y < grid.height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                tilemap.SetTile(
                    pos,
                    grid.GetTile(x, y) == WorldGrid.TileType.Wall
                        ? wallTile
                        : floorTile
                );
            }
        }
    }
}
