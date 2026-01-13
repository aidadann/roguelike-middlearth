using UnityEngine;
using System.Collections.Generic;

public static class DungeonGenerator
{
    public static DungeonGrid Generate(
        int width,
        int height,
        int seed,
        int minRoomSize = 6,
        int maxDepth = 4
    )
    {
        DungeonGrid grid = new DungeonGrid(width, height);

        // Start with solid walls
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid.walls[x, y] = true;

        var rng = new System.Random(seed);

        // BSP root
        RectInt root = new RectInt(1, 1, width - 2, height - 2);
        List<RectInt> rooms = new List<RectInt>();

        SplitNode(root, 0, maxDepth, minRoomSize, rng, rooms);

        // Carve rooms
        foreach (var room in rooms)
        {
            for (int x = room.x; x < room.xMax; x++)
                for (int y = room.y; y < room.yMax; y++)
                    grid.walls[x, y] = false;
        }

        // Connect rooms with random walk corridors
        for (int i = 1; i < rooms.Count; i++)
        {
            Vector2Int a = RoomCenter(rooms[i - 1]);
            Vector2Int b = RoomCenter(rooms[i]);

            CarveRandomWalk(grid, a, b, rng);
        }

        return grid;
    }

    // ---------------- BSP ----------------

    private static void SplitNode(
        RectInt node,
        int depth,
        int maxDepth,
        int minSize,
        System.Random rng,
        List<RectInt> rooms
    )
    {
        if (depth >= maxDepth ||
            node.width < minSize * 2 ||
            node.height < minSize * 2)
        {
            rooms.Add(Shrink(node, rng));
            return;
        }

        bool splitVertical = rng.NextDouble() > 0.5;

        if (node.width > node.height)
            splitVertical = true;
        else if (node.height > node.width)
            splitVertical = false;

        if (splitVertical)
        {
            int splitX = rng.Next(minSize, node.width - minSize);
            SplitNode(new RectInt(node.x, node.y, splitX, node.height),
                depth + 1, maxDepth, minSize, rng, rooms);
            SplitNode(new RectInt(node.x + splitX, node.y, node.width - splitX, node.height),
                depth + 1, maxDepth, minSize, rng, rooms);
        }
        else
        {
            int splitY = rng.Next(minSize, node.height - minSize);
            SplitNode(new RectInt(node.x, node.y, node.width, splitY),
                depth + 1, maxDepth, minSize, rng, rooms);
            SplitNode(new RectInt(node.x, node.y + splitY, node.width, node.height - splitY),
                depth + 1, maxDepth, minSize, rng, rooms);
        }
    }

    private static RectInt Shrink(RectInt rect, System.Random rng)
    {
        int margin = 1;
        int w = rng.Next(rect.width / 2, rect.width - margin);
        int h = rng.Next(rect.height / 2, rect.height - margin);

        int x = rect.x + rng.Next(1, rect.width - w);
        int y = rect.y + rng.Next(1, rect.height - h);

        return new RectInt(x, y, w, h);
    }

    // ---------------- Corridors ----------------

    private static void CarveRandomWalk(
        DungeonGrid grid,
        Vector2Int from,
        Vector2Int to,
        System.Random rng
    )
    {
        Vector2Int pos = from;

        while (pos != to)
        {
            grid.walls[pos.x, pos.y] = false;

            if (rng.NextDouble() < 0.5)
                pos.x += pos.x < to.x ? 1 : -1;
            else
                pos.y += pos.y < to.y ? 1 : -1;
        }
    }

    private static Vector2Int RoomCenter(RectInt r)
    {
        return new Vector2Int(
            r.x + r.width / 2,
            r.y + r.height / 2
        );
    }
}
