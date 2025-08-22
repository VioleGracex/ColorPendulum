using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections;

public class BallSpawner : MonoBehaviour
{
        public Transform spawnHole; // The black circle "hole"
        public GameObject ballPrefab;
        public PendulumController pendulum;
    public int nextQueueSize = 3;
    public int poolSize = 9; // Number of balls in the pool to shuffle from

        private Queue<BallColor> ballQueue = new Queue<BallColor>();

        [Header("Gameplay")]
        private Ball currentBall;
        private bool waitingForPlacement = false;

        private DistanceJoint2D currentJoint;
        private Rigidbody2D currentBallRb;
        private Coroutine breakRoutine;
        private bool canBreakJoint = false;
        [Header("Ball Spawner Options")]
        public bool allowBreak = true; // Set to false to disable breaking

        public void InitAndSpawnFirst()
        {
            ballQueue.Clear();
            GenerateShuffledQueue();
            UpdateNextBallUI();
            SpawnNextBall();
        }

        private void GenerateShuffledQueue()
        {
            // Create a pool with poolSize balls, distributed as evenly as possible among all colors
            List<BallColor> pool = new List<BallColor>();
            int colorCount = System.Enum.GetValues(typeof(BallColor)).Length;
            int perColor = poolSize / colorCount;
            int extra = poolSize % colorCount;
            for (int i = 0; i < colorCount; i++)
            {
                int count = perColor + (i < extra ? 1 : 0);
                for (int j = 0; j < count; j++)
                {
                    pool.Add((BallColor)i);
                }
            }
            // Shuffle and ensure no 3 of the same color in a row
            System.Random rng = new System.Random();
            bool valid = false;
            int maxTries = 20;
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

        public void SpawnNextBall()
        {
            if (GameManager.Instance.state != GameState.Playing) return;
            if (ballQueue.Count == 0)
            {
                GenerateShuffledQueue();
            }

            BallColor color = ballQueue.Dequeue();
            UpdateNextBallUI();


            // Animate ball coming out of hole with scale
            Vector3 spawnPos = spawnHole.position;
            GameObject ballObj = Instantiate(ballPrefab, spawnPos + Vector3.up * 1.5f, Quaternion.identity);
            ballObj.transform.localScale = Vector3.zero;
            canBreakJoint = false;
            ballObj.transform.DOMove(spawnPos, 0.25f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                ballObj.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack).OnComplete(() =>
                {
                    Ball ball = ballObj.GetComponent<Ball>();
                    ball.SetColor(color);
                    currentBall = ball;
                    waitingForPlacement = true;

                    // Ensure Rigidbody2D is enabled and set up
                    Rigidbody2D rb = ballObj.GetComponent<Rigidbody2D>();
                    if (!rb) rb = ballObj.AddComponent<Rigidbody2D>();
                    rb.gravityScale = 0f; // No gravity until attached
                    rb.bodyType = RigidbodyType2D.Dynamic;

                    // Attach DistanceJoint2D and set anchor to pendulum pivot
                    DistanceJoint2D joint = ballObj.GetComponent<DistanceJoint2D>();
                    if (!joint) joint = ballObj.AddComponent<DistanceJoint2D>();
                    joint.autoConfigureConnectedAnchor = false;
                    joint.connectedBody = pendulum.pivot.GetComponent<Rigidbody2D>();
                    joint.anchor = Vector2.zero; // center of ball
                    joint.connectedAnchor = pendulum.pivot.InverseTransformPoint(pendulum.pivot.position);
                    joint.enableCollision = true;

                    // Now attach to pendulum (for reference)
                    pendulum.AttachBall(ball);

                    // Start gravity and swinging
                    rb.gravityScale = 2f;

                    // Apply force to start infinite swing
                    float swingDir = Random.value < 0.5f ? -1f : 1f;
                    float force = 8f; // Tune as needed for desired swing
                    rb.AddForce(new Vector2(swingDir * force, 0), ForceMode2D.Impulse);

                    // Store references for breaking
                    currentJoint = joint;
                    currentBallRb = rb;

                    // Start listening for input to break the joint
                    if (breakRoutine != null) StopCoroutine(breakRoutine);
                    canBreakJoint = true;
                    breakRoutine = StartCoroutine(WaitForBreakInput());
                });
            });
        }
        // Update the next ball UI (show next N balls)
    
        private void UpdateNextBallUI()
        {
            if (UIManager.Instance != null)
            {
                int showCount = Mathf.Min(ballQueue.Count, nextQueueSize);
                BallColor[] nextColors = new BallColor[showCount];
                int idx = 0;
                foreach (var c in ballQueue)
                {
                    if (idx >= showCount) break;
                    nextColors[idx++] = c;
                }
                UIManager.Instance.UpdateNextBalls(nextColors);
            }
        }

        private IEnumerator WaitForBreakInput()
        {
            EnhancedTouchSupport.Enable();
            if (!allowBreak)
            {
                // If breaking is disabled, just wait forever
                yield break;
            }
            bool released = false;

            while (!released)
            {
                if (!canBreakJoint || currentJoint == null)
                {
                    yield return null;
                    continue;
                }
                // Mouse click
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && currentJoint != null)
                    released = true;
                // Touch
                foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
                {
                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began && currentJoint != null)
                    {
                        released = true;
                        break;
                    }
                }
                // Space bar broken
               /*  if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && currentJoint != null)
                 {
                     released = true;
                     break;
                 } */

                // Only check for placedInTube while swinging
                if (waitingForPlacement && currentBall != null && currentBall.placedInTube)
                {
                    // Ball was placed, spawn next ball and stop waiting
                    waitingForPlacement = false;
                    currentBall = null;
                    SpawnNextBall();
                    yield break;
                }

                yield return null;
            }

            // Break the joint and stop applying force instantly
            if (currentJoint != null)
            {
                // Remove RopeRenderer if present, disable line first
                var rope = currentJoint.GetComponent<RopeRenderer>();
                if (rope != null)
                {
                    rope.DisableRope();
                    Destroy(rope);
                }
                Destroy(currentJoint);
                currentJoint = null;
            }

            // Now, after release, check for out-of-bounds (ball not placed in tube and comes to rest)
            while (waitingForPlacement && currentBall != null)
            {
                Rigidbody2D rb = currentBall.GetComponent<Rigidbody2D>();
                float stuckTime = 0f;
                while (waitingForPlacement && currentBall != null)
                {
                    if (currentBall.placedInTube)
                    {
                        // Ball was placed, spawn next ball and stop waiting
                        waitingForPlacement = false;
                        currentBall = null;
                        SpawnNextBall();
                        yield break;
                    }
                    else if (rb != null && rb.linearVelocity.magnitude <= 0.1f)
                    {
                        stuckTime += Time.deltaTime;
                        if (stuckTime >= 0.3f)
                        {
                            // Ball is out of bounds (resting, not placed for 0.3s)
                            Debug.Log("Ball out of bounds! Lost a heart.");
                            Destroy(currentBall.gameObject);
                            waitingForPlacement = false;
                            currentBall = null;
                            GameManager.Instance.SubtractHeart();
                            if (GameManager.Instance.state  != GameState.GameOver)
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
}