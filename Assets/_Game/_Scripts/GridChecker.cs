using System.Collections.Generic;
using UnityEngine;

public static class GridChecker
{
    // Returns list of (x,y) coordinates with matched balls (horizontal, vertical, diagonal)
    public static List<(int,int)> GetMatchedBalls(Ball[,] grid, int col, int row)
    {
        List<(int,int)> matched = new List<(int,int)>();
        BallColor color = grid[col, row].color;

        // Horizontal
        List<(int,int)> horiz = new List<(int,int)>();
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            if (grid[x, row] != null && grid[x, row].color == color)
                horiz.Add((x, row));
        }
        if (horiz.Count >= 3) matched.AddRange(horiz);

        // Vertical
        List<(int,int)> vert = new List<(int,int)>();
        for (int y = 0; y < grid.GetLength(1); y++)
        {
            if (grid[col, y] != null && grid[col, y].color == color)
                vert.Add((col, y));
        }
        if (vert.Count >= 3) matched.AddRange(vert);

        // Diagonal /
        List<(int,int)> diag1 = new List<(int,int)>();
        for (int d = -2; d <= 2; d++)
        {
            int x = col + d;
            int y = row + d;
            if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
                if (grid[x, y] != null && grid[x, y].color == color)
                    diag1.Add((x, y));
        }
        if (diag1.Count >= 3) matched.AddRange(diag1);

        // Diagonal \
        List<(int,int)> diag2 = new List<(int,int)>();
        for (int d = -2; d <= 2; d++)
        {
            int x = col + d;
            int y = row - d;
            if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
                if (grid[x, y] != null && grid[x, y].color == color)
                    diag2.Add((x, y));
        }
        if (diag2.Count >= 3) matched.AddRange(diag2);

        // Remove duplicates
        HashSet<(int,int)> set = new HashSet<(int,int)>(matched);
        return new List<(int,int)>(set);
    }
}