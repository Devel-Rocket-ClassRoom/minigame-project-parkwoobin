using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Prologue, Playing, Paused, StageClear, GameOver }

    public GameState CurrentState { get; private set; }

    public static UnityAction<GameState> OnStateChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ChangeState(GameState.Playing);
    }

    public void ChangeState(GameState next)
    {
        CurrentState = next;
        OnStateChanged?.Invoke(next);
    }

    public void StartGame()  => ChangeState(GameState.Playing);
    public void PauseGame()  => ChangeState(GameState.Paused);
    public void ResumeGame() => ChangeState(GameState.Playing);
    public void StageClear() => ChangeState(GameState.StageClear);
    public void GameOver()   => ChangeState(GameState.GameOver);

    public bool IsPlaying => CurrentState == GameState.Playing;
}
