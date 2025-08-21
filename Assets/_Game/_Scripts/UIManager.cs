using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject gamePanel;
    public GameObject gameOverPanel;

    [Header("Buttons")]
    public Button startButton;
    public Button replayButton;
    public Button menuButton;

    [Header("Text")]
    public Text scoreText;
    public Text finalScoreText;

    [Header("Next Balls UI")]
    public Image[] nextBallImages;
    public Sprite[] colorSprites;

    private void Awake()
    {
        Instance = this;

        startButton.onClick.AddListener(() => GameManager.Instance.OnStartButtonClicked());
        replayButton.onClick.AddListener(() => GameManager.Instance.OnReplayButtonClicked());
        menuButton.onClick.AddListener(() => GameManager.Instance.OnMenuButtonClicked());
    }

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        gamePanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }

    public void AnimateStartButtonAndTubes(Action onComplete)
    {
        // Example DOTween animation for start button and tubes
        startButton.transform.DOMoveY(-Screen.height, 0.5f).OnComplete(() =>
        {
            mainMenuPanel?.SetActive(false);
            onComplete?.Invoke();
        });
        // Also animate tube sprites falling in
        // ... (implement as needed)
    }

    public void ShowGameUI(int score)
    {
        mainMenuPanel?.SetActive(false);
        gamePanel?.SetActive(true);
        gameOverPanel?.SetActive(false);
        UpdateScore(score);
    }

    public void UpdateScore(int score)
    {
        if(scoreText == null) return;
        scoreText.text = $"Score: {score}";
    }

    public void UpdateNextBalls(BallColor[] nextColors)
    {
        if (nextColors.Length <= 0) return;
        for (int i = 0; i < nextBallImages.Length; i++)
        {
            if (i < nextColors.Length)
                nextBallImages[i].sprite = colorSprites[(int)nextColors[i]];
            else
                nextBallImages[i].enabled = false;
        }
    }

    public void ShowGameOver(int score)
    {
        gamePanel?.SetActive(false);
        gameOverPanel?.SetActive(true);
        finalScoreText.text = $"Final Score: {score}";
    }
}