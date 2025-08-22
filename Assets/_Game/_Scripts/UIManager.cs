using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;
using TMPro;

public class UIManager : MonoBehaviour
{
#region Fields
    [Header("Hearts UI")]
    [SerializeField] Image[] heartImages; // Assign heart images in inspector (optional)

    [Header("Camera Override")]
    [SerializeField] Camera overrideCamera;

    [Header("Tube Animation")]
    [SerializeField] GameObject tubesprefab;
    [SerializeField] private float tubeDropDuration = 0.5f;
    [SerializeField] private float tubeDropStagger = 0.1f;
    [SerializeField] private float tubeLandShakeStrength = 0.3f;
    [SerializeField] private float tubeLandShakeDuration = 0.2f;
    private GameObject spawnedTubes;
    public static UIManager Instance;

    [Header("Hole Animation")]
    [SerializeField] GameObject holeObject;

    [Header("Panels")]
    [SerializeField] public UIPanels panels;

    [Header("Buttons")]
    [SerializeField] Button startButton;
    [SerializeField] Button replayButton;
    [SerializeField] Button menuButton;

    [Header("Text")]
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI finalScoreText;

    [Header("Tube Offsets")]
    [SerializeField] Vector2 tubeSpawnOffset = Vector2.zero;
    [SerializeField] Vector2 tubeDropOffset = Vector2.zero;

    [Header("Next Balls UI")]
    [SerializeField] private Image[] nextBallImages; // Assign 3 UI Images in Inspector
#endregion

#region UI Methods
    public void UpdateScore(int score)
    {
        if (scoreText == null) return;
        scoreText.text = $"Score: {score}";
    }

    public void UpdateHearts(int current, int max)
    {
        if (heartImages != null && heartImages.Length > 0)
        {
            for (int i = 0; i < heartImages.Length; i++)
            {
                if (i < max)
                {
                    heartImages[i].gameObject.SetActive(true);
                    heartImages[i].color = i < current ? Color.white : new Color(1,1,1,0.2f); // faded if lost
                }
                else
                {
                    heartImages[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            Debug.Log($"[UIManager] Hearts: {current}/{max}");
        }
    }

    public void UpdateNextBalls(BallColor[] nextColors)
    {
        // To Do : fix or remove animation
        if (nextBallImages == null) return;
        for (int i = 0; i < nextBallImages.Length; i++)
        {
            if (i < nextColors.Length && nextColors[i] != BallColor.None)
            {
                nextBallImages[i].gameObject.SetActive(true);
                nextBallImages[i].color = nextColors[i].ToColor();

                // Animate: move in from right, rotate, scale up
                RectTransform rt = nextBallImages[i].rectTransform;
                rt.DOKill(); // Stop any previous tweens
                Vector3 startPos = rt.anchoredPosition + new Vector2(200f, 0f); // 200px to the right
                rt.anchoredPosition = startPos;
                rt.localRotation = Quaternion.Euler(0, 0, 90f);
                rt.localScale = Vector3.one * 0.5f;
                float delay = 0.05f * i;
                rt.DOAnchorPos(rt.anchoredPosition - new Vector2(200f, 0f), 0.35f).SetEase(Ease.OutBack).SetDelay(delay);
                rt.DORotate(Vector3.zero, 0.35f).SetEase(Ease.OutBack).SetDelay(delay);
                rt.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetDelay(delay);
            }
            else
            {
                nextBallImages[i].gameObject.SetActive(false);
            }
        }
    }

    public void ShowGameUI(int score)
    {
        ShowOnlyPanel(panels.gamePanel);
        UpdateScore(score);
    }

    public void ShowGameOver(int score)
    {
        ShowOnlyPanel(panels.gameOverPanel);
        if (finalScoreText != null)
            finalScoreText.text = $"Final Score: {score}";
    }

    // Call this to enable only the specified panel and disable all others
    public void ShowOnlyPanel(GameObject panelToShow)
    {
        if (panels.mainMenuPanel != null) panels.mainMenuPanel.SetActive(false);
        if (panels.gamePanel != null) panels.gamePanel.SetActive(false);
        if (panels.gameOverPanel != null) panels.gameOverPanel.SetActive(false);
        if (panelToShow != null) panelToShow.SetActive(true);
    }
#endregion

#region Unity Events

    private void Awake()
    {
        Instance = this;
        startButton?.onClick.AddListener(() => GameManager.Instance.OnStartButtonClicked());
        replayButton?.onClick.AddListener(() => GameManager.Instance.OnReplayButtonClicked());
        menuButton?.onClick.AddListener(() => GameManager.Instance.OnMenuButtonClicked());
    }
#endregion

#region UI Panel Methods
    public void ShowMainMenu()
    {
        ShowOnlyPanel(panels.mainMenuPanel);

        // Animate tubes and start button dropping from above
        Camera cam = overrideCamera != null ? overrideCamera : Camera.main;
        if (cam != null)
        {
            // Tubes animation: if spawnedTubes exists, animate it falling and destroy
            if (spawnedTubes != null)
            {
                float camHeight = 2f * cam.orthographicSize;
                float yFall = cam.transform.position.y - cam.orthographicSize - 3f; // fall below screen
                spawnedTubes.transform.DOScaleY(0.7f, 0.4f).SetEase(Ease.InBack);
                spawnedTubes.transform.DOMoveY(yFall, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    Destroy(spawnedTubes);
                    spawnedTubes = null;
                });
            }

            // Start button animation
            RectTransform startBtnRect = startButton.transform as RectTransform;
            if (startBtnRect != null)
            {
                float btnYTarget = 0f;
                float btnYStart = Screen.height + 200f; // 200px above screen
                Vector3 btnStartPos = new Vector3(startBtnRect.anchoredPosition.x, btnYStart, 0f);
                startBtnRect.anchoredPosition = btnStartPos;
                startBtnRect.DOAnchorPosY(btnYTarget, tubeDropDuration).SetEase(Ease.OutBounce);
            }

            // Hide hole with scale animation if it exists
            if (holeObject != null && holeObject.activeSelf)
            {
                holeObject.SetActive(true);
                holeObject.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    holeObject.SetActive(false);
                });
            }
        }
    }
#endregion

#region Animation Methods
    public void AnimateStartButtonAndTubes(Action onComplete)
    {
        // Animate start button falling
        startButton.transform.DOMoveY(-Screen.height, 0.5f).OnComplete(() =>
        {
            ShowOnlyPanel(null); // Hide all panels
            // Spawn and animate the single tubes object
            SpawnAndDropTubes(onComplete);
        });
    }

