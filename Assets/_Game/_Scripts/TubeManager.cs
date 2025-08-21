using UnityEngine;
using System.Collections.Generic;

public class TubeManager : MonoBehaviour
{
    public int columns = 3;
    public int rows = 3;
    public Transform[] tubePositions;
    private Ball[,] grid;

    private void Awake()
    {
        grid = new Ball[columns, rows];
    }

    public void ResetTubes()
    {
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y] != null)
                {
                    Destroy(grid[x, y].gameObject);
                    grid[x, y] = null;
                }
            }
        }
    }

    public void StackBallInTube(Ball ball)
    {
        int col = GetClosestTube(ball.transform.position.x);
        for (int row = 0; row < rows; row++)
        {
            if (grid[col, row] == null)
            {
                grid[col, row] = ball;
                ball.transform.position = tubePositions[col].position + Vector3.up * (row * 0.8f);
                CheckAndClearMatches(col, row);
                CheckGameOver();
                GameManager.Instance.ballSpawner.SpawnNextBall();
                return;
            }
        }
        // If tube full, let ball bounce or just destroy (should not happen)
    }

    private int GetClosestTube(float x)
    {
        int closest = 0;
        float minDist = Mathf.Abs(x - tubePositions[0].position.x);
        for (int i = 1; i < tubePositions.Length; i++)
        {
            float dist = Mathf.Abs(x - tubePositions[i].position.x);
            if (dist < minDist)
            {
                closest = i;
                minDist = dist;
            }
        }
        return closest;
    }

    private void CheckAndClearMatches(int col, int row)
    {
        List<(int,int)> matched = GridChecker.GetMatchedBalls(grid, col, row);
        if (matched.Count >= 3)
        {
            int score = 0;
            foreach (var (x, y) in matched)
            {
                Ball b = grid[x, y];
                score += GetScoreForColor(b.color);
                b.PlayClearEffect();
                grid[x, y] = null;
            }
            GameManager.Instance.AddScore(score);
        }
    }

    private int GetScoreForColor(BallColor color)
    {
        switch (color)
        {
            case BallColor.Red: return 10;
            case BallColor.Green: return 10;
            case BallColor.Blue: return 10;
            default: return 10;
        }
    }

    private void CheckGameOver()
    {
        for (int x = 0; x < columns; x++)
        {
            if (grid[x, rows - 1] == null)
                return;
        }
        GameManager.Instance.GameOver();
    }
}