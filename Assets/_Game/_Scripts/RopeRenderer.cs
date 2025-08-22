using UnityEngine;

/// <summary>
/// Renders a rope using LineRenderer between the object's DistanceJoint2D and its connected body.
/// Attach this to the same GameObject as the DistanceJoint2D (e.g. your ball).
/// </summary>
public class RopeRenderer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private DistanceJoint2D joint;
    private bool disabled = false;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        joint = GetComponent<DistanceJoint2D>();
    }

    /// <summary>
    /// Disables the rope rendering and stops updating the line.
    /// </summary>
    public void DisableRope()
    {
        disabled = true;
        if (lineRenderer != null)
            lineRenderer.enabled = false;
        Destroy(lineRenderer);
    }

    void Update()
    {
        if (disabled)
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            return;
        }
        if (joint == null || joint.connectedBody == null)
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            return;
        }

        if (lineRenderer != null)
            lineRenderer.enabled = true;

        // Ball's anchor position (world)
        Vector3 ballAnchor = transform.TransformPoint(joint.anchor);

        // Connected body's anchor position (world)
        Vector3 connectedAnchor = joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);

        // Set line positions
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, connectedAnchor);
        lineRenderer.SetPosition(1, ballAnchor);
    }
}