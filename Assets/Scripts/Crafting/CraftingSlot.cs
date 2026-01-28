using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CraftingSlot : MonoBehaviour, IDropHandler
{
    [Header("UI")]
    public Image iconImage;     // Ссылка на картинку внутри слота
    public Sprite defaultSprite; // Спрайт для пустого слота (если нужен)

    [Header("Data")]
    public ItemData currentItem;

    void Start()
    {
        UpdateSlotUI();
    }

    // Срабатывает при падении предмета на слот
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[CraftingSlot] OnDrop сработал на слоте {gameObject.name}!");
        
        GameObject draggedObj = eventData.pointerDrag;
        if (draggedObj == null) 
        {
            Debug.LogError("Перетаскиваемый объект (draggedObj) равен NULL!");
            return;
        }

        Debug.Log($"Мы тащим объект с именем: '{draggedObj.name}'");

        // Проверка 1: SoulItemUI
        SoulItemUI soulUI = draggedObj.GetComponent<SoulItemUI>();
        Debug.Log($"Поиск SoulItemUI: {(soulUI != null ? "НАЙДЕН" : "НЕ НАЙДЕН")}");
        
        if (soulUI != null)
        {
             Debug.Log($"В SoulItemUI есть ItemData? {(soulUI.itemData != null ? "ДА ("+soulUI.itemData.ItemName+")" : "НЕТ (NULL)")}");
             if (soulUI.itemData != null)
             {
                 SetItem(soulUI.itemData);
                 return;
             }
        }

        // Проверка 2: Инвентарь
        UIDraggableItem draggable = draggedObj.GetComponent<UIDraggableItem>();
        if (draggable != null && draggable.inventorySlotRef != null)
        {
            Debug.Log("Это предмет из инвентаря. Принимаем.");
            SetItem(draggable.inventorySlotRef.itemData);
            return;
        }
        
        Debug.LogWarning("Слот не принял предмет, так как не нашел компонентов данных!");
    }

    public void SetItem(ItemData newItem)
    {
        currentItem = newItem;
        UpdateSlotUI();

        // Сообщаем менеджеру UI, что содержимое изменилось
        if (CraftingUIManager.Instance != null)
        {
            CraftingUIManager.Instance.OnSlotUpdated();
        }
        // Поддержка старого варианта (через Panel)
        else if (CraftingPanel.Instance != null)
        {
            CraftingPanel.Instance.CheckCrafting();
        }
    }

    public void ClearSlot()
    {
        currentItem = null;
        UpdateSlotUI();
    }

    public ItemData GetResourceInSlot()
    {
        return currentItem;
    }

    private void UpdateSlotUI()
    {
        if (iconImage == null) return;

        if (currentItem != null)
        {
            iconImage.sprite = currentItem.Icon;
            iconImage.color = Color.white;
            iconImage.enabled = true;
        }
        else
        {
            if (defaultSprite != null)
            {
                iconImage.sprite = defaultSprite;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }
    }
}