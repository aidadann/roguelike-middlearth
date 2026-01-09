using UnityEngine;

public static class CaveGenerator
{
    public static CaveGrid Generate(
        int width,
        int height,
        int seed,
        float initialWallChance = 0.45f,
        int iterations = 5)
    {
        var grid = new CaveGrid(width, height);
        var rng = new System.Random(seed);

        // Initial fill
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool border = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                grid.walls[x, y] = border || rng.NextDouble() < initialWallChance;
            }
        }

        // Cellular automata steps
        for (int i = 0; i < iterations; i++)
        {
            Step(grid);
        }

        return grid;
    }

    private static void Step(CaveGrid grid)
    {
        bool[,] newMap = new bool[grid.width, grid.height];

        for (int x = 0; x < grid.width; x++)
        {
            for (int y = 0; y < grid.height; y++)
            {
                int wallCount = CountWalls(grid, x, y);

                if (wallCount > 4)
                    newMap[x, y] = true;
                else if (wallCount < 4)
                    newMap[x, y] = false;
                else
                    newMap[x, y] = grid.walls[x, y];
            }
        }

        grid.walls = newMap;
    }

    private static int CountWalls(CaveGrid grid, int x, int y)
    {
        int count = 0;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (nx < 0 || ny < 0 || nx >= grid.width || ny >= grid.height)
                    count++;
                else if (grid.walls[nx, ny])
                    count++;
            }
        }

        return count;
    }
}
