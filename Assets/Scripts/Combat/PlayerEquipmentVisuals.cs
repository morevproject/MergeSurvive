using UnityEngine;
using System.Collections;

public class PlayerEquipmentVisuals : MonoBehaviour
{
    [Header("Melee Weapons")]
    public GameObject swordModel;   

    [Header("Tools")]
    public GameObject axeModel;     
    public GameObject pickaxeModel; 
    public GameObject shovelModel; // <--- НОВОЕ ПОЛЕ

    [Header("Ranged Weapons")]
    public GameObject bowModel;     

    // Запоминаем, что у нас реально надето, чтобы вернуть после рубки
    private ItemData currentEquippedItem;

    // 1. Этот метод вызывается из UI слота (когда одеваем/снимаем)
    public void UpdateWeaponVisuals(ItemData item)
    {
        currentEquippedItem = item;
        RefreshVisuals();
    }

    // Внутренний метод обновления (показывает только оружие, прячет инструменты)
    private void RefreshVisuals()
    {
        // Сначала всё выключаем
        HideAll();

        if (currentEquippedItem == null) return;

        switch (currentEquippedItem.ItemName)
        {
            // Оружие показываем ВСЕГДА
            case "Sword":
            case "Iron Sword":
                if (swordModel != null) swordModel.SetActive(true);
                break;
            
            case "Bow":
            case "Wooden Bow":
                if (bowModel != null) bowModel.SetActive(true);
                break;

            // Инструменты НЕ показываем (они в "кармане")
        }
    }

    // 2. ВРЕМЕННЫЙ метод: вызывай его из скрипта сбора ресурсов (InteractionUIManager)
    public void ShowToolTemporarily(string toolType, float duration)
    {
        StartCoroutine(ToolAnimationRoutine(toolType, duration));
    }

    private IEnumerator ToolAnimationRoutine(string toolType, float duration)
    {
        // 1. Прячем текущее оружие (меч/лук)
        HideAll();

        // 2. Показываем нужный инструмент
        if (toolType == "Axe" && axeModel != null) axeModel.SetActive(true);
        if (toolType == "Pickaxe" && pickaxeModel != null) pickaxeModel.SetActive(true);
        if (toolType == "Shovel" && shovelModel != null) shovelModel.SetActive(true); // <--- ЛОПАТА

        // 3. Ждем пока пройдет анимация удара
        yield return new WaitForSeconds(duration);

        // 4. Возвращаем всё как было (меч, лук или пустые руки)
        RefreshVisuals();
    }

    private void HideAll()
    {
        if (swordModel != null) swordModel.SetActive(false);
        if (axeModel != null) axeModel.SetActive(false);
        if (pickaxeModel != null) pickaxeModel.SetActive(false);
        if (shovelModel != null) shovelModel.SetActive(false); // <--- ПРЯЧЕМ ЛОПАТУ
        if (bowModel != null) bowModel.SetActive(false);
    }
}