using UnityEngine;
using UnityEngine.UI;
using System.Collections; // <-- Обязательно добавь для работы корутин

public class NannyInteractable : MonoBehaviour
{
    [Header("UI Облачко")]
    public GameObject chatBubbleUI; 
    public Button chatBubbleButton; 

    [Header("Ссылка на контроллер")]
    public NannyMenuController menuController;

    [Header("Настройки сцены")]
    public bool disableInTutorial = false; 

    // === НОВЫЙ ФЛАГ ===
    private bool isPlayerNear = false; 

    private void Start()
    {
        if (chatBubbleUI != null) chatBubbleUI.SetActive(false);
        
        if (chatBubbleButton != null)
        {
            chatBubbleButton.onClick.AddListener(OnBubbleTapped);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (disableInTutorial) return; 

        if (other.CompareTag("Player"))
        {
            isPlayerNear = true; // Игрок вошел в зону
            if (chatBubbleUI != null) chatBubbleUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (disableInTutorial) return; 

        if (other.CompareTag("Player"))
        {
            isPlayerNear = false; // Игрок покинул зону
            if (chatBubbleUI != null) chatBubbleUI.SetActive(false);
            
            if (menuController != null && menuController.nannyMenuPanel.activeSelf)
            {
                menuController.CloseMenu();
            }
        }
    }

    private void OnBubbleTapped()
    {
        if (disableInTutorial) return; 

        if (menuController != null)
        {
            chatBubbleUI.SetActive(false); 
            menuController.OpenMenu();
            
            // Запускаем режим "ожидания" закрытия меню
            StartCoroutine(WaitForMenuClose()); 
        }
    }

    // === НОВАЯ КОРУТИНА ===
    private IEnumerator WaitForMenuClose()
    {
        // Крутим невидимый цикл, пока открыто ХОТЯ БЫ ОДНО из окон Няни
        while (menuController.nannyMenuPanel.activeInHierarchy || 
               (menuController.exitConfirmPanel != null && menuController.exitConfirmPanel.activeInHierarchy) || 
               (menuController.dailyRewardPanel != null && menuController.dailyRewardPanel.activeInHierarchy))
        {
            yield return null; // Ждем следующего кадра
        }

        // Как только все меню закрылись: проверяем, стоит ли игрок всё ещё рядом
        if (isPlayerNear && chatBubbleUI != null)
        {
            chatBubbleUI.SetActive(true); // Возвращаем облачко!
        }
    }
}