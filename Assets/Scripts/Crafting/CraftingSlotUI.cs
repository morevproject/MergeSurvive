using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraftingSlotUI : MonoBehaviour, IDropHandler
{
    [Header("UI References")]
    [SerializeField] private Image slotIconImage;

    [Header("Slot Data")]
    public string resourceName = ""; 
    public Sprite resourceIcon;
    
    // Новое поле: нам нужен реальный объект ItemData, чтобы крафт работал
    public ItemData currentItem; 

    public bool IsEmpty => string.IsNullOrEmpty(resourceName);

    void Awake()
    {
        ClearSlot();
    }
    
    // Вспомогательный метод для получения предмета (нужен для Manger)
    public ItemData GetResourceInSlot()
    {
        return currentItem;
    }

    // --- ИСПРАВЛЕННЫЙ МЕТОД DROPHANDLER ---
    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        // 1. Проверяем, не перетащили ли мы Душу (рецепт) из списка
        SoulItemUI soulUI = draggedObject.GetComponent<SoulItemUI>();
        if (soulUI != null && soulUI.itemData != null)
        {
            UpdateSlot(soulUI.itemData);
            return;
        }

        // 2. Проверяем, не перетащили ли мы Предмет из инвентаря
        UIDraggableItem draggable = draggedObject.GetComponent<UIDraggableItem>();
        if (draggable != null && draggable.inventorySlotRef != null)
        {
            UpdateSlot(draggable.inventorySlotRef.itemData);
            return;
        }
    }

    public void UpdateSlot(ItemData newItem)
    {
        if (newItem == null) return;
        
        currentItem = newItem;
        resourceName = newItem.ItemName;
        resourceIcon = newItem.Icon;

        UpdateSlotVisual();

        // Сообщаем менеджеру
        if (CraftingUIManager.Instance != null)
            CraftingUIManager.Instance.OnSlotUpdated();
    }

    private void UpdateSlotVisual()
    {
        if (slotIconImage != null)
        {
            slotIconImage.sprite = resourceIcon;
            slotIconImage.enabled = (resourceIcon != null);

            // --- ТВОЯ ЛОГИКА РАСТЯГИВАНИЯ ИКОНКИ (СОХРАНЕНО) ---
            if (resourceIcon != null)
            {
                RectTransform rt = slotIconImage.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; 
                rt.offsetMax = Vector2.zero; 
                rt.localScale = Vector3.one;
            }
        }
    }

    public void ClearSlot()
    {
        resourceName = "";
        resourceIcon = null;
        currentItem = null;

        if (slotIconImage != null)
        {
            slotIconImage.enabled = false;
            slotIconImage.sprite = null;
        }

        if (CraftingUIManager.Instance != null)
        {
            CraftingUIManager.Instance.OnSlotUpdated();
        }
    }
}