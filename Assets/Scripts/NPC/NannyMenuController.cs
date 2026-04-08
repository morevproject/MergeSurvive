using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class NannyMenuController : MonoBehaviour
{
    [Header("Окна UI")]
    public GameObject nannyMenuPanel;
    public GameObject exitConfirmPanel;
    public GameObject dailyRewardPanel; // Панель дейликов (Panel_DailyRewards)

    [Header("Кнопки Меню Няни")]
    public Button btnDaily;
    public Button btnExit;
    public Button btnClose;

    [Header("Кнопки Выхода")]
    public Button btnConfirmExitYes;
    public Button btnConfirmExitNo;

    [Header("Сюжет (Дейлик уже собран)")]
    public List<DialogueNode> dailyWaitDialogue;

    private void Start()
    {
        // Подписываем все кнопки
        if (btnDaily != null) btnDaily.onClick.AddListener(OnDailyClicked);
        if (btnExit != null) btnExit.onClick.AddListener(OnExitClicked);
        if (btnClose != null) btnClose.onClick.AddListener(CloseMenu);

        if (btnConfirmExitYes != null) btnConfirmExitYes.onClick.AddListener(ConfirmExit);
        if (btnConfirmExitNo != null) btnConfirmExitNo.onClick.AddListener(CancelExit);

        // Прячем всё на старте
        nannyMenuPanel.SetActive(false);
        exitConfirmPanel.SetActive(false);
    }

    public void OpenMenu()
    {
        nannyMenuPanel.SetActive(true);
        exitConfirmPanel.SetActive(false);
        
        // Замораживаем игру через наш UIManager
        if (UIManager.Instance != null) UIManager.Instance.SetMenuState(true);
    }

    public void CloseMenu()
    {
        nannyMenuPanel.SetActive(false);
        exitConfirmPanel.SetActive(false);
        
        // Размораживаем игру
        if (UIManager.Instance != null) UIManager.Instance.SetMenuState(false);
    }

    // --- ЛОГИКА ДЕЙЛИКОВ ---
    private void OnDailyClicked()
    {
        // 1. Прячем меню няни
        nannyMenuPanel.SetActive(false);

        // 2. Показываем окно дейликов
        if (dailyRewardPanel != null) dailyRewardPanel.SetActive(true);

        // 3. Запускаем диалог и используем магию коллбэков!
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(dailyWaitDialogue, () =>
            {
                // Этот блок сработает только когда игрок тапнет по панели диалога в самом конце!
                if (dailyRewardPanel != null) dailyRewardPanel.SetActive(false); // Прячем дейлики
                nannyMenuPanel.SetActive(true); // Возвращаем меню Няни
            });
        }
    }

    // --- ЛОГИКА ВЫХОДА ---
    private void OnExitClicked()
    {
        exitConfirmPanel.SetActive(true);
        nannyMenuPanel.SetActive(false);
    }

    private void CancelExit()
    {
        exitConfirmPanel.SetActive(false);
        nannyMenuPanel.SetActive(true);
    }

    private void ConfirmExit()
    {
        Debug.Log("Выход из игры...");
        
        // Эта команда закроет игру на реальном устройстве
        Application.Quit(); 

        // А этот хитрый блок остановит режим Play в самом редакторе Unity!
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}