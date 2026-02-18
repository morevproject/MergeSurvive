using UnityEngine;
using UnityEngine.EventSystems;

public class AvatarDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    // 1. Проверка: Видит ли мышка вообще этот объект?
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Если при наведении мыши в консоли НЕТ этого сообщения - значит проблема в Raycast Target или перекрытии другим объектом
        Debug.Log(">>> Мышка зашла на Аватар!"); 
    }

    public void OnPointerExit(PointerEventData eventData)
    {
       // Debug.Log("<<< Мышка ушла с Аватара");
    }

    // 2. Основной метод сброса
    public void OnDrop(PointerEventData eventData)
    {
        // Debug.Log(">>> СОБЫТИЕ OnDrop СРАБОТАЛО!"); // Можно закомментировать

        if (eventData.pointerDrag == null) return;

        UIDraggableItem droppedItem = eventData.pointerDrag.GetComponent<UIDraggableItem>();

        if (droppedItem == null || droppedItem.currentSlot == null) return;

        ItemData item = droppedItem.currentSlot.itemData;
        // Debug.Log($"Пытаемся съесть: {item.ItemName}, Тип: {item.itemType}");

        if (item.itemType == ItemType.Food)
        {
            // ИЗМЕНЕНИЕ: Передаем ссылку на сам перетаскиваемый объект (droppedItem)
            TryConsume(item, droppedItem);
        }
        else
        {
            Debug.LogWarning("[Avatar] Это не еда!");
        }
    }

    // ИЗМЕНЕНИЕ: Добавили второй аргумент (UIDraggableItem draggable)
    private void TryConsume(ItemData foodItem, UIDraggableItem draggable)
    {
        if (PlayerStats.Instance == null) return;

        int cur = PlayerStats.Instance.currentHealth;
        int max = PlayerStats.Instance.maxHealth;

        if (cur <= 0) return;

        if (cur < max)
        {
            PlayerStats.Instance.Heal(foodItem.healAmount);
            Debug.Log($"[Avatar] Ням! {foodItem.GetName()} дал +{foodItem.healAmount} HP.");

            // --- ФИКС ЗАВИСШЕЙ ИКОНКИ ---
            // Принудительно убиваем перетаскивание ПЕРЕД удалением предмета
            if (draggable != null) draggable.ForceEndDrag();
            // ----------------------------

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.RemoveItem(foodItem, 1);
            }
        }
        else
        {
             Debug.Log("[Avatar] Отказ: Здоровье уже полное!");
        }
    }
}