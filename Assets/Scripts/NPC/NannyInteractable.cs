using UnityEngine;
using UnityEngine.UI;

public class NannyInteractable : MonoBehaviour
{
    [Header("UI Облачко")]
    public GameObject chatBubbleUI; 
    public Button chatBubbleButton; 

    [Header("Ссылка на контроллер")]
    public NannyMenuController menuController;

    [Header("Настройки сцены")]
    public bool disableInTutorial = false; // <-- НОВАЯ ГАЛОЧКА

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
        // Если стоит галочка туториала - просто игнорируем приближение игрока
        if (disableInTutorial) return; 

        if (other.CompareTag("Player"))
        {
            if (chatBubbleUI != null) chatBubbleUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (disableInTutorial) return; // Тут тоже игнорируем

        if (other.CompareTag("Player"))
        {
            if (chatBubbleUI != null) chatBubbleUI.SetActive(false);
            
            if (menuController != null && menuController.nannyMenuPanel.activeSelf)
            {
                menuController.CloseMenu();
            }
        }
    }

    private void OnBubbleTapped()
    {
        if (disableInTutorial) return; // Защита от случайных кликов

        if (menuController != null)
        {
            chatBubbleUI.SetActive(false); 
            menuController.OpenMenu();
        }
    }
}