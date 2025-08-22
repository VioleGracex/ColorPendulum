
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Manages stacking, matching, clearing, lid closing, and game over logic for tubes/columns.
/// Tubes are defined by their OverlapBox regions.
/// When a ball enters a tube's region and slows down (comes to rest), it is centered and stacked automatically.
/// </summary>
public class TubeManager : MonoBehaviour
{
    #region === Inspector Fields ===
    [Header("Tube Grid")]
    [SerializeField] public int columns = 3;
    [SerializeField] public int rows = 3;

    [Header("Ball Detection")]
    [SerializeField] private LayerMask ballLayerMask;
    [SerializeField] public float laneHeight = 6f;
    [SerializeField] private float laneOffset = 0f;
    [SerializeField] private float laneOffsetY = 0f;
    [SerializeField] private float ballSize = 0.5f;

    [Header("Stacking Settings")]
    [SerializeField] private float velocityThreshold = 1f;
    [SerializeField] private int framesAtRestRequired = 3;

    [Header("Camera")]
    [SerializeField] private Camera overrideCamera;
    #endregion

    private Ball[,] grid;
    private readonly HashSet<Ball> stackedBalls = new HashSet<Ball>();
    private readonly HashSet<Ball> stackingInProgress = new HashSet<Ball>();
    private readonly Dictionary<Ball, int> ballRestingFrames = new Dictionary<Ball, int>();
    private LidsController lidsController;
    private float overlapCheckInterval = 0f;
    private float overlapCheckTimer = 0f;
    [SerializeField]
    private BallSpawner ballSpawner;
    

    #region === Unity Lifecycle ===
    private void Awake()
    {
        grid = new Ball[columns, rows];
        lidsController = FindFirstObjectByType<LidsController>();
    }

    private bool shiftingInProgress = false;
    private void Update()
    {
        overlapCheckTimer += Time.deltaTime;
        if (overlapCheckTimer >= overlapCheckInterval)
        {
            overlapCheckTimer = 0f;
            DetectBallsAndStack();
        }
        CleanupRestingBalls();

        // Always check if balls need shifting (e.g., if any are hanging in the air)
        if (!shiftingInProgress && BallsNeedShifting())
        {
            StartCoroutine(ShiftBallsDownSmoothlyAuto());
        }
    }

    // Returns true if any ball is above an empty slot in its column
    private bool BallsNeedShifting()
    {
        for (int col = 0; col < columns; col++)
        {
            bool foundEmpty = false;
            for (int row = 0; row < rows; row++)
            {
                if (grid[col, row] == null)
                    foundEmpty = true;
                else if (foundEmpty)
                    return true; // Ball above an empty slot
            }
        }
        return false;
    }

    // Wrapper to set shiftingInProgress flag
    private IEnumerator ShiftBallsDownSmoothlyAuto()
    {
        shiftingInProgress = true;
        yield return StartCoroutine(ShiftBallsDownSmoothly());
        UpdateGridByOverlap();
        shiftingInProgress = false;
    }

    private void OnDrawGizmos()
    {
        DrawTubeGizmos();
    }
    #endregion

    #region === Gizmo Drawing ===
    private void DrawTubeGizmos()
    {
        // Draw lanes and numbers in the Scene editor (always visible)
        if (columns <= 0 || rows <= 0) return;
        Camera cam = GetActiveCamera();
        if (cam == null) return;
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;
        float laneWidth = camWidth / columns;
        float bottomY = cam.transform.position.y - cam.orthographicSize;
        Color[] colColors = { Color.red, Color.green, Color.blue, Color.magenta, Color.cyan, Color.yellow };
        for (int col = 0; col < columns; col++)
        {
            Vector3 basePos = GetLaneCenter(col);
            // Draw lane area
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(basePos + Vector3.up * (laneHeight / 2f), new Vector3(laneWidth, laneHeight, 0.1f));
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(basePos + Vector3.down * 0.5f, col.ToString());
#endif
            // Draw row markers and overlap boxes
            for (int row = 0; row < rows; row++)
            {
                Vector3 rowPos = basePos + Vector3.up * (row * ballSize + ballSize / 2f);
                // Row marker
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(rowPos, new Vector3(ballSize, ballSize, 0.5f));
                // Overlap box (slightly smaller, colored by column)
                Gizmos.color = colColors[col % colColors.Length];
                Gizmos.DrawWireCube(rowPos, new Vector3(ballSize * 0.9f, ballSize * 0.9f, 0.2f));
            }
        }
    }
    #endregion

