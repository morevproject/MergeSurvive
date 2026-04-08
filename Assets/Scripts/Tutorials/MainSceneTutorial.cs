using UnityEngine;
using System.Collections.Generic;

public class MainSceneTutorial : MonoBehaviour
{
    [Header("Диалоги")]
    public List<DialogueNode> struggleDialogue; 
    public List<DialogueNode> successDialogue;  

    private int failedMergeCount = 0;
    private bool tutorialTriggered = false;

    private void Start()
    {
        if (PlayerPrefs.GetInt("PremiumUnlocked", 0) == 1)
        {
            tutorialTriggered = true;
            Debug.Log("[MainTutorial] Туториал уже пройден ранее. Включаем UI тихо.");
            
            // Сначала включаем лампочки через HintManager (теперь там безопасная проверка)
            if (HintManager.Instance != null) HintManager.Instance.Tutorial_ShowBulbsUI();
            
            // Включаем плашку алмазов напрямую, без вызова методов туториала
            if (CurrencyManager.Instance != null && CurrencyManager.Instance.gemsText != null)
            {
                CurrencyManager.Instance.gemsText.transform.parent.gameObject.SetActive(true);
            }
        }
        else
        {
            Debug.Log("[MainTutorial] Туториал готов к запуску! Ждем действий игрока.");
            CraftingManager.OnRecipeDiscovered += CheckRecipeCount;
            CraftingManager.OnMergeFailed += HandleMergeFailed;
        }
    }

    private void OnDestroy()
    {
        CraftingManager.OnRecipeDiscovered -= CheckRecipeCount;
        CraftingManager.OnMergeFailed -= HandleMergeFailed;
    }

    private void HandleMergeFailed()
    {
        if (tutorialTriggered) return;

        failedMergeCount++;
        Debug.Log($"[MainTutorial] Неудачных попыток крафта: {failedMergeCount}/3");
        
        if (failedMergeCount >= 3)
        {
            Debug.Log("[MainTutorial] ЗАПУСК ТУТОРИАЛА: Серия неудач!");
            StartTutorial(struggleDialogue);
        }
    }

    private void CheckRecipeCount(Recipe newRecipe)
    {
        if (tutorialTriggered) return;

        failedMergeCount = 0; 
        
        int recipesCount = CraftingManager.Instance.GetDiscoveredRecipes().Count;
        Debug.Log($"[MainTutorial] Открыт новый рецепт! Всего открыто: {recipesCount}/5");

        if (recipesCount >= 5)
        {
            Debug.Log("[MainTutorial] ЗАПУСК ТУТОРИАЛА: 5 рецептов открыто!");
            StartTutorial(successDialogue);
        }
    }

    private void StartTutorial(List<DialogueNode> dialogueToPlay)
    {
        tutorialTriggered = true;

        // 1. Проверяем, есть ли менеджер на сцене
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("<color=red>[MainTutorial] ОШИБКА: На сцене Main нет объекта DialogueManager!</color>");
            return;
        }

        // 2. Проверяем, не забыл ли ты написать текст в Инспекторе
        if (dialogueToPlay == null || dialogueToPlay.Count == 0)
        {
            Debug.LogError("<color=red>[MainTutorial] ОШИБКА: Список диалогов пуст! Добавь реплики в Инспекторе.</color>");
            return;
        }

        // Если всё окей - запускаем!
        DialogueManager.Instance.StartDialogue(dialogueToPlay);
    }

    // === МАГИЧЕСКАЯ КНОПКА ДЛЯ ДЕБАГА ===
    [ContextMenu("Сбросить сохранение туториала Няни")]
    public void ResetTutorialSave()
    {
        PlayerPrefs.SetInt("PremiumUnlocked", 0);
        PlayerPrefs.Save();
        tutorialTriggered = false;
        failedMergeCount = 0;
        Debug.LogWarning("Сохранение сброшено! Перезапустите сцену для проверки.");
    }
}