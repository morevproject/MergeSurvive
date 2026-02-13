using UnityEngine;
using UnityEngine.UI;

public class PlayerProfileUI : MonoBehaviour
{
    public static PlayerProfileUI Instance;

    [Header("Health UI")]
    public Image healthBarFill; 

    [Header("Panel Animation")]
    public RectTransform equipmentPanel; 
    public Button avatarButton;          
    
    [Header("Slots")]
    public EquipmentSlotUI weaponSlot;   
    public EquipmentSlotUI armorSlot;    
    public EquipmentSlotUI ammoSlot;     
    
    // Ссылки на инструменты
    public EquipmentSlotUI axeSlot;      
    public EquipmentSlotUI pickaxeSlot;  
    public EquipmentSlotUI shovelSlot;   // <--- Лопата тут уже есть

    private bool isPanelExpanded = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnHealthChanged += UpdateHealthBar;
            // Обновляем бар сразу
            UpdateHealthBar((float)PlayerStats.Instance.currentHealth / PlayerStats.Instance.maxHealth);
        }

        if (avatarButton != null)
            avatarButton.onClick.AddListener(TogglePanel);

        // Прячем лишнее со старта
        if (ammoSlot != null) ammoSlot.gameObject.SetActive(false);
        if (equipmentPanel != null) equipmentPanel.gameObject.SetActive(false); 
    }

    private void UpdateHealthBar(float percent)
    {
        if (healthBarFill != null) healthBarFill.fillAmount = percent;
    }

    public void TogglePanel()
    {
        isPanelExpanded = !isPanelExpanded;
        if (equipmentPanel != null) equipmentPanel.gameObject.SetActive(isPanelExpanded);
    }

    // --- ГЛАВНЫЙ МЕТОД: ВЫЗЫВАЕТСЯ ИЗ СЛОТОВ ---
    public void OnEquipmentChanged()
    {
        // 1. Логика стрел
        bool showAmmo = false;
        if (weaponSlot.currentItem != null && weaponSlot.currentItem.requiresAmmo)
        {
            showAmmo = true;
        }

        if (ammoSlot != null)
        {
            ammoSlot.gameObject.SetActive(showAmmo);
            // Если слот скрылся, а там что-то лежало - снимаем (опционально)
            if (!showAmmo && ammoSlot.currentItem != null)
            {
                ammoSlot.EquipItem(null); 
            }
        }

        // 2. Пересчитываем статы (ТЕПЕРЬ С БРОНЕЙ И ЛОПАТОЙ)
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.RecalculateStats(
                weaponSlot.currentItem, 
                armorSlot.currentItem,         // <--- Броня пошла в расчет!
                ammoSlot != null ? ammoSlot.currentItem : null,
                axeSlot != null ? axeSlot.currentItem : null,     
                pickaxeSlot != null ? pickaxeSlot.currentItem : null,
                shovelSlot != null ? shovelSlot.currentItem : null // <--- ДОБАВИЛИ ЛОПАТУ (6-й аргумент)
            );
        }
    }

    public void SaveEquipmentToData(SaveData data)
    {
        // Если в слоте что-то есть - сохраняем имя предмета (ItemName), иначе пустую строку
        data.equippedWeaponID = (weaponSlot.currentItem != null) ? weaponSlot.currentItem.ItemName : "";
        data.equippedArmorID = (armorSlot.currentItem != null) ? armorSlot.currentItem.ItemName : "";
        
        data.equippedAxeID = (axeSlot != null && axeSlot.currentItem != null) ? axeSlot.currentItem.ItemName : "";
        data.equippedPickaxeID = (pickaxeSlot != null && pickaxeSlot.currentItem != null) ? pickaxeSlot.currentItem.ItemName : "";
        data.equippedShovelID = (shovelSlot != null && shovelSlot.currentItem != null) ? shovelSlot.currentItem.ItemName : "";

        // Для патронов сохраняем и имя, и количество
        if (ammoSlot != null && ammoSlot.currentItem != null)
        {
            data.equippedAmmoID = ammoSlot.currentItem.ItemName;
            // Берем количество из инвентаря, так как слот отображает предмет
            data.equippedAmmoCount = InventoryManager.Instance.GetItemCount(ammoSlot.currentItem);
        }
        else
        {
            data.equippedAmmoID = "";
            data.equippedAmmoCount = 0;
        }
    }

    public void LoadEquipmentFromData(SaveData data)
    {
        // 1. Сначала грузим ОРУЖИЕ
        LoadSlot(weaponSlot, data.equippedWeaponID);
        
        // 2. Сразу после оружия вызываем пересчет!
        // Это заставит UI проверить: "Ага, это лук! Надо включить слот для стрел!"
        OnEquipmentChanged(); 

        // 3. Теперь, когда слот стрел (возможно) включился, грузим сами СТРЕЛЫ
        if (!string.IsNullOrEmpty(data.equippedAmmoID) && ammoSlot != null)
        {
            // Принудительно включаем слот перед загрузкой, если он вдруг еще выключен
            // (хотя OnEquipmentChanged выше должен был это сделать, но подстрахуемся)
            if (weaponSlot.currentItem != null && weaponSlot.currentItem.requiresAmmo)
            {
                ammoSlot.gameObject.SetActive(true);
            }

            ItemData item = Resources.Load<ItemData>($"Items/{data.equippedAmmoID}");
            if (item != null)
            {
                // Ставим визуально
                SetItemToSlotVisualsOnly(ammoSlot, item);
            }
        }

        // 4. Грузим остальное (Броня, Инструменты)
        LoadSlot(armorSlot, data.equippedArmorID);
        LoadSlot(axeSlot, data.equippedAxeID);
        LoadSlot(pickaxeSlot, data.equippedPickaxeID);
        LoadSlot(shovelSlot, data.equippedShovelID);
        
        // 5. ФИНАЛЬНЫЙ ПЕРЕСЧЕТ СТАТОВ
        // Теперь, когда броня загружена, этот вызов применит защиту к статам игрока.
        Invoke(nameof(DelayedStatsUpdate), 0.1f);
    }

    // Небольшая задержка нужна, чтобы убедиться, что все скрипты (в т.ч. PlayerStats) прогрузились
    private void DelayedStatsUpdate()
    {
        OnEquipmentChanged();
        Debug.Log("[PlayerProfileUI] Load complete. Stats recalculated.");
    }

    private void LoadSlot(EquipmentSlotUI slot, string itemID)
    {
        if (slot == null) return;
        
        // Очищаем слот перед загрузкой
        SetItemToSlotVisualsOnly(slot, null);

        if (!string.IsNullOrEmpty(itemID))
        {
            // Пытаемся найти предмет в папке Resources/Items/
            ItemData item = Resources.Load<ItemData>($"Items/{itemID}");
            if (item != null)
            {
                SetItemToSlotVisualsOnly(slot, item);
            }
        }
    }

    // ВАЖНО: Этот метод ставит предмет БЕЗ логики "RemoveFromInventory"
    // Потому что при загрузке игры мы не забираем предмет из рюкзака, он уже сохранен как "надетый"
    private void SetItemToSlotVisualsOnly(EquipmentSlotUI slot, ItemData item)
    {
        slot.currentItem = item;
        slot.UpdateVisual(); // Теперь можно вызывать напрямую!
    }
}