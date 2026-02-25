using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO; // Обязательно для работы с файлами (File, Path)

public class MainMenuController : MonoBehaviour
{
    public string tutorialSceneName = "Scene_00_Tutorial_Past";
    
    [Header("UI")]
    public Button continueButton;

    private void Start()
    {
        bool hasSave = false;

        // 1. Проверяем наличие физического файла сохранения
        string savePath = Path.Combine(Application.persistentDataPath, "gamesave.json");
        if (File.Exists(savePath))
        {
            hasSave = true;
        }

        // 2. Или проверяем, начал ли игрок туториал (шаг > 0)
        if (PlayerPrefs.GetInt("TutorialStep", 0) > 0)
        {
            hasSave = true;
        }

        // Включаем или выключаем кнопку в зависимости от того, нашли ли мы сохранение
        if (continueButton != null)
        {
            continueButton.interactable = hasSave;
        }
    }

    public void OnNewGameClicked()
    {
        // 1. Очищаем старую системную память
        PlayerPrefs.DeleteAll(); 
        
        // 2. ОСТАВЛЯЕМ ЗАПИСКУ ДЛЯ ТУТОРИАЛА: "Удали старые файлы!"
        PlayerPrefs.SetInt("NeedWipeSave", 1); 
        PlayerPrefs.Save();

        // 3. Изгоняем старого призрака туториала (если он висит в памяти)
        if (TutorialManager.Instance != null)
        {
            Destroy(TutorialManager.Instance.gameObject);
            TutorialManager.Instance = null;
        }

        // 4. Загружаем туториал. Там нас встретит нормальный GameManager.
        SceneManager.LoadScene(tutorialSceneName);
    }

    public void OnContinueClicked()
    {
        // Читаем последнюю сцену (если произойдет сбой, загрузит туториал)
        string sceneToLoad = PlayerPrefs.GetString("LastSavedScene", tutorialSceneName);
        SceneManager.LoadScene(sceneToLoad);
    }
    
    public void OnExitClicked()
    {
        Application.Quit();
        Debug.Log("Выход из игры");
    }
}