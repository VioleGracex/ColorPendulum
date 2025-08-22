using UnityEngine;

[RequireComponent(typeof(Canvas))]
[ExecuteAlways]
public class WorldSpaceCanvasResizer : MonoBehaviour
{
    public Camera targetCamera;
    public float unitsPerScreenHeight = 1f; // Scale factor

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetCamera.orthographic)
        {
            float height = targetCamera.orthographicSize * 2f * unitsPerScreenHeight;
            float width = height * targetCamera.aspect;

            rectTransform.sizeDelta = new Vector2(width, height);

            // Optional: Keep canvas in front of camera
            transform.position = targetCamera.transform.position + targetCamera.transform.forward * 5f;
            transform.rotation = targetCamera.transform.rotation;
        }
        else
        {
            // Perspective case: adjust based on distance & FOV
            float distance = Vector3.Distance(transform.position, targetCamera.transform.position);
            float height = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * targetCamera.aspect;

            rectTransform.sizeDelta = new Vector2(width, height);
        }
    }
}