    #region === Tube Logic ===

    private void DetectBallsAndStack()
    {
        // Detect balls in each tube's region and stack them if not already stacked or stacking
        for (int col = 0; col < columns; col++)
        {
            Vector3 basePos = GetLaneCenter(col);
            Vector2 boxSize = new Vector2(ballSize * 0.9f, laneHeight);
            Vector3 boxCenter = basePos + Vector3.up * (laneHeight / 2f);
            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, ballLayerMask);

            foreach (var hit in hits)
            {
                Ball b = hit.GetComponent<Ball>();
                if (b == null || stackedBalls.Contains(b) || stackingInProgress.Contains(b))
                    continue;

                Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
                if (rb == null) continue;

                // Check if the ball is "slow" enough
                if (rb.linearVelocity.magnitude < velocityThreshold)
                {
                    // Increment resting frame count
                    if (!ballRestingFrames.ContainsKey(b))
                        ballRestingFrames[b] = 1;
                    else
                        ballRestingFrames[b]++;

                    // If slow for enough frames, start stacking
                    if (ballRestingFrames[b] >= framesAtRestRequired)
                    {
                        stackingInProgress.Add(b);
                        ballRestingFrames.Remove(b);
                        StartCoroutine(StackBallSmoothly(b, col));
                    }
                }
                else
                {
                    // Reset resting frame count if ball speeds up again
                    ballRestingFrames[b] = 0;
                }
            }
        }
    }


    private void CleanupRestingBalls()
    {
        // Remove balls from rest dict if they're not in any tube anymore
        var ballsToRemove = new List<Ball>();
        foreach (var kv in ballRestingFrames)
        {
            Ball b = kv.Key;
            bool found = false;
            for (int col = 0; col < columns; col++)
            {
                Vector3 basePos = GetLaneCenter(col);
                Vector2 boxSize = new Vector2(ballSize * 0.9f, laneHeight);
                Vector3 boxCenter = basePos + Vector3.up * (laneHeight / 2f);
                Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, ballLayerMask);
                foreach (var hit in hits)
                {
                    if (hit.GetComponent<Ball>() == b)
                    {
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }
            if (!found)
                ballsToRemove.Add(b);
        }
        foreach (var b in ballsToRemove)
            ballRestingFrames.Remove(b);
    }


    public IEnumerator StackBallSmoothly(Ball ball, int col)
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
        yield return ball.transform.DOMove(targetPos, 0.2f).SetEase(Ease.OutBack).WaitForCompletion();

        // Freeze ball after move
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }

        stackedBalls.Add(ball);
        stackingInProgress.Remove(ball);
        ball.placedInTube = true;

        // Update grid to reflect this ball's new position
        UpdateGridByOverlap();

        // Matching logic
        List<(int, int)> matched = GridChecker.GetMatchedBalls(grid, col, assignedRow);
        bool matchFound = matched.Count >= 3;
        if (matchFound)
        {
            // Detect match types for multiplier
            int horizCount = 0, vertCount = 0, diagCount = 0;
            BallColor matchColor = ball.color;
            // Horizontal
            for (int x = 0; x < columns; x++)
                if (grid[x, assignedRow] != null && grid[x, assignedRow].color == matchColor)
                    horizCount++;
            // Vertical
            for (int y = 0; y < rows; y++)
                if (grid[col, y] != null && grid[col, y].color == matchColor)
                    vertCount++;
            // Diagonal /
            int diag1Count = 0;
            for (int d = -2; d <= 2; d++)
            {
                int x = col + d;
                int y = assignedRow + d;
                if (x >= 0 && x < columns && y >= 0 && y < rows)
                    if (grid[x, y] != null && grid[x, y].color == matchColor)
                        diag1Count++;
            }
            // Diagonal \
            int diag2Count = 0;
            for (int d = -2; d <= 2; d++)
            {
                int x = col + d;
                int y = assignedRow - d;
                if (x >= 0 && x < columns && y >= 0 && y < rows)
                    if (grid[x, y] != null && grid[x, y].color == matchColor)
                        diag2Count++;
            }
            diagCount = Mathf.Max(diag1Count, diag2Count);

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

            // Score multiplier logic
            int multiplier = 1;
            bool horiz5 = horizCount >= 5;
            bool vert5 = vertCount >= 5;
            bool both5 = horiz5 && vert5;
            bool diag = diagCount >= 3;
            if (both5)
                multiplier = 4;
            else if (diag && diagCount >= 5)
                multiplier = 2;

            GameManager.Instance.AddScore(score * multiplier);

            // Animate balls falling into empty slots before updating grid
            yield return StartCoroutine(ShiftBallsDownSmoothly());

            // Update grid after destruction and shifting
            UpdateGridByOverlap();
        }

        // Always update lids after stacking and any possible match/clear
        UpdateAllLids();

        // Game Over Check
        if (IsGridFull() && !matchFound)
        {
            GameManager.Instance.GameOver();
            yield break;
        }

        // Always spawn next ball if game is still playing (after all clears/lids)
        /* if (!ballSpawner)
            ballSpawner = FindFirstObjectByType<BallSpawner>();
        if (GameManager.Instance != null && GameManager.Instance.state == GameState.Playing)
            ballSpawner?.SpawnNextBall(); */
    }

    // Updates all lids for all columns based on current grid state
    private void UpdateAllLids()
    {
        if (lidsController == null)
            lidsController = FindFirstObjectByType<LidsController>();
        if (lidsController == null) return;
        for (int lidCol = 0; lidCol < columns; lidCol++)
        {
            int count = 0;
            for (int row = 0; row < rows; row++)
            {
                if (grid[lidCol, row] != null) count++;
            }
            // Check for vertical match-3 in this column
            bool hasMatch = false;
            if (count >= 3)
            {
                BallColor? lastColor = null;
                int streak = 0;
                for (int row = 0; row < rows; row++)
                {
                    Ball b = grid[lidCol, row];
                    if (b != null)
                    {
                        if (lastColor.HasValue && b.color == lastColor.Value)
                            streak++;
                        else
                            streak = 1;
                        lastColor = b.color;
                        if (streak >= 3)
                        {
                            hasMatch = true;
                            break;
                        }
                    }
                    else
                    {
                        streak = 0;
                        lastColor = null;
                    }
                }
                if (!hasMatch)
                    lidsController.CloseLid(lidCol);
                else
                    lidsController.OpenLid(lidCol);
            }
            else
            {
                // Less than 3 balls: always open lid
                lidsController.OpenLid(lidCol);
            }
        }
    }

    /// <summary>
    /// Animates all balls in each column to fall smoothly into the lowest available slots after clears.
    /// </summary>
    private IEnumerator ShiftBallsDownSmoothly()
    {
        float moveDuration = 0.2f;
        List<Tweener> tweens = new List<Tweener>();
        for (int col = 0; col < columns; col++)
        {
            // Gather balls in this column, from bottom to top
            List<Ball> ballsInCol = new List<Ball>();
            for (int row = 0; row < rows; row++)
            {
                if (grid[col, row] != null)
                    ballsInCol.Add(grid[col, row]);
            }
            // Animate each ball to its new row position (bottom up)
            for (int newRow = 0; newRow < ballsInCol.Count; newRow++)
            {
                Ball ball = ballsInCol[newRow];
                Vector3 basePos = GetLaneCenter(col);
                Vector3 targetPos = basePos + Vector3.up * (newRow * ballSize + ballSize / 2f);
                // Only animate if not already at target position
                if ((ball.transform.position - targetPos).sqrMagnitude > 0.001f)
                {
                    tweens.Add(ball.transform.DOMove(targetPos, moveDuration).SetEase(Ease.InOutQuad));
                }
            }
        }
        if (tweens.Count > 0)
            yield return DOTween.Sequence().AppendInterval(moveDuration).WaitForCompletion();
    }

    /// <summary>
    /// Updates grid array using OverlapBoxAll in each column.
    /// Should be called after any change to ball positions or destruction.
    /// </summary>
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


    #region === Utility Methods ===
    private Vector3 GetLaneCenter(int col)
    {
        Camera cam = GetActiveCamera();
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

    private Camera GetActiveCamera()
    {
        if (overrideCamera != null)
            return overrideCamera;
        return Camera.main;
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
    #endregion

    #region === Cleanup ===
    /// <summary>
    /// Clears all tubes, destroys all balls, resets all state. Call when returning to menu.
    /// </summary>
    public void ClearAllTubesAndBalls()
    {
        // Destroy all balls
        var allBalls = FindObjectsByType<Ball>(FindObjectsSortMode.None);
        foreach (var ball in allBalls)
        {
            ball.PlayClearEffect();
        }
        // Reset all containers
        grid = new Ball[columns, rows];
        stackedBalls.Clear();
        stackingInProgress.Clear();
        ballRestingFrames.Clear();
    }
    #endregion
    #endregion
}