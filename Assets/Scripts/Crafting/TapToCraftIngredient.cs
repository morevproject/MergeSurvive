using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class TapToCraftIngredient : MonoBehaviour, IPointerClickHandler
{
    private ItemData itemData;
    private Button myButton;

    void Start()
    {
        TryFindItemData();

        myButton = GetComponent<Button>();
        
        if (myButton != null)
        {
            myButton.onClick.AddListener(DoCraftJump);
        }
    }

    private void TryFindItemData()
    {
        var soulUI = GetComponent<SoulItemUI>();
        if (soulUI != null && soulUI.itemData != null) 
        {
            itemData = soulUI.itemData;
            return;
        }

        var ingredientUI = GetComponent<CraftingIngredientUI>();
        if (ingredientUI != null && !string.IsNullOrEmpty(ingredientUI.ResourceName))
        {
            itemData = Resources.Load<ItemData>("Items/" + ingredientUI.ResourceName);
            if (itemData == null) itemData = Resources.Load<ItemData>(ingredientUI.ResourceName);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (myButton == null)
        {
            DoCraftJump();
        }
    }

    public void DoCraftJump()
    {
        if (itemData == null) TryFindItemData();

        if (itemData == null) 
        {
            Debug.LogWarning($"[TapToCraft] Не могу найти данные для предмета {gameObject.name}");
            return;
        }

        IEnumerable<CraftingSlotUI> availableSlots = null;

        if (CraftingUIManager.Instance != null && CraftingUIManager.Instance.craftingSlots != null)
        {
            availableSlots = CraftingUIManager.Instance.craftingSlots;
        }
        else if (CraftingPanel.Instance != null && CraftingPanel.Instance.slots != null)
        {
            availableSlots = CraftingPanel.Instance.slots;
        }

        if (availableSlots != null)
        {
            foreach (var slot in availableSlots)
            {
                if (slot == null) continue; 

                // Ищем свободный слот
                if (slot.currentItem == null)
                {
                    // === ЗАПУСКАЕМ КРАСИВЫЙ ПОЛЕТ ===
                    StartCoroutine(FlyToSlotRoutine(slot, itemData));
                    return; 
                }
            }
            Debug.Log("[TapToCraft] Все слоты крафта заняты!");
        }
    }

    // === НОВАЯ МАГИЯ АНИМАЦИИ ===
    private IEnumerator FlyToSlotRoutine(CraftingSlotUI slot, ItemData data)
    {
        // 1. Бронируем слот мгновенно (чтобы быстрые тапы не засунули туда 2 предмета)
        slot.SetItem(data);
        
        // 2. Прячем иконку в слоте на время полета (сохраняем интригу)
        if (slot.iconImage != null) slot.iconImage.enabled = false;

        // 3. Запускаем анимацию полета через HintManager (скорость 0.25 сек - быстро и резко!)
        if (HintManager.Instance != null && HintManager.Instance.flyingIconPrefab != null)
        {
            yield return HintManager.Instance.StartCoroutine(HintManager.Instance.FlyIconToSlot(data.Icon, transform.position, slot, 0.25f));
        }
        else
        {
            // Если HintManager вдруг отключен, просто ждем кадр для безопасности
            yield return null; 
        }

        // 4. Полет окончен — включаем иконку (если игрок не успел нажать кнопку Очистить во время полета)
        if (slot != null && slot.currentItem == data && slot.iconImage != null)
        {
            slot.iconImage.enabled = true;
        }
    }
}