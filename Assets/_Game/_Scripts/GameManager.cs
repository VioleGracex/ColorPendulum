using UnityEngine;

public enum GameState
{
    MainMenu,
    AnimatingIn,
    Playing,
    GameOver,
    Cleaning,
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Managers")]
    public BallSpawner ballSpawner;
    public TubeManager tubeManager;
    public UIManager uiManager;

    [Header("Game State")]
    public GameState state = GameState.MainMenu;
    int score = 0;

    [Header("Hearts System")]
    public int maxHearts = 4;
    public int currentHearts = 4;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        uiManager?.ShowMainMenu();
        state = GameState.MainMenu;
    }

    public void OnStartButtonClicked()
    {
        state = GameState.AnimatingIn;
        uiManager.AnimateStartButtonAndTubes(() =>
        {
            StartGame();
        });
    }

    public void StartGame()
    {
        // Reset all game state
        score = 0;
        currentHearts = maxHearts;

    // Reset UI
        uiManager?.ShowGameUI(score);
        uiManager?.UpdateHearts(currentHearts, maxHearts);

        // Reset grid and balls
        tubeManager?.ClearAllBalls();
        state = GameState.Playing;
        // Reset ball spawner
        ballSpawner?.InitAndSpawnFirst();
    }
    public void AddHeart(int amount = 1)
    {
        currentHearts = Mathf.Min(currentHearts + amount, maxHearts);
        uiManager?.UpdateHearts(currentHearts, maxHearts);
    }

    public void SubtractHeart(int amount = 1)
    {
        currentHearts = Mathf.Max(currentHearts - amount, 0);
        uiManager?.UpdateHearts(currentHearts, maxHearts);
        if (currentHearts <= 0)
        {
            GameOver();
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
        uiManager?.UpdateScore(score);
    }

    public void GameOver()
    {
        state = GameState.GameOver;
        Debug.Log("[GameManager] Game Over ! Final Score: " + score);
        uiManager?.ShowGameOver(score);
    }

    public void OnReplayButtonClicked()
    {
        StartGame();
    }

    public void OnMenuButtonClicked()
    {
        state = GameState.MainMenu;
        uiManager?.ShowMainMenu();
        tubeManager?.ClearAllBalls();
    }
}