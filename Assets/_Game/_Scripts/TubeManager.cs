using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Manages stacking, matching, clearing, lid closing, and game over logic for tubes/columns.
/// Tubes are defined by their OverlapBox regions.
/// When a ball enters a tube's region, it is detected and stacked automatically.
/// </summary>
public class TubeManager : MonoBehaviour
{
    public int columns = 3;
    public int rows = 3;
    private Ball[,] grid;
    private HashSet<Ball> stackedBalls = new HashSet<Ball>();

    [Header("Ball Detection")]
    public LayerMask ballLayerMask;
    public float laneHeight = 6f;
    public float laneOffset = 0f;
    public float laneOffsetY = 0f;
    public float ballSize = 0.5f;

    [SerializeField] Camera overrideCamera;
    private LidsController lidsController;
    private HashSet<Ball> stackingInProgress = new HashSet<Ball>();

    private void Awake()
    {
        grid = new Ball[columns, rows];
        lidsController = FindFirstObjectByType<LidsController>();
    }

    private void Update()
    {
        // Detect balls in each tube's region and stack them if not already stacked or not already stacking
        for (int col = 0; col < columns; col++)
        {
            Vector3 basePos = GetLaneCenter(col);
            Vector2 boxSize = new Vector2(ballSize * 0.9f, laneHeight);
            Vector3 boxCenter = basePos + Vector3.up * (laneHeight / 2f);
            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, ballLayerMask);

            foreach (var hit in hits)
            {
                Ball b = hit.GetComponent<Ball>();
                if (b != null && !stackedBalls.Contains(b) && !stackingInProgress.Contains(b))
                {
                    stackingInProgress.Add(b);
                    // Start smooth stacking (coroutine or Tween)
                    StartCoroutine(StackBallSmoothly(b, col));
                }
            }
        }
    }

    private IEnumerator<WaitForSeconds> StackBallSmoothly(Ball ball, int col)
    {
        // Find first empty row in this column
        int assignedRow = -1;
        for (int row = 0; row < rows; row++)
        {
            if (grid[col, row] == null)
            {
                assignedRow = row;
                break;
            }
        }
        if (assignedRow == -1)
        {
            stackingInProgress.Remove(ball);
            yield break;
        }

        // Disable physics while moving
        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Vector3 basePos = GetLaneCenter(col);
        Vector3 targetPos = basePos + Vector3.up * (assignedRow * ballSize + ballSize / 2f);

        // Tween to slot position
        ball.transform.DOMove(targetPos, 0.2f).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(0.2f);

        // Freeze ball after move
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }

        stackedBalls.Add(ball);
        stackingInProgress.Remove(ball);

        // Update grid to reflect this ball's new position
        UpdateGridByOverlap();

        // Matching logic
        List<(int, int)> matched = GridChecker.GetMatchedBalls(grid, col, assignedRow);
        bool matchFound = matched.Count >= 3;
        if (matchFound)
        {
            int score = 0;
            foreach (var (x, y) in matched)
            {
                Ball b2 = grid[x, y];
                if (b2 != null)
                {
                    score += GetScoreForColor(b2.color);
                    b2.PlayClearEffect();
                    grid[x, y] = null;
                }
            }
            GameManager.Instance.AddScore(score);

            // Update grid after destruction
            UpdateGridByOverlap();
        }

        // Lid Logic
        if (lidsController == null)
            lidsController = FindFirstObjectByType<LidsController>();
        if (lidsController != null)
        {
            for (int lidCol = 0; lidCol < columns; lidCol++)
            {
                int count = 0;
                for (int row = 0; row < rows; row++)
                {
                    if (grid[lidCol, row] != null) count++;
                }
                if (count >= 3)
                    lidsController.CloseLid(lidCol);
            }
        }

        // Game Over Check
        if (IsGridFull())
        {
            GameManager.Instance.GameOver();
            yield break;
        }

        // Spawn Next Ball if you want spawn only after ball is settled else spawn in ball spawner after snapping joint
        //GameManager.Instance.ballSpawner.SpawnNextBall();
    }

    // OverlapBox grid update (use after each stack/match)
    public void UpdateGridByOverlap()
    {
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                grid[x, y] = null;

        for (int col = 0; col < columns; col++)
        {
            Vector3 basePos = GetLaneCenter(col);
            Vector2 boxSize = new Vector2(ballSize * 0.9f, laneHeight);
            Vector3 boxCenter = basePos + Vector3.up * (laneHeight / 2f);
            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, ballLayerMask);
            List<(Ball ball, float y)> foundBalls = new List<(Ball, float)>();
            foreach (var hit in hits)
            {
                Ball b = hit.GetComponent<Ball>();
                if (b != null)
                    foundBalls.Add((b, b.transform.position.y));
            }
            foundBalls.Sort((a, b) => a.y.CompareTo(b.y));
            for (int row = 0; row < rows && row < foundBalls.Count; row++)
            {
                grid[col, row] = foundBalls[row].ball;
            }
        }
    }

    public bool IsGridFull()
    {
        for (int x = 0; x < columns; x++)
        {
            if (grid[x, rows - 1] == null)
                return false;
        }
        return true;
    }

    private Vector3 GetLaneCenter(int col)
    {
        Camera cam = overrideCamera != null ? overrideCamera : Camera.main;
        if (cam == null) return Vector3.zero;
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;
        float laneWidth = camWidth / columns;
        float bottomY = cam.transform.position.y - cam.orthographicSize;
        float startX = cam.transform.position.x - camWidth / 2f + laneWidth / 2f + laneOffset;
        float x = startX + col * laneWidth;
        float y = bottomY + laneOffsetY;
        return new Vector3(x, y, 0f);
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
}