using UnityEngine;
using UnityEngine.UI;
using TMPro; // Если используешь обычный Text, замени на using UnityEngine.UI;

public class InventoryItemDetails : MonoBehaviour
{
    public static InventoryItemDetails Instance;

    [Header("UI References")]
    public GameObject detailsPanel; 
    public Image itemIcon;
    // Используй TextMeshProUGUI или Text в зависимости от того, что у тебя в UI
    public TextMeshProUGUI itemNameText; 
    public TextMeshProUGUI itemDescriptionText; 
    
    public Button findButton; 
    public Button closeBackgroundButton; // Прозрачная кнопка на фоне для закрытия

    private ItemData currentItem;

    private void Awake()
    {
        Instance = this;
        if (detailsPanel != null) detailsPanel.SetActive(false);
        
        if (findButton != null) findButton.onClick.AddListener(OnFindButtonClicked);
        if (closeBackgroundButton != null) closeBackgroundButton.onClick.AddListener(ClosePanel);
    }

    public void SelectItem(ItemData item)
    {
        if (item == null) return;

        currentItem = item;
        
        // Открываем панель
        if (detailsPanel != null) detailsPanel.SetActive(true);
        
        // Заполняем данными
        if (itemIcon != null) itemIcon.sprite = item.Icon;
        if (itemNameText != null) itemNameText.text = item.GetName();
        if (itemDescriptionText != null) itemDescriptionText.text = item.Description;

        UpdateFindButtonVisibility(item);
    }

    private void UpdateFindButtonVisibility(ItemData item)
    {
        if (findButton == null) return;

        // ПРОСТАЯ И НАДЕЖНАЯ ЛОГИКА:
        // Если это Ресурс — кнопку показываем.
        // (Если его нет на карте, ResourceFinder сам скажет об этом игроку)
        bool showButton = (item.itemType == ItemType.Resource);

        findButton.gameObject.SetActive(showButton);
    }

    public void OnFindButtonClicked()
    {
        if (currentItem != null && ResourceFinder.Instance != null)
        {
            // Закрываем инвентарь, чтобы видеть, куда летит камера
            if (UIManager.Instance != null) UIManager.Instance.ToggleInventory();
            
            // Запускаем поиск
            ResourceFinder.Instance.FindResourceNodes(currentItem.ItemName);
            
            // Закрываем панель деталей
            ClosePanel();
        }
    }

    public void ClosePanel()
    {
        if (detailsPanel != null) detailsPanel.SetActive(false);
    }
}