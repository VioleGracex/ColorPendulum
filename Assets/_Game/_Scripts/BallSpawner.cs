using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections;

/// <summary>
/// Spawns balls, manages queue, pool, UI updates and gameplay controls.
/// - Always shows 3 balls in the UI, in the correct order.
/// - Maintains a "current pool" and "next pool" to avoid reshuffling the order after each throw.
/// - Each pool is only shuffled when it's created, and pools are never shuffled together.
/// - Each pool always has 3 of each color (for 3 colors, poolSize = 9), so game is always solvable.
/// - Keeps track of used balls for replay/analytics or solvability.
/// - Organizes code in clear regions for Algorithm, UI, and Ingame logic.
/// </summary>
public class BallSpawner : MonoBehaviour
{
    #region === Algorithm Fields & Logic ===
    public Transform spawnHole;
    public GameObject ballPrefab;
    public PendulumController pendulum;
    [Header("Ball Pool Settings")]
    public int nextQueueSize = 3;
    public int poolSize = 9;   // Always 9 (3 of each color for 3 colors, for solvable puzzles)

    // Pools: Only swap and shuffle when a pool is fully used!
    private List<BallColor> currentPool = new List<BallColor>();
    private List<BallColor> nextPool = new List<BallColor>();
    private int poolIndex = 0; // Index of next ball to use in currentPool

    private List<BallColor> usedColors = new List<BallColor>();
    private List<BallColor> allColors = new List<BallColor>();
    #endregion

    #region === Ingame Fields & State ===
    [Header("Gameplay")]
    private Ball currentBall;
    private bool waitingForPlacement = false;
    private DistanceJoint2D currentJoint;
    private Rigidbody2D currentBallRb;
    private Coroutine breakRoutine;
    private bool canBreakJoint = false;
    [Header("Ball Spawner Options")]
    public bool allowBreak = true;
    #endregion

    #region === MonoBehaviour / Entry Points ===
    public void InitAndSpawnFirst()
    {
        // Gather all BallColors except BallColor.None, and always only 3!
        allColors.Clear();
        foreach (var bc in System.Enum.GetValues(typeof(BallColor)))
        {
            BallColor c = (BallColor)bc;
            if (c != BallColor.None)
                allColors.Add(c);
        }
        // Enforce: only the first 3 colors allowed!
        if (allColors.Count > 3)
            allColors = allColors.GetRange(0, 3);

        poolSize = 3 * allColors.Count; // Always 9 for 3 colors

        currentPool.Clear();
        nextPool.Clear();
        usedColors.Clear();
        poolIndex = 0;
        GenerateShuffledPool(currentPool, allColors, poolSize);
        GenerateShuffledPool(nextPool, allColors, poolSize);
        UpdateNextBallUI(true);
        SpawnNextBall();
    }
    #endregion

    #region === Algorithm: Pool/Queue Management ===

    /// <summary>
    /// Generates a shuffled pool with exactly poolSize balls, only 3 colors (each 3 times for poolSize=9).
    /// Never generates more than two of the same color in a row.
    /// </summary>
    private void GenerateShuffledPool(List<BallColor> pool, List<BallColor> allowedColors, int amount)
    {
        pool.Clear();
        // Only use the three allowed colors!
        int colorCount = 3;
        // Fill pool with even distribution (3 of each color)
        for (int i = 0; i < colorCount; i++)
        {
            for (int j = 0; j < amount / colorCount; j++)
                pool.Add(allowedColors[i]);
        }

        // Shuffle, ensuring no 3-in-a-row
        System.Random rng = new System.Random();
        bool valid = false;
        int maxTries = 50;
        while (!valid && maxTries-- > 0)
        {
            // Fisher-Yates shuffle
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var temp = pool[i];
                pool[i] = pool[j];
                pool[j] = temp;
            }
            // Check no 3-in-a-row
            valid = true;
            for (int i = 2; i < pool.Count; i++)
            {
                if (pool[i] == pool[i - 1] && pool[i] == pool[i - 2])
                {
                    valid = false;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Get the next BallColor for spawning, swap and shuffle pools only when completely used!
    /// </summary>
    private BallColor GetNextBallColor()
    {
        if (poolIndex >= currentPool.Count)
        {
            // Move nextPool to currentPool, then (and ONLY then) shuffle new nextPool.
            var temp = currentPool;
            currentPool = nextPool;
            nextPool = temp;
            GenerateShuffledPool(nextPool, allColors, poolSize);
            poolIndex = 0;
        }
        BallColor color = currentPool[poolIndex];
        poolIndex++;
        usedColors.Add(color);
        return color;
    }

    /// <summary>
    /// For UI: Get the next N ball colors (across currentPool and nextPool, in order)
    /// </summary>
    private BallColor[] PeekNextBallColors(int n)
    {
        BallColor[] nextColors = new BallColor[n];
        int tempIndex = poolIndex;
        int colorsFromCurrent = Mathf.Min(currentPool.Count - tempIndex, n);

        // Fill from currentPool
        for (int i = 0; i < colorsFromCurrent; i++)
        {
            nextColors[i] = currentPool[tempIndex + i];
        }

        // Fill from nextPool if needed
        for (int i = colorsFromCurrent; i < n; i++)
        {
            if (nextPool.Count > 0)
                nextColors[i] = nextPool[i - colorsFromCurrent];
            else
                nextColors[i] = BallColor.None;
        }
        return nextColors;
    }
    #endregion

    #region === Ingame: Ball Spawning & Placement ===
    public void SpawnNextBall()
    {
        if (GameManager.Instance.state != GameState.Playing) return;

        BallColor color = GetNextBallColor();

        UpdateNextBallUI(false);

        // Animate ball coming out of hole with scale
        Vector3 spawnPos = spawnHole.position;
        GameObject ballObj = Instantiate(ballPrefab, spawnPos + Vector3.up * 1.5f, Quaternion.identity);
        Ball ball = ballObj.GetComponent<Ball>();
        ball.SetColor(color);
        ballObj.transform.localScale = Vector3.zero;
        canBreakJoint = false;
        ballObj.transform.DOMove(spawnPos, 0.25f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            ballObj.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                currentBall = ball;
                waitingForPlacement = true;
                Rigidbody2D rb = ballObj.GetComponent<Rigidbody2D>();
                if (!rb) rb = ballObj.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.bodyType = RigidbodyType2D.Dynamic;

                DistanceJoint2D joint = ballObj.GetComponent<DistanceJoint2D>();
                if (!joint) joint = ballObj.AddComponent<DistanceJoint2D>();
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedBody = pendulum.pivot.GetComponent<Rigidbody2D>();
                joint.anchor = Vector2.zero;
                joint.connectedAnchor = pendulum.pivot.InverseTransformPoint(pendulum.pivot.position);
                joint.enableCollision = true;

                pendulum.AttachBall(ball);
                rb.gravityScale = 2f;
                float swingDir = Random.value < 0.5f ? -1f : 1f;
                float force = 8f;
                rb.AddForce(new Vector2(swingDir * force, 0), ForceMode2D.Impulse);

                currentJoint = joint;
                currentBallRb = rb;
                if (breakRoutine != null) StopCoroutine(breakRoutine);
                canBreakJoint = true;
                breakRoutine = StartCoroutine(WaitForBreakInput());
            });
        });
    }

    private IEnumerator WaitForBreakInput()
    {
        EnhancedTouchSupport.Enable();
        if (!allowBreak) yield break;
        bool released = false;

        while (!released)
        {
            if (!canBreakJoint || currentJoint == null) { yield return null; continue; }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && currentJoint != null)
                released = true;
            foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
            {
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began && currentJoint != null)
                {
                    released = true;
                    break;
                }
            }
            if (waitingForPlacement && currentBall != null && currentBall.placedInTube)
            {
                waitingForPlacement = false;
                currentBall = null;
                SpawnNextBall();
                yield break;
            }
            yield return null;
        }

        if (currentJoint != null)
        {
            var rope = currentJoint.GetComponent<RopeRenderer>();
            if (rope != null)
            {
                rope.DisableRope();
                Destroy(rope);
            }
            Destroy(currentJoint);
            currentJoint = null;
        }

        while (waitingForPlacement && currentBall != null)
        {
            Rigidbody2D rb = currentBall.GetComponent<Rigidbody2D>();
            float stuckTime = 0f;
            while (waitingForPlacement && currentBall != null)
            {
                if (currentBall.placedInTube)
                {
                    waitingForPlacement = false;
                    currentBall = null;
                    SpawnNextBall();
                    yield break;
                }
                else if (rb != null && rb.linearVelocity.magnitude <= 0.1f)
                {
                    stuckTime += Time.deltaTime;
                    if (stuckTime >= 1.5f)
                    {
                        Debug.Log("Ball out of bounds! Lost a heart.");
                        Destroy(currentBall.gameObject);
                        waitingForPlacement = false;
                        currentBall = null;
                        GameManager.Instance.SubtractHeart();
                        if (GameManager.Instance.state != GameState.GameOver)
                        {
                            SpawnNextBall();
                            yield break;
                        }
                    }
                }
                else
                {
                    stuckTime = 0f;
                }
                yield return null;
            }
        }
    }
    #endregion

    #region === UI ===
    // Animate UI balls: always show 3 and animate new balls in correct order
    private void UpdateNextBallUI(bool initial = false)
    {
        if (UIManager.Instance != null)
        {
            BallColor[] nextColors = PeekNextBallColors(nextQueueSize);
            UIManager.Instance.UpdateNextBalls(nextColors, initial);
        }
    }
    #endregion

    #region === Public: For Analytics or Replay ===
    /// <summary>
    /// Returns a list of all colors that have been used/spawned so far.
    /// </summary>
    public List<BallColor> GetUsedColors()
    {
        return new List<BallColor>(usedColors);
    }
    #endregion
}