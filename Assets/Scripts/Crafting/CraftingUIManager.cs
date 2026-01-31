using UnityEngine;
using System.Collections.Generic;
using System.Collections; 

public class CraftingUIManager : MonoBehaviour
{
    public static CraftingUIManager Instance;

    [Header("UI References")]
    public List<CraftingSlot> craftingSlots; // Слоты крафта
    public CraftingResultSlot resultSlot;    // Слот результата

    public bool IsCrafting { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // ИСПРАВЛЕНИЕ: Вернул название OnSlotUpdated, чтобы CraftingSlotUI не ругался
    public void OnSlotUpdated()
    {
        if (IsCrafting) return;

        // Собираем список предметов (ItemData) из слотов
        List<ItemData> ingredients = new List<ItemData>();
        bool hasItems = false;

        foreach (var slot in craftingSlots)
        {
            if (slot == null) continue;

            // Берем данные предмета из слота
            ItemData item = slot.GetResourceInSlot();

            if (item != null)
            {
                hasItems = true;
                ingredients.Add(item);
            }
        }

        // Если слоты пустые — очищаем результат
        if (!hasItems)
        {
            if (resultSlot != null) resultSlot.ClearSlot();
            return;
        }

        // Ищем рецепт через менеджер
        Recipe foundRecipe = CraftingManager.Instance.TryMergeItems(ingredients);

        if (foundRecipe != null)
        {
            StartCoroutine(ProcessDiscovery(foundRecipe));
        }
        else
        {
            if (resultSlot != null) resultSlot.ClearSlot();
        }
    }

    IEnumerator ProcessDiscovery(Recipe recipe)
    {
        IsCrafting = true;

        // Показываем, что получится
        if (resultSlot != null) resultSlot.SetResult(recipe);

        yield return new WaitForSeconds(1.0f);

        // Открываем рецепт
        CraftingManager.Instance.DiscoverRecipe(recipe);

        // Очищаем ингредиенты
        foreach (var slot in craftingSlots)
        {
            if (slot != null) slot.ClearSlot();
        }

        // Очищаем результат
        if (resultSlot != null) resultSlot.ClearSlot();

        IsCrafting = false;
        
        // Проверяем еще раз (на случай, если остались предметы)
        OnSlotUpdated();
    }

    // =========================================================
    // НОВЫЙ МЕТОД ДЛЯ КНОПКИ "ОЧИСТИТЬ"
    // =========================================================
    public void ClearAllSlots()
    {
        // Не даем очищать, если прямо сейчас идет анимация крафта
        if (IsCrafting) return;

        // 1. Пробегаем по всем слотам крафта и чистим их
        foreach (var slot in craftingSlots)
        {
            if (slot != null)
            {
                slot.ClearSlot(); // Этот метод просто удаляет иконку и ItemData
            }
        }

        // 2. Очищаем слот результата
        if (resultSlot != null)
        {
            resultSlot.ClearSlot();
        }

        Debug.Log("UI Верстака очищен по кнопке.");
    }
}