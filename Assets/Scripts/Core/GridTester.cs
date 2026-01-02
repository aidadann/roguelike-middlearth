using UnityEngine;

public class GridTester : MonoBehaviour
{
    public int width = 20;
    public int height = 15;
    public float tileSize = 1f;

    private WorldGrid worldGrid;

    void Start()
    {
        worldGrid = new WorldGrid(width, height);
    }

    void OnDrawGizmos()
    {
        if (worldGrid == null)
            return;

        for (int x = 0; x < worldGrid.width; x++)
        {
            for (int y = 0; y < worldGrid.height; y++)
            {
                TileType tile = worldGrid.GetTile(x, y);

                Gizmos.color = tile switch
                {
                    TileType.Floor => Color.gray,
                    TileType.Wall => Color.black,
                    _ => Color.clear
                };

                Vector3 pos = new Vector3(x * tileSize, y * tileSize, 0);
                Gizmos.DrawCube(pos, Vector3.one * 0.9f);
            }
        }
    }
        void OnDrawGizmosSelected()
    {
        if (worldGrid == null)
            return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 10;
        style.normal.textColor = Color.white;

        for (int x = 0; x < worldGrid.width; x++)
        {
            for (int y = 0; y < worldGrid.height; y++)
            {
                Vector3 pos = new Vector3(x, y, 0);
                UnityEditor.Handles.Label(pos, $"({x},{y})", style);
            }
        }
    }

}
