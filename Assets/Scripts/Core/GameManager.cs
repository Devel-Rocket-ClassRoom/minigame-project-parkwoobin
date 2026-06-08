using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 현재 게임 상태가 프롤로그인지, 플레이 중인지, 일시정지인지, 스테이지 클리어인지, 게임 오버인지 관리하는 스크립트
/// </summary>

public class GameManager : MonoBehaviour
{
    // 다른 스크립트에서 GameManager.Instance로 접근 가능하도록 싱글톤 패턴 구현
    public static GameManager Instance { get; private set; }

    // 게임 상태 열거형 정의
    public enum GameState { Prologue, Playing, Paused, StageClear, GameOver }

    public GameState CurrentState { get; private set; }

    public static UnityAction<GameState> OnStateChanged;    // 다른 스크립트에서 게임 상태 변경 이벤트를 구독해서 상태 변화를 알 수 있음

    void Awake()
    {
        Application.targetFrameRate = 60;
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad는 루트 오브젝트에만 동작 — 자식이면 자동으로 루트로 이동
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ChangeState(GameState.Playing);
    }

    public void ChangeState(GameState next)
    {
        CurrentState = next;
        OnStateChanged?.Invoke(next);   // 상태 변경 이벤트 호출
    }

    public void StartGame() => ChangeState(GameState.Playing);
    public void PauseGame() => ChangeState(GameState.Paused);
    public void ResumeGame() => ChangeState(GameState.Playing);
    public void StageClear() => ChangeState(GameState.StageClear);
    public void GameOver() => ChangeState(GameState.GameOver);

    public bool IsPlaying => CurrentState == GameState.Playing; // 현재 게임이 플레이 중인지 여부
}
