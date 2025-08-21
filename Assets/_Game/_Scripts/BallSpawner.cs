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

    private Queue<BallColor> ballQueue = new Queue<BallColor>();

    public void InitAndSpawnFirst()
    {
        ballQueue.Clear();
        GenerateSolvableQueue();
        UIManager.Instance.UpdateNextBalls(ballQueue.ToArray());
        SpawnNextBall();
    }

    private void GenerateSolvableQueue()
    {
        // Always guarantee at least 3 of the same color in the nextQueue
        BallColor guaranteed = (BallColor)Random.Range(0, 3);
        List<BallColor> colors = new List<BallColor>();
        for (int i = 0; i < 3; i++) colors.Add(guaranteed);
        while (colors.Count < nextQueueSize)
            colors.Add((BallColor)Random.Range(0, 3));
        // Shuffle for some randomness
        for (int i = 0; i < colors.Count; i++)
        {
            int j = Random.Range(i, colors.Count);
            var temp = colors[i];
            colors[i] = colors[j];
            colors[j] = temp;
        }
        foreach (var c in colors) ballQueue.Enqueue(c);
    }

    private DistanceJoint2D currentJoint;
    private Rigidbody2D currentBallRb;
    private Coroutine breakRoutine;
    private bool canBreakJoint = false;

    public void SpawnNextBall()
    {
        if (GameManager.Instance.state != GameState.Playing) return;
        if (ballQueue.Count == 0)
        {
            GenerateSolvableQueue();
        }

        BallColor color = ballQueue.Dequeue();
        UIManager.Instance.UpdateNextBalls(ballQueue.ToArray());

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

    private IEnumerator WaitForBreakInput()
    {
        EnhancedTouchSupport.Enable();
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
            // Space bar
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && currentJoint != null)
            {
                released = true;
                break;
            }

            yield return null;
        }

        // Break the joint and stop applying force
        bool brokeJoint = false;
        if (currentJoint != null)
        {
            Destroy(currentJoint);
            currentJoint = null;
            brokeJoint = true;
        }

        // Only spawn next ball if joint was actually broken (not first ball or already detached)
        if (brokeJoint)
        {
            yield return new WaitForSeconds(0.5f);
            SpawnNextBall();
        }
    }
}