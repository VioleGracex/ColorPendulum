using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Camera Override")]
    [SerializeField] Camera overrideCamera;
    [Header("Tube Animation")]
    [SerializeField] GameObject tubesprefab;
    // tubeDropYOffset removed; use tubeSpawnOffset/tubeDropOffset instead
    [SerializeField] float tubeDropDuration = 0.5f;
    [SerializeField] float tubeDropStagger = 0.1f;
    [SerializeField] float tubeLandShakeStrength = 0.3f;
    [SerializeField] float tubeLandShakeDuration = 0.2f;
    GameObject spawnedTubes;
    public static UIManager Instance;

    [Header("Hole Animation")]
    [SerializeField] GameObject holeObject;

    [Header("Panels")]
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject gamePanel;
    [SerializeField] GameObject gameOverPanel;

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

    private void Awake()
    {
        Instance = this;

        startButton?.onClick.AddListener(() => GameManager.Instance.OnStartButtonClicked());
        replayButton?.onClick.AddListener(() => GameManager.Instance.OnReplayButtonClicked());
        menuButton?.onClick.AddListener(() => GameManager.Instance.OnMenuButtonClicked());
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (gamePanel != null) gamePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Animate tubes and start button dropping from above
        Camera cam = overrideCamera != null ? overrideCamera : Camera.main;
        if (cam != null)
        {
            // Tubes animation
            if (spawnedTubes != null)
            {
                float camHeight = 2f * cam.orthographicSize;
                float yTarget = cam.transform.position.y - cam.orthographicSize + tubeDropOffset.y;
                float yStart = cam.transform.position.y + camHeight / 2f + tubeSpawnOffset.y;
                Vector3 startPos = new Vector3(spawnedTubes.transform.position.x, yStart, spawnedTubes.transform.position.z);
                spawnedTubes.transform.position = startPos;
                spawnedTubes.transform.DOMoveY(yTarget, tubeDropDuration).SetEase(Ease.OutBounce);
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

    public void AnimateStartButtonAndTubes(Action onComplete)
    {
        // Animate start button falling
        startButton.transform.DOMoveY(-Screen.height, 0.5f).OnComplete(() =>
        {
            mainMenuPanel?.SetActive(false);
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

        // Instantiate the tubes prefab only once
        if (spawnedTubes != null)
        {
            Destroy(spawnedTubes);
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


    public void ShowGameUI(int score)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        UpdateScore(score);
    }

    public void UpdateScore(int score)
    {
        if(scoreText == null) return;
        scoreText.text = $"Score: {score}";
    }

    public void UpdateNextBalls(BallColor[] nextColors)
    {

    }

    public void ShowGameOver(int score)
    {
        if (gamePanel != null) gamePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (finalScoreText != null)
            finalScoreText.text = $"Final Score: {score}";
    }
}