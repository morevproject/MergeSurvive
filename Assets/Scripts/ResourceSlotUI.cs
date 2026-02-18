using UnityEngine;
using UnityEngine.UI;

public class ResourceSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;    
    public Text amountText;    

    [Header("Data")]
    public string resourceName; 
    public int currentAmount;
    public ItemData itemData; 

    private bool shouldPulse = false; 
    private Button buttonComponent; 

    private void Awake()
    {
        buttonComponent = GetComponent<Button>();
        
        // АВТОМАТИЧЕСКИ ПОДПИСЫВАЕМСЯ НА КЛИК
        if (buttonComponent != null)
        {
            buttonComponent.onClick.RemoveAllListeners(); // На случай дублей
            buttonComponent.onClick.AddListener(OnSlotClicked);
        }
    }

    // --- ОБРАБОТКА КЛИКА ПО СЛОТУ ---
    public void OnSlotClicked()
    {
        // Передаем данные в панель деталей
        if (itemData != null && InventoryItemDetails.Instance != null)
        {
            InventoryItemDetails.Instance.SelectItem(itemData);
        }
    }
    // --------------------------------

    private void Update()
    {
        // Логика пульсации (для туториалов или подсветки нехватки ресурсов)
        if (shouldPulse && iconImage != null)
        {
            float speed = 8.0f; 
            float minScale = 0.9f;
            float maxScale = 1.1f;
            
            Color normalColor = Color.white;
            Color alertColor = new Color(1f, 0.3f, 0.3f); 

            float time = Time.unscaledTime * speed;
            float factor = (Mathf.Sin(time) + 1.0f) / 2.0f; 

            iconImage.transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, factor);
            iconImage.color = Color.Lerp(normalColor, alertColor, factor);
        }
    }

    public void UpdateSlot(string newResourceName, Sprite newIcon, int newAmount, ItemData newItemData)
    {
        resourceName = newResourceName;
        currentAmount = newAmount;
        itemData = newItemData;

        // Если ссылки на UI слетели, пробуем найти их
        if (iconImage == null)
        {
            Transform t = transform.Find("Icon");
            if (t != null) iconImage = t.GetComponent<Image>();
            if (iconImage == null) iconImage = GetComponent<Image>();
        }

        if (iconImage != null)
        {
            if (newIcon != null)
            {
                iconImage.sprite = newIcon;
                iconImage.enabled = true;
                iconImage.color = Color.white; 
                iconImage.transform.localScale = Vector3.one;
            }
            else
            {
                iconImage.enabled = false;
            }
        }
        
        UpdateAmount(newAmount);
        
        // Обновляем данные для Drag & Drop
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
            // Если пульсируем - отключаем стандартную реакцию кнопки, чтобы не мешала цвету
            if (isActive) buttonComponent.transition = Selectable.Transition.None;
            else buttonComponent.transition = Selectable.Transition.ColorTint;
        }

        if (!isActive && iconImage != null)
        {
            iconImage.color = Color.white;
            iconImage.transform.localScale = Vector3.one;
        }
    }

    private void OnDisable()
    {
        if (iconImage != null)
        {
            iconImage.color = Color.white;
            iconImage.transform.localScale = Vector3.one;
        }
    }
}