using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

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
        EnhancedTouchSupport.Enable();
    }

    void Update()
    {
        float swing = Mathf.Sin((Time.time - startTime) * swingSpeed) * swingAmplitude;
        transform.localRotation = Quaternion.Euler(0, 0, swing);

        bool released = false;
        // PC: Mouse click
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            released = true;
        }
        // Mobile: Touch
        foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
        {
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                released = true;
                break;
            }
        }

        if (attachedBall != null && released)
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
    private void OnDestroy()
    {
        EnhancedTouchSupport.Disable();
    }
}