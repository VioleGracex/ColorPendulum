using UnityEngine;

public class PendulumController : MonoBehaviour
{
    public Transform pivot;
    public float swingAmplitude = 45f;
    public float swingSpeed = 1.5f;
    private float startTime;
    private Ball attachedBall;
    private DistanceJoint2D joint;

    void Start()
    {
        startTime = Time.time;
    }

    void Update()
    {
        float swing = Mathf.Sin((Time.time - startTime) * swingSpeed) * swingAmplitude;
        transform.localRotation = Quaternion.Euler(0, 0, swing);

        if (attachedBall != null && Input.GetMouseButtonDown(0))
        {
            ReleaseBall();
        }
    }

    public void AttachBall(Ball ball)
    {
        attachedBall = ball;
        joint = ball.gameObject.AddComponent<DistanceJoint2D>();
        joint.connectedBody = pivot.GetComponent<Rigidbody2D>();
        // Set joint parameters
    }

    private void ReleaseBall()
    {
        Destroy(joint);
        attachedBall.OnReleased();
        attachedBall = null;
        // Tell GameManager/BallSpawner to spawn next ball after a delay
    }
}