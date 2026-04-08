using UnityEngine;
using UnityEngine.UI;

public class NannyInteractable : MonoBehaviour
{
    [Header("UI Облачко")]
    public GameObject chatBubbleUI; // Сюда перетащим Canvas облачка "..."
    public Button chatBubbleButton; // Сама кнопка внутри облачка

    [Header("Ссылка на контроллер")]
    public NannyMenuController menuController;

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
        // Игрок подошел - показываем облачко
        if (other.CompareTag("Player"))
        {
            if (chatBubbleUI != null) chatBubbleUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Игрок отошел - прячем облачко
        if (other.CompareTag("Player"))
        {
            if (chatBubbleUI != null) chatBubbleUI.SetActive(false);
            
            // Если игрок убежал с открытым меню, принудительно закрываем его
            if (menuController != null && menuController.nannyMenuPanel.activeSelf)
            {
                menuController.CloseMenu();
            }
        }
    }

    private void OnBubbleTapped()
    {
        if (menuController != null)
        {
            chatBubbleUI.SetActive(false); // Прячем облачко, чтобы не мешало
            menuController.OpenMenu();
        }
    }
}