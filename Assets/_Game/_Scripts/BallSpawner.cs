using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections;

/// <summary>
/// Spawns balls, manages queue, pool, UI updates and gameplay controls.
/// - Always shows 3 balls in the UI.
/// - Every 3 balls used from the pool, generates 3 more (while preserving "no 3 in a row" rule).
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
    public int poolSize = 9; // Initial pool size (will be expanded in chunks of 3)
    private Queue<BallColor> ballQueue = new Queue<BallColor>();
    private List<BallColor> usedColors = new List<BallColor>();
    private int poolRefillChunk = 3;
    private int ballsUsedSinceLastRefill = 0;
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

    #region === MonoBehavior / Entry Points ===
    public void InitAndSpawnFirst()
    {
        ballQueue.Clear();
        usedColors.Clear();
        ballsUsedSinceLastRefill = 0;
        GenerateShuffledQueue(poolSize);
        EnsureQueueSize(nextQueueSize);
        UpdateNextBallUI(true);
        SpawnNextBall();
    }
    #endregion

    #region === Algorithm: Pool/Queue Management ===

    // Generates a shuffled pool, ensuring no 3 of the same color in a row, and enqueues all into ballQueue
    private void GenerateShuffledQueue(int amount)
    {
        List<BallColor> pool = new List<BallColor>();
        int colorCount = System.Enum.GetValues(typeof(BallColor)).Length;
        int perColor = amount / colorCount;
        int extra = amount % colorCount;
        for (int i = 0; i < colorCount; i++)
        {
            int count = perColor + (i < extra ? 1 : 0);
            for (int j = 0; j < count; j++)
                pool.Add((BallColor)i);
        }

        // Shuffle and ensure no 3 of the same color in a row
        System.Random rng = new System.Random();
        bool valid = false;
        int maxTries = 20;
        while (!valid && maxTries-- > 0)
        {
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var temp = pool[i];
                pool[i] = pool[j];
                pool[j] = temp;
            }
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
        foreach (var c in pool) ballQueue.Enqueue(c);
    }

    // Ensures the queue always has at least "minSize" balls, filling with random colors if needed
    private void EnsureQueueSize(int minSize)
    {
        int colorCount = System.Enum.GetValues(typeof(BallColor)).Length;
        while (ballQueue.Count < minSize)
        {
            BallColor randomColor = (BallColor)Random.Range(0, colorCount);
            BallColor[] arr = ballQueue.ToArray();
            if (arr.Length >= 2 && arr[arr.Length - 1] == randomColor && arr[arr.Length - 2] == randomColor)
            {
                List<BallColor> possible = new List<BallColor>();
                for (int i = 0; i < colorCount; i++)
                    if ((BallColor)i != randomColor)
                        possible.Add((BallColor)i);
                randomColor = possible[Random.Range(0, possible.Count)];
            }
            ballQueue.Enqueue(randomColor);
        }
    }

    // Every 3 balls used from the pool, generate 3 more and add to queue.
    private void RefillPoolIfNeeded()
    {
        ballsUsedSinceLastRefill++;
        if (ballsUsedSinceLastRefill >= poolRefillChunk)
        {
            GenerateShuffledQueue(poolRefillChunk);
            ballsUsedSinceLastRefill = 0;
        }
    }
    #endregion

    #region === Ingame: Ball Spawning & Placement ===
    public void SpawnNextBall()
    {
        if (GameManager.Instance.state != GameState.Playing) return;
        if (ballQueue.Count == 0)
            GenerateShuffledQueue(poolRefillChunk);

        BallColor color = ballQueue.Dequeue();
        usedColors.Add(color);

        RefillPoolIfNeeded();
        EnsureQueueSize(nextQueueSize); // Always keep the UI fed

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
                    if (stuckTime >= 0.5f)
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
    // Animate UI balls: always show 3 and animate new balls in
    private void UpdateNextBallUI(bool initial = false)
    {
        if (UIManager.Instance != null)
        {
            BallColor[] nextColors = new BallColor[nextQueueSize];
            int idx = 0;
            foreach (var c in ballQueue)
            {
                if (idx >= nextQueueSize) break;
                nextColors[idx++] = c;
            }
            for (int i = idx; i < nextQueueSize; i++)
                nextColors[i] = BallColor.None;
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