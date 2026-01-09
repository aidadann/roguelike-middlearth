using UnityEngine;

public class CaveGrid
{
    public int width;
    public int height;
    public bool[,] walls;

    public CaveGrid(int width, int height)
    {
        this.width = width;
        this.height = height;
        walls = new bool[width, height];
    }
}
