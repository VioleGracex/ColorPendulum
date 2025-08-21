using UnityEngine;

[ExecuteAlways]
public class OrthographicCameraResizer : MonoBehaviour
{
    [Header("Design Resolution")]
    public float targetVerticalSize = 16f; // Set to your designed vertical height in world units (e.g., 16 for 9:16)
    public float targetAspect = 9f / 16f;  // Your base aspect ratio

    void Update()
    {
        ResizeCamera();
    }

    void ResizeCamera()
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        float currentAspect = (float)Screen.width / Screen.height;
        // Always fit the height (recommended for vertical games)
        cam.orthographicSize = targetVerticalSize / 2f;

        // Optionally, if you want to fit width instead for very wide screens:
        // if (currentAspect > targetAspect)
        //     cam.orthographicSize = (targetVerticalSize / 2f) * (targetAspect / currentAspect);
    }
}