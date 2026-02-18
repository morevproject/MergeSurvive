using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerEquipmentVisuals : MonoBehaviour
{
    public static PlayerEquipmentVisuals Instance;

    [System.Serializable]
    public class ItemVisual
    {
        public string itemID;       // Имя предмета из ItemData (например "Scrap_Axe")
        public GameObject model;    // Ссылка на объект в руке
    }

    [Header("Weapon Models (Right Hand)")]
    public List<ItemVisual> weaponModels; // Сюда добавляй мечи, дубинки, луки

    [Header("Tool Models (Right Hand - Temporary)")]
    public List<ItemVisual> toolModels;   // Сюда добавляй топоры, кирки, лопаты

    [Header("Armor Models (Body)")]
    public List<ItemVisual> armorModels;  // Сюда добавляй броню

    // Запоминаем текущее оружие, чтобы вернуть его после рубки дерева
    private string currentWeaponID;

    private void Awake()
    {
        Instance = this;
        HideAll();
    }

    // 1. ВЫЗЫВАЕТСЯ ИЗ СЛОТА (когда переодеваемся)
    // Заменил аргумент с ItemData на string для гибкости, но можно и перегрузку оставить
    public void UpdateWeaponVisuals(ItemData item)
    {
        currentWeaponID = (item != null) ? item.ItemName : "";
        RefreshVisuals();
    }

    // Обновляем постоянный визуал (Оружие + Броня)
    public void RefreshVisuals()
    {
        HideAllWeapons();
        
        // Включаем оружие
        if (!string.IsNullOrEmpty(currentWeaponID))
        {
            ItemVisual vis = weaponModels.Find(x => x.itemID == currentWeaponID);
            if (vis != null && vis.model != null) vis.model.SetActive(true);
        }

        // Броня обновляется отдельно, обычно из PlayerProfileUI
        // Если хочешь обновлять её здесь, нужно передавать ID брони в этот метод
    }

    public void EquipArmor(string armorID)
    {
        HideAllArmor();
        if (string.IsNullOrEmpty(armorID)) return;

        ItemVisual vis = armorModels.Find(x => x.itemID == armorID);
        if (vis != null && vis.model != null) vis.model.SetActive(true);
    }

    // 2. ВРЕМЕННЫЙ ИНСТРУМЕНТ (для сбора ресурсов)
    // Теперь принимаем КОНКРЕТНЫЙ ID (например "Cyber_Axe"), а не абстрактный тип
    public void ShowToolTemporarily(string itemID, float duration)
    {
        StartCoroutine(ToolAnimationRoutine(itemID, duration));
    }

    private IEnumerator ToolAnimationRoutine(string itemID, float duration)
    {
        // 1. Прячем оружие
        HideAllWeapons();
        HideAllTools();

        // 2. Ищем и включаем нужный инструмент
        ItemVisual tool = toolModels.Find(x => x.itemID == itemID);
        if (tool != null && tool.model != null)
        {
            tool.model.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[Visuals] Не найдена модель инструмента для: {itemID}");
        }

        // 3. Ждем
        yield return new WaitForSeconds(duration);

        // 4. Прячем инструмент и возвращаем оружие
        HideAllTools();
        RefreshVisuals();
    }

    private void HideAll()
    {
        HideAllWeapons();
        HideAllTools();
        HideAllArmor();
    }

    private void HideAllWeapons() { foreach (var v in weaponModels) if (v.model != null) v.model.SetActive(false); }
    private void HideAllTools() { foreach (var v in toolModels) if (v.model != null) v.model.SetActive(false); }
    private void HideAllArmor() { foreach (var v in armorModels) if (v.model != null) v.model.SetActive(false); }
}