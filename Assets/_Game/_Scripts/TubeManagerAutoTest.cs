using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;

/// <summary>
/// Manual test runner for TubeManager & BallSpawner using NaughtyAttributes.
/// Attach to an empty GameObject in the scene with references set in the inspector.
/// Use buttons in the Inspector to run each test or all tests.
/// </summary>
public class TubeManagerAutoTest : MonoBehaviour
{
    public TubeManager tubeManager;
    public BallSpawner ballSpawner;
    public GameManager gameManager;
    public float ballStackDelay = 1.0f; // Time between fake drops

    [Button("Run All TubeManager Tests")]
    public void ButtonRunAllTests()
    {
        if (!CanRunTests()) return;
        StartCoroutine(RunAllTests());
    }

    [Button("Test Vertical Match-3")]
    public void ButtonTest_VerticalMatch3()
    {
        if (!CanRunTests()) return;
        StartCoroutine(Test_VerticalMatch3());
    }

    [Button("Test Horizontal Match-3")]
    public void ButtonTest_HorizontalMatch3()
    {
        if (!CanRunTests()) return;
        StartCoroutine(Test_HorizontalMatch3());
    }

    [Button("Test Diagonal Match-3")]
    public void ButtonTest_DiagonalMatch3()
    {
        if (!CanRunTests()) return;
        StartCoroutine(Test_DiagonalMatch3());
    }

    [Button("Test Non-Matching 3 (Lid Close)")]
    public void ButtonTest_NonMatching3_LidClose()
    {
        if (!CanRunTests()) return;
        StartCoroutine(Test_NonMatching3_LidClose());
    }

    [Button("Test Matching in Different Lanes/Columns")]
    public void ButtonTest_MultiColumnRowMatch()
    {
        if (!CanRunTests()) return;
        StartCoroutine(Test_MultiColumnRowMatch());
    }

    [Button("Test Shift and Chain Matching")]
    public void ButtonTest_ShiftAndChainMatch()
    {
        if (!CanRunTests()) return;
        StartCoroutine(Test_ShiftAndChainMatch());
    }

    [Button("Test Last Match (Any Direction, Not Game Over)")]
    public void ButtonTest_LastMatchNotGameOver()
    {
        if (!CanRunTests()) return;
        StartCoroutine(Test_LastMatchNotGameOver());
    }

    [Button("Test Game Over")]
    public void ButtonTest_GameOver()
    {
        if (!CanRunTests()) return;
        StartCoroutine(Test_GameOver());
    }

    [Button("Test Cleanup")]
    public void ButtonTest_Cleanup()
    {
        if (!CanRunTests()) return;
        StartCoroutine(Test_Cleanup());
    }

    private bool CanRunTests()
    {
        if (gameManager == null || gameManager.state != GameState.Playing)
        {
            Debug.LogWarning("Cannot run test: Game is not in Playing state.");
            return false;
        }
        return true;
    }

    private IEnumerator RunAllTests()
    {
        Debug.Log("=== TubeManager Automated Tests Start ===");
        yield return Test_VerticalMatch3();
        yield return Test_HorizontalMatch3();
        yield return Test_DiagonalMatch3();
        yield return Test_NonMatching3_LidClose();
        yield return Test_MultiColumnRowMatch();
        yield return Test_ShiftAndChainMatch();
        yield return Test_LastMatchNotGameOver();
        yield return Test_GameOver();
        yield return Test_Cleanup();
        Debug.Log("=== TubeManager Automated Tests End ===");
    }

    private IEnumerator Test_VerticalMatch3()
    {
        Debug.Log("Test: Vertical Match-3");
        yield return CleanupAndWait();
        for (int i = 0; i < 3; i++)
        {
            Ball ball = SpawnTestBall(BallColor.Red, col: 0);
            yield return StackAndWait(ball, 0);
        }
        yield return new WaitForSeconds(0.5f);
        Assert(tubeManagerIsColumnEmpty(0), "Vertical match-3 did not clear column 0");
    }

    private IEnumerator Test_HorizontalMatch3()
    {
        Debug.Log("Test: Horizontal Match-3");
        yield return CleanupAndWait();
        for (int col = 0; col < 3; col++)
        {
            Ball ball = SpawnTestBall(BallColor.Green, col);
            yield return StackAndWait(ball, col);
        }
        yield return new WaitForSeconds(0.5f);
        for (int col = 0; col < 3; col++)
            Assert(tubeManagerIsSlotEmpty(col, 0), $"Horizontal match-3 did not clear col {col}, row 0");
    }

    private IEnumerator Test_DiagonalMatch3()
    {
        Debug.Log("Test: Diagonal Match-3");
        yield return CleanupAndWait();
        var coords = new[] { (0, 0), (1, 1), (2, 2) };
        foreach (var (col, row) in coords)
        {
            for (int r = 0; r < row; r++)
                yield return StackAndWait(SpawnTestBall(BallColor.Red, col), col);
            yield return StackAndWait(SpawnTestBall(BallColor.Blue, col), col);
        }
        yield return new WaitForSeconds(0.5f);
        foreach (var (col, row) in coords)
            Assert(tubeManagerIsSlotEmpty(col, row), $"Diagonal match-3 did not clear {col},{row}");
    }

    private IEnumerator Test_NonMatching3_LidClose()
    {
        Debug.Log("Test: Non-matching 3 - Lid Close");
        yield return CleanupAndWait();
        BallColor[] colors = { BallColor.Red, BallColor.Green, BallColor.Blue };
        for (int i = 0; i < 3; i++)
            yield return StackAndWait(SpawnTestBall(colors[i], 1), 1);
        yield return new WaitForSeconds(0.5f);
        if (tubeManager.TryGetComponent<LidsController>(out var lids))
        {
            Assert(lids.IsLidClosed(1), "Lid was not closed on column 1");
        }
        else
        {
            Debug.LogWarning("LidsController.IsLidClosed check skipped (not implemented)");
        }
    }

    private IEnumerator Test_MultiColumnRowMatch()
    {
        Debug.Log("Test: Multi-Lane/Column Matching");
        yield return CleanupAndWait();
        // Diagonal Green
        for (int i = 0; i < 3; i++)
        {
            for (int r = 0; r < i; r++)
                yield return StackAndWait(SpawnTestBall(BallColor.Red, i), i);
            yield return StackAndWait(SpawnTestBall(BallColor.Green, i), i);
        }
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < 3; i++)
            Assert(tubeManagerIsSlotEmpty(i, i), $"Diagonal (green) did not clear {i},{i}");

        // Anti-diagonal Blue
        for (int r = 0; r < 2; r++) yield return StackAndWait(SpawnTestBall(BallColor.Red, 0), 0);
        yield return StackAndWait(SpawnTestBall(BallColor.Blue, 0), 0);
        yield return StackAndWait(SpawnTestBall(BallColor.Red, 1), 1);
        yield return StackAndWait(SpawnTestBall(BallColor.Blue, 1), 1);
        yield return StackAndWait(SpawnTestBall(BallColor.Blue, 2), 2);

        yield return new WaitForSeconds(0.5f);

