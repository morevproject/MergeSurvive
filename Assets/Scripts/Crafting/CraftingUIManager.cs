using UnityEngine;
using System.Collections.Generic;
using System.Collections; 

public class CraftingUIManager : MonoBehaviour
{
    public static CraftingUIManager Instance;

    [Header("UI References")]
    public List<CraftingSlotUI> craftingSlots; 
    public CraftingResultSlot resultSlot;    

    public bool IsCrafting { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void OnSlotUpdated()
    {
        if (IsCrafting) return;

        List<ItemData> ingredients = new List<ItemData>();
        bool hasItems = false;

        foreach (var slot in craftingSlots)
        {
            if (slot == null) continue;

            ItemData item = slot.GetResourceInSlot();

            if (item != null)
            {
                hasItems = true;
                ingredients.Add(item);
            }
        }

        if (!hasItems)
        {
            if (resultSlot != null) resultSlot.ClearSlot();
            return;
        }

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
        IsCrafting = true; // Сразу вешаем жесткий замок!

        // === УМНЫЙ ФЕЙСКОНТРОЛЬ ===
        // Собираем список того, что РЕАЛЬНО нужно для рецепта
        List<string> requiredItems = new List<string>();
        foreach (var req in recipe.requiredIngredients)
        {
            for (int i = 0; i < req.amount; i++) requiredItems.Add(req.resourceName);
        }

        // Проходим с НАЧАЛА В КОНЕЦ. Оставляем первые правильные души,
        // а всё, что не вписывается (даже если оно затесалось в середине), удаляем!
        for (int i = 0; i < craftingSlots.Count; i++)
        {
            var slot = craftingSlots[i];
            if (slot != null && slot.GetResourceInSlot() != null)
            {
                string resName = slot.GetResourceInSlot().ItemName;
                
                // Если душа нужна - вычеркиваем её из "списка ожидания" и оставляем на столе
                if (requiredItems.Contains(resName))
                {
                    requiredItems.Remove(resName); 
                }
                else
                {
                    // Душа лишняя - испаряем!
                    slot.ClearSlot(); 
                }
            }
        }
        // ============================

        // Показываем результат (Иконку нового рецепта)
        if (resultSlot != null) resultSlot.SetResult(recipe);

        // === ДАЕМ ВРЕМЯ ДЛЯ АНИМАЦИИ (2.5 сек) ===
        yield return new WaitForSeconds(2.5f);

        // Торжественно открываем рецепт
        CraftingManager.Instance.DiscoverRecipe(recipe);

        // Финальная зачистка стола после открытия
        foreach (var slot in craftingSlots)
        {
            if (slot != null) slot.ClearSlot();
        }

        if (resultSlot != null) resultSlot.ClearSlot();

        // Снимаем блок, игрок снова может кликать
        IsCrafting = false;
        
        OnSlotUpdated();
    }

    public void ClearAllSlots()
    {
        if (IsCrafting) return;

        IsCrafting = true; // Блокируем, чтобы не вызвать случайное открытие

        foreach (var slot in craftingSlots)
        {
            if (slot != null)
            {
                slot.ClearSlot(); 
            }
        }

        if (resultSlot != null)
        {
            resultSlot.ClearSlot();
        }

        IsCrafting = false; 

        OnSlotUpdated(); 
        Debug.Log("UI Верстака очищен по кнопке.");
    }
}