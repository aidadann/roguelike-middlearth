public class DungeonGrid
{
    public int width;
    public int height;

    // true = wall, false = floor
    public bool[,] walls;

    public DungeonGrid(int width, int height)
    {
        this.width = width;
        this.height = height;
        walls = new bool[width, height];
    }
}