        Assert(tubeManagerIsSlotEmpty(0, 2), "Anti-diagonal (blue) did not clear (0,2)");
        Assert(tubeManagerIsSlotEmpty(1, 1), "Anti-diagonal (blue) did not clear (1,1)");
        Assert(tubeManagerIsSlotEmpty(2, 0), "Anti-diagonal (blue) did not clear (2,0)");
    }

    private IEnumerator Test_ShiftAndChainMatch()
    {
        Debug.Log("Test: Shifting and Chain Match");
        yield return CleanupAndWait();

        // Stack (from bottom to top in col 0): Red, Green, Green
        yield return StackAndWait(SpawnTestBall(BallColor.Red, 0), 0);
        yield return StackAndWait(SpawnTestBall(BallColor.Green, 0), 0);
        yield return StackAndWait(SpawnTestBall(BallColor.Green, 0), 0);

        // Now stack 2 Green in col 1, then one Red at top
        yield return StackAndWait(SpawnTestBall(BallColor.Green, 1), 1);
        yield return StackAndWait(SpawnTestBall(BallColor.Green, 1), 1);
        yield return StackAndWait(SpawnTestBall(BallColor.Red, 1), 1);

        // Now, clear (0,1), (0,2), (1,0), (1,1) by adding Green to (1,2) to trigger vertical and horizontal match after shifting
        yield return StackAndWait(SpawnTestBall(BallColor.Green, 1), 1);

        yield return new WaitForSeconds(1f);

        Assert(tubeManagerIsSlotEmpty(1, 0), "After shift Red at (1,0) should be cleared (was part of a match)");
        Assert(tubeManagerIsSlotEmpty(1, 1), "After shift (1,1) should be empty");
        Assert(tubeManagerIsSlotEmpty(1, 2), "After shift (1,2) should be empty");
    }

    // This test checks that the last match (vertical, horizontal, diagonal) with a full grid does not trigger gameover
    private IEnumerator Test_LastMatchNotGameOver()
    {
        int cols = tubeManager.columns;
        int rows = tubeManager.rows;
        Debug.Log("Test: Last Match Not Game Over (VERTICAL)");
        yield return CleanupAndWait();

        // Fill grid except last column with different colors (no matches)
        BallColor[] safeColors = { BallColor.Red, BallColor.Green, BallColor.Blue, BallColor.Magenta};
        int colorIdx = 0;
        for (int col = 0; col < cols - 1; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                BallColor color = safeColors[colorIdx++ % safeColors.Length];
                yield return StackAndWait(SpawnTestBall(color, col), col);
            }
        }
        // Last column: stack 3 Reds for vertical match
        for (int row = 0; row < 3; row++)
            yield return StackAndWait(SpawnTestBall(BallColor.Red, cols - 1), cols - 1);

        yield return new WaitForSeconds(1f);
        Assert(gameManager.state == GameState.Playing, "VERTICAL: Game should not be over after last column match.");

        Debug.Log("Test: Last Match Not Game Over (HORIZONTAL)");
        yield return CleanupAndWait();

        // Fill grid except last row with different colors (no matches)
        colorIdx = 0;
        for (int row = 1; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                BallColor color = safeColors[colorIdx++ % safeColors.Length];
                yield return StackAndWait(SpawnTestBall(color, col), col);
            }
        }
        // Last row: stack 3 Blues for horizontal match
        for (int col = 0; col < cols; col++)
            yield return StackAndWait(SpawnTestBall(BallColor.Blue, col), col);

        yield return new WaitForSeconds(1f);
        Assert(gameManager.state == GameState.Playing, "HORIZONTAL: Game should not be over after last row match.");

        Debug.Log("Test: Last Match Not Game Over (DIAGONAL)");
        yield return CleanupAndWait();

        // Fill grid except diagonal
        colorIdx = 0;
        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                if (col == row) continue; // diagonal is left empty
                BallColor color = safeColors[colorIdx++ % safeColors.Length];
                yield return StackAndWait(SpawnTestBall(color, col), col);
            }
        }
        // Now fill diagonal with match color
        for (int i = 0; i < cols; i++)
            yield return StackAndWait(SpawnTestBall(BallColor.Green, i), i);

        yield return new WaitForSeconds(1f);
        Assert(gameManager.state == GameState.Playing, "DIAGONAL: Game should not be over after diagonal match.");

        // Now fill all empty slots with non-matching colors to fill grid fully (should game over now)
        colorIdx = 0;
        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                if (!tubeManagerIsSlotEmpty(col, row)) continue;
                BallColor color = safeColors[colorIdx++ % safeColors.Length];
                yield return StackAndWait(SpawnTestBall(color, col), col);
            }
        }
        yield return new WaitForSeconds(0.5f);
        Assert(gameManager.state == GameState.GameOver, "Game did not end after grid filled post-last match.");
    }

    private IEnumerator Test_GameOver()
    {
        Debug.Log("Test: Game Over");
        yield return CleanupAndWait();

        // Use a pattern that guarantees no 3-in-a-row in any direction
        BallColor[,] pattern = new BallColor[3,3]
        {
            { BallColor.Red,   BallColor.Green, BallColor.Blue },
            { BallColor.Green, BallColor.Magenta,  BallColor.Red  },
            { BallColor.Magenta,  BallColor.Red,   BallColor.Green }
        };

        for (int row = 0; row < tubeManager.rows; row++)
        {
            for (int col = 0; col < tubeManager.columns; col++)
            {
                BallColor color = pattern[row, col];
                yield return StackAndWait(SpawnTestBall(color, col), col);
            }
        }

        yield return new WaitForSeconds(0.5f);
        Assert(gameManager.state == GameState.GameOver, "Game did not end when grid was full");
    }

    private IEnumerator Test_Cleanup()
    {
        Debug.Log("Test: ClearAllBalls");
        yield return CleanupAndWait();
        yield return new WaitForSeconds(1f);

        Assert(FindObjectsByType<Ball>(FindObjectsSortMode.None).Length == 0, "ClearAllBalls did not remove all balls");
        gameManager.OnMenuButtonClicked();
    }

    private IEnumerator CleanupAndWait()
    {
        tubeManager.ClearAllBalls();
        yield return WaitPhysicsFrame();
        yield return new WaitForSeconds(0.2f);
    }

    private Ball SpawnTestBall(BallColor color, int col)
    {
        Vector3 pos = tubeManager.transform.position + Vector3.up * 10f;
        GameObject ballGO = Instantiate(ballSpawner.ballPrefab, pos, Quaternion.identity);
        Ball ball = ballGO.GetComponent<Ball>();
        ball.SetColor(color);

        Vector3 slot = tubeManager.GetType()
            .GetMethod("GetLaneCenter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(tubeManager, new object[] { col }) as Vector3? ?? Vector3.zero;
        ballGO.transform.position = slot + Vector3.up * (tubeManager.laneHeight + 1f);

        Rigidbody2D rb = ballGO.GetComponent<Rigidbody2D>();
        if (!rb) rb = ballGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        return ball;
    }

    private IEnumerator StackAndWait(Ball ball, int col)
    {
        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        var stackingField = tubeManager.GetType().GetField("stackingInProgress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var stackingSet = (HashSet<Ball>)stackingField.GetValue(tubeManager);
        stackingSet.Add(ball);

        tubeManager.StartCoroutine(tubeManager.StackBallSmoothly(ball, col));
        yield return new WaitForSeconds(0.25f);
    }

    private IEnumerator WaitPhysicsFrame()
    {
        yield return new WaitForFixedUpdate();
        yield return null;
    }

    private void Assert(bool condition, string message)
    {
        if (!condition)
            Debug.LogError("Test Failed: " + message);
        else
            Debug.Log("Test Passed: " + message);
    }

    private bool tubeManagerIsColumnEmpty(int col)
    {
        var gridField = tubeManager.GetType().GetField("grid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var grid = (Ball[,])gridField.GetValue(tubeManager);
        for (int row = 0; row < tubeManager.rows; row++)
            if (grid[col, row] != null)
                return false;
        return true;
    }
    private bool tubeManagerIsSlotEmpty(int col, int row)
    {
        var gridField = tubeManager.GetType().GetField("grid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var grid = (Ball[,])gridField.GetValue(tubeManager);
        return grid[col, row] == null;
    }
}