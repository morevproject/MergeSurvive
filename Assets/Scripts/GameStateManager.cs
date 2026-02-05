using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    // Вернули старые названия + добавили Cutscene
    public enum GameState
    {
        Playing,    // (Было Gameplay) Обычная игра
        MenuOpen,   // (Было UI) Открыт инвентарь/верстак
        Cutscene    // НОВОЕ: Для режима поиска/кинематографии
    }

    public GameState CurrentState { get; private set; }

    // Вернули старое событие, чтобы UIManager не ломался
    public System.Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetState(GameState.Playing);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;

        // Логика курсора и времени
        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case GameState.MenuOpen:
                Time.timeScale = 1f; 
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case GameState.Cutscene:
                Time.timeScale = 1f;
                // В катсцене прячем курсор (чтобы не мешал смотреть)
                // Если хочешь оставить видным, поставь visible = true
                Cursor.lockState = CursorLockMode.Locked; 
                Cursor.visible = false;
                break;
        }

        Debug.Log($"GameState changed to: {newState}");

        // Оповещаем подписчиков (твои старые скрипты это слушают)
        OnGameStateChanged?.Invoke(newState);
    }
}