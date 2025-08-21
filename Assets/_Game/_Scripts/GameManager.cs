using UnityEngine;

public enum GameState
{
    MainMenu,
    AnimatingIn,
    Playing,
    GameOver
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
    public int score = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        uiManager.ShowMainMenu();
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
        state = GameState.Playing;
        score = 0;
        uiManager.ShowGameUI(score);
        tubeManager.ResetTubes();
        ballSpawner.InitAndSpawnFirst();
    }

    public void AddScore(int amount)
    {
        score += amount;
        uiManager.UpdateScore(score);
    }

    public void GameOver()
    {
        state = GameState.GameOver;
        uiManager.ShowGameOver(score);
    }

    public void OnReplayButtonClicked()
    {
        StartGame();
    }

    public void OnMenuButtonClicked()
    {
        state = GameState.MainMenu;
        uiManager.ShowMainMenu();
        tubeManager.ResetTubes();
    }
}