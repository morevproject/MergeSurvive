using UnityEngine;
using UnityEngine.UI;

public class ResourceSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image bgImage;      // Сюда перетащим объект BG (подложка)
    public Image iconImage;    // Сюда перетащим объект Icon (сам гриб/камень)
    public Text amountText;    

    [Header("Backgrounds (По порядку ItemType)")]
    public Sprite[] categoryBackgrounds; // Массив цветных подложек
    public Sprite defaultBackground;     // Фон по умолчанию (если пусто)

    [Header("Data")]
    public string resourceName; 
    public int currentAmount;
    public ItemData itemData; 

    private bool shouldPulse = false; 
    private Button buttonComponent; 

    private void Awake()
    {
        buttonComponent = GetComponent<Button>();
        
        if (buttonComponent != null)
        {
            buttonComponent.onClick.RemoveAllListeners(); 
            buttonComponent.onClick.AddListener(OnSlotClicked);
        }
    }

    public void OnSlotClicked()
    {
        if (itemData != null && InventoryItemDetails.Instance != null)
        {
            InventoryItemDetails.Instance.SelectItem(itemData);
        }
    }

    private void Update()
    {
        // Пульсация теперь работает на подложке (BG) - выглядит круче!
        if (shouldPulse && bgImage != null)
        {
            float speed = 8.0f; 
            float minScale = 0.9f;
            float maxScale = 1.1f;
            
            Color normalColor = Color.white;
            Color alertColor = new Color(1f, 0.3f, 0.3f); // Красноватый оттенок

            float time = Time.unscaledTime * speed;
            float factor = (Mathf.Sin(time) + 1.0f) / 2.0f; 

            bgImage.transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, factor);
            bgImage.color = Color.Lerp(normalColor, alertColor, factor);
        }
    }

    public void UpdateSlot(string newResourceName, Sprite newIcon, int newAmount, ItemData newItemData)
    {
        resourceName = newResourceName;
        currentAmount = newAmount;
        itemData = newItemData;

        // --- 1. ОБНОВЛЯЕМ ИКОНКУ ---
        if (iconImage != null)
        {
            if (newIcon != null)
            {
                iconImage.sprite = newIcon;
                iconImage.enabled = true;
                iconImage.color = Color.white; 
            }
            else
            {
                iconImage.enabled = false;
            }
        }
        
        // --- 2. АВТОМАТИЧЕСКИ МЕНЯЕМ ПОДЛОЖКУ ---
        if (bgImage != null)
        {
            if (itemData != null && categoryBackgrounds != null && categoryBackgrounds.Length > 0)
            {
                // Получаем индекс категории (0 = Resource, 1 = Tool и т.д.)
                int typeIndex = (int)itemData.itemType;
                
                // Проверяем, не вышли ли мы за пределы массива
                if (typeIndex >= 0 && typeIndex < categoryBackgrounds.Length && categoryBackgrounds[typeIndex] != null)
                {
                    bgImage.sprite = categoryBackgrounds[typeIndex];
                }
                else
                {
                    bgImage.sprite = defaultBackground;
                }
            }
            else
            {
                bgImage.sprite = defaultBackground;
            }
        }
        
        UpdateAmount(newAmount);
        
        var draggable = GetComponent<UIDraggableItem>();
        if (draggable != null)
        {
            InventorySlot tempSlot = new InventorySlot(newItemData, newAmount);
            draggable.SetSlotData(tempSlot);
        }
    }

    public void UpdateAmount(int newAmount)
    {
        currentAmount = newAmount;
        if (amountText != null)
        {
            amountText.text = newAmount > 0 ? newAmount.ToString() : ""; 
            amountText.color = Color.white;
        }
    }

    public void SetHighlight(bool isActive)
    {
        shouldPulse = isActive;
        
        if (buttonComponent != null)
        {
            if (isActive) buttonComponent.transition = Selectable.Transition.None;
            else buttonComponent.transition = Selectable.Transition.ColorTint;
        }

        if (!isActive && bgImage != null)
        {
            bgImage.color = Color.white;
            bgImage.transform.localScale = Vector3.one;
        }
    }

    private void OnDisable()
    {
        // Возвращаем масштаб и цвет на место, если окно внезапно закрыли
        if (bgImage != null)
        {
            bgImage.color = Color.white;
            bgImage.transform.localScale = Vector3.one;
        }
    }
}