    private void SpawnAndDropTubes(Action onComplete)
    {
        Camera cam = overrideCamera != null ? overrideCamera : Camera.main;
        if (!cam) { onComplete?.Invoke(); return; }

        // Calculate spawn and target positions for the whole tubes object
        float camHeight = 2f * cam.orthographicSize;
        float yTarget = cam.transform.position.y - cam.orthographicSize + tubeDropOffset.y;
        float yStart = cam.transform.position.y + camHeight / 2f + tubeSpawnOffset.y;
        Vector3 dropPos = new Vector3(
            cam.transform.position.x + tubeDropOffset.x,
            yTarget,
            spawnedTubes != null ? spawnedTubes.transform.position.z : 0f
        );
        Vector3 spawnPos = new Vector3(
            cam.transform.position.x + tubeSpawnOffset.x,
            yStart,
            spawnedTubes != null ? spawnedTubes.transform.position.z : 0f
        );

        // If tubes already exist, animate them falling off screen and destroy after animation
        if (spawnedTubes != null)
        {
            float yFall = cam.transform.position.y - cam.orthographicSize - 3f; // fall below screen
            spawnedTubes.transform.DOScaleY(0.7f, 0.4f).SetEase(Ease.InBack);
            spawnedTubes.transform.DOMoveY(yFall, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
            {
                Destroy(spawnedTubes);
                spawnedTubes = null;
            });
        }
        spawnedTubes = Instantiate(tubesprefab, spawnPos, Quaternion.identity);

        // Animate the tubes object dropping down
        spawnedTubes.transform.DOMoveY(yTarget, tubeDropDuration).SetEase(Ease.OutBounce).OnComplete(() =>
        {
            CameraShake(cam, tubeLandShakeStrength, tubeLandShakeDuration);

            // Animate hole pop-in after tube lands
            if (holeObject != null)
            {
                holeObject.SetActive(true);
                holeObject.transform.localScale = Vector3.zero;
                holeObject.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            }

            onComplete?.Invoke();
        });
    }

    // Draw gizmo for tube drop target in the editor
    private void OnDrawGizmos()
    {
        Camera cam = overrideCamera != null ? overrideCamera : Camera.main;
        if (cam == null) return;
        float camHeight = 2f * cam.orthographicSize;
        float yTarget = cam.transform.position.y - cam.orthographicSize + tubeDropOffset.y;
        float yStart = cam.transform.position.y + camHeight / 2f + tubeSpawnOffset.y;
        Vector3 dropPos = new Vector3(
            cam.transform.position.x + tubeDropOffset.x,
            yTarget,
            0f
        );
        Vector3 spawnPos = new Vector3(
            cam.transform.position.x + tubeSpawnOffset.x,
            yStart,
            0f
        );
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(dropPos, 0.2f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(spawnPos, 0.2f);
    }

    private void CameraShake(Camera cam, float strength, float duration)
    {
        // Simple shake using DOTween
        cam.transform.DOComplete();
        cam.transform.DOShakePosition(duration, strength, 20, 90, false, true);
    }
#endregion
}