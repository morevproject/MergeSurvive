using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIDraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI")]
    public Image image;
    private Transform parentAfterDrag; // Оставляем как было у тебя
    
    // ВАЖНО: Делаем это поле публичным, чтобы EquipmentSlotUI мог узнать, что мы тащим
    public InventorySlot currentSlot; 

    public void SetSlotData(InventorySlot slot)
    {
        currentSlot = slot; // Сохраняем данные
        
        if (image != null && slot != null && slot.itemData != null)
        {
            image.sprite = slot.itemData.Icon;
            image.enabled = true;
        }
    }
    
    [Header("Settings")]
    public Canvas canvas;

    [HideInInspector] public InventorySlot inventorySlotRef; 

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private GameObject draggingObject;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
    }
    
    // Метод для обновления данных (вызывается из ResourceSlotUI)
    public void UpdateData(string name, Sprite icon, int amount, ItemData data)
    {
         // Если нужно обновить внутренние данные, можно сделать это тут
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // --- ТВОЯ ЛОГИКА СОЗДАНИЯ ---
        draggingObject = new GameObject("Dragging Icon");
        draggingObject.transform.SetParent(canvas.transform, false);
        draggingObject.transform.SetAsLastSibling();

        Image image = draggingObject.AddComponent<Image>();
        image.raycastTarget = false; // Важно: пролетаем сквозь иконку

        Sprite foundSprite = null;

        // --- ТВОЯ ЛОГИКА ПОИСКА ИКОНКИ (НЕ ТРОГАЕМ!) ---
        
        // 1. Сначала ищем строго объект с именем "Icon" внутри
        Transform iconTransform = transform.Find("Icon");
        if (iconTransform != null && iconTransform.GetComponent<Image>() != null)
        {
            foundSprite = iconTransform.GetComponent<Image>().sprite;
        }

        // 2. Если не нашли по имени, ищем в SoulItemUI
        if (foundSprite == null)
        {
            var soulUI = GetComponent<SoulItemUI>();
            if (soulUI != null && soulUI.icon != null) foundSprite = soulUI.icon.sprite;
        }

        // 3. Если всё еще нет - ищем ЛЮБУЮ картинку
        if (foundSprite == null)
        {
            var images = GetComponentsInChildren<Image>();
            foreach(var img in images)
            {
                if (img.gameObject != this.gameObject && img.sprite != null)
                {
                    foundSprite = img.sprite;
                    break;
                }
            }
        }
        
        // 4. Крайний случай: берем свою картинку
        if (foundSprite == null && GetComponent<Image>() != null)
        {
            foundSprite = GetComponent<Image>().sprite;
        }
        // -------------------------------------

        if (foundSprite != null)
        {
            image.sprite = foundSprite;
            image.rectTransform.sizeDelta = rectTransform.sizeDelta; 
        }
        else
        {
            image.color = new Color(1, 0, 0, 0.5f); // Красный квадрат ошибки
            image.rectTransform.sizeDelta = new Vector2(50, 50);
        }

        if (canvasGroup != null) canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggingObject != null)
        {
            draggingObject.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Теперь просто вызываем наш новый метод, чтобы не дублировать код
        ForceEndDrag();
    }

    // === НОВЫЙ МЕТОД ===
    // Нужен, чтобы скрипт слота мог сам сказать "Хватит тащить, иконка прилипла"
    public void ForceEndDrag()
    {
        if (draggingObject != null)
        {
            Destroy(draggingObject);
            draggingObject = null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }
}