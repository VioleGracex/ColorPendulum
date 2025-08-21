using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

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

    public void SpawnNextBall()
    {
        if (GameManager.Instance.state != GameState.Playing) return;
        if (ballQueue.Count == 0)
        {
            GenerateSolvableQueue();
        }

        BallColor color = ballQueue.Dequeue();
        UIManager.Instance.UpdateNextBalls(ballQueue.ToArray());

        // Animate ball coming out of hole
        Vector3 spawnPos = spawnHole.position;
        GameObject ballObj = Instantiate(ballPrefab, spawnPos + Vector3.up * 1.5f, Quaternion.identity);
        ballObj.transform.DOMove(spawnPos, 0.25f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            Ball ball = ballObj.GetComponent<Ball>();
            ball.SetColor(color);

            // Ensure Rigidbody2D is enabled and set up
            Rigidbody2D rb = ballObj.GetComponent<Rigidbody2D>();
            if (!rb) rb = ballObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // No gravity until attached
            rb.bodyType = RigidbodyType2D.Dynamic;

            // Attach DistanceJoint2D and set anchor to spawnHole
            DistanceJoint2D joint = ballObj.GetComponent<DistanceJoint2D>();
            if (!joint) joint = ballObj.AddComponent<DistanceJoint2D>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedBody = pendulum.pivot.GetComponent<Rigidbody2D>();
            joint.anchor = ballObj.transform.InverseTransformPoint(ballObj.transform.position);
            joint.connectedAnchor = pendulum.pivot.InverseTransformPoint(pendulum.pivot.position);
            joint.enableCollision = false;

            // Now attach to pendulum (for reference)
            pendulum.AttachBall(ball);

            // Start gravity and swinging
            rb.gravityScale = 2f;
        });
    }
}