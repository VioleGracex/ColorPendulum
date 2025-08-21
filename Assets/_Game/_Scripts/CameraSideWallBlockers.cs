using UnityEngine;

/// <summary>
/// Spawns invisible BoxCollider2D walls at the left and right edges of the specified camera's view,
/// blocking objects from escaping horizontally.
/// </summary>
public class CameraSideWallBlockers : MonoBehaviour
{
    [Header("Camera & Wall Settings")]
    [SerializeField] Camera targetCamera;
    [SerializeField] float wallThickness = 0.5f; // Thickness of the invisible wall
    [SerializeField] float wallVerticalMargin = 1.0f; // Extra height above/below the camera

    private GameObject leftWall;
    private GameObject rightWall;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        CreateOrUpdateWalls();
    }

#if UNITY_EDITOR
    void Update()
    {
        // In Editor, auto-update if camera/aspect changes
        if (!Application.isPlaying)
            CreateOrUpdateWalls();
    }
#endif

    void CreateOrUpdateWalls()
    {
        if (targetCamera == null) return;

        float camHeight = targetCamera.orthographicSize * 2f;
        float camWidth = camHeight * targetCamera.aspect;
        Vector3 camPos = targetCamera.transform.position;

        // Wall height with margin
        float wallHeight = camHeight + wallVerticalMargin * 2f;

        // --- LEFT WALL ---
        if (leftWall == null)
        {
            leftWall = new GameObject("LeftWall");
            leftWall.transform.parent = this.transform;
            var col = leftWall.AddComponent<BoxCollider2D>();
            col.isTrigger = false;
        }
        leftWall.transform.position = new Vector3(
            camPos.x - camWidth / 2f - wallThickness / 2f,
            camPos.y,
            0f
        );
        var leftCollider = leftWall.GetComponent<BoxCollider2D>();
        leftCollider.size = new Vector2(wallThickness, wallHeight);

        // --- RIGHT WALL ---
        if (rightWall == null)
        {
            rightWall = new GameObject("RightWall");
            rightWall.transform.parent = this.transform;
            var col = rightWall.AddComponent<BoxCollider2D>();
            col.isTrigger = false;
        }
        rightWall.transform.position = new Vector3(
            camPos.x + camWidth / 2f + wallThickness / 2f,
            camPos.y,
            0f
        );
        var rightCollider = rightWall.GetComponent<BoxCollider2D>();
        rightCollider.size = new Vector2(wallThickness, wallHeight);
    }
}