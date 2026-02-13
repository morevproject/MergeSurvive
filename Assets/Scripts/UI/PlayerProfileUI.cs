using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))] // <--- АВТОМАТИЧЕСКИ ДОБАВЛЯЕТ КОМПОНЕНТ
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
    
    [Header("Tools")]
    public EquipmentSlotUI axeSlot;      
    public EquipmentSlotUI pickaxeSlot;  
    public EquipmentSlotUI shovelSlot;   

    private bool isPanelExpanded = false;
    private CanvasGroup canvasGroup; // <--- Ссылка на компонент блокировки

    private void Awake()
    {
        if (Instance == null) Instance = this;
        
        // Получаем компонент для управления прозрачностью и кликами
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnHealthChanged += UpdateHealthBar;
            UpdateHealthBar((float)PlayerStats.Instance.currentHealth / PlayerStats.Instance.maxHealth);
        }

        if (avatarButton != null)
            avatarButton.onClick.AddListener(TogglePanel);

        if (ammoSlot != null) ammoSlot.gameObject.SetActive(false);
        if (equipmentPanel != null) equipmentPanel.gameObject.SetActive(false); 
    }

    // --- НОВЫЙ МЕТОД: БЛОКИРОВКА ВВОДА (ДЛЯ БОССА) ---
    public void SetInputLock(bool isLocked)
    {
        if (canvasGroup != null)
        {
            // Если locked = true, мы отключаем Raycast (клики проходят сквозь, нажать нельзя)
            // Но при этом все элементы остаются видимыми.
            canvasGroup.blocksRaycasts = !isLocked;
        }
    }
    // -------------------------------------------------

    private void UpdateHealthBar(float percent)
    {
        if (healthBarFill != null) healthBarFill.fillAmount = percent;
    }

    public void TogglePanel()
    {
        isPanelExpanded = !isPanelExpanded;
        if (equipmentPanel != null) equipmentPanel.gameObject.SetActive(isPanelExpanded);
    }

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
            if (!showAmmo && ammoSlot.currentItem != null)
            {
                ammoSlot.EquipItem(null); 
            }
        }

        // 2. Пересчитываем ГЛОБАЛЬНЫЕ статы
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.RecalculateStats(
                weaponSlot.currentItem, 
                armorSlot.currentItem,         
                ammoSlot != null ? ammoSlot.currentItem : null,
                axeSlot != null ? axeSlot.currentItem : null,     
                pickaxeSlot != null ? pickaxeSlot.currentItem : null,
                shovelSlot != null ? shovelSlot.currentItem : null 
            );
        }

        // 3. СИНХРОНИЗАЦИЯ ИГРОКА (Критично для Босса!)
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            var fighterStats = playerObj.GetComponent<FighterStats>();
            if (fighterStats != null)
            {
                ItemData weapon = weaponSlot.currentItem;
                if (weapon != null)
                {
                    fighterStats.UpdateWeaponStats(weapon.minDamage, weapon.maxDamage, weapon.attackRange);
                }
                else
                {
                    fighterStats.UpdateWeaponStats(0, 0, 0); 
                }
            }
        }
    }

    public void SaveEquipmentToData(SaveData data)
    {
        data.equippedWeaponID = (weaponSlot.currentItem != null) ? weaponSlot.currentItem.ItemName : "";
        data.equippedArmorID = (armorSlot.currentItem != null) ? armorSlot.currentItem.ItemName : "";
        data.equippedAxeID = (axeSlot != null && axeSlot.currentItem != null) ? axeSlot.currentItem.ItemName : "";
        data.equippedPickaxeID = (pickaxeSlot != null && pickaxeSlot.currentItem != null) ? pickaxeSlot.currentItem.ItemName : "";
        data.equippedShovelID = (shovelSlot != null && shovelSlot.currentItem != null) ? shovelSlot.currentItem.ItemName : "";

        if (ammoSlot != null && ammoSlot.currentItem != null)
        {
            data.equippedAmmoID = ammoSlot.currentItem.ItemName;
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
        // 1. Грузим ОРУЖИЕ
        LoadSlot(weaponSlot, data.equippedWeaponID);
        
        // 2. СТРЕЛЫ
        if (ammoSlot != null)
        {
            bool hasSavedAmmo = !string.IsNullOrEmpty(data.equippedAmmoID);
            bool weaponNeedsAmmo = (weaponSlot.currentItem != null && weaponSlot.currentItem.requiresAmmo);

            if (hasSavedAmmo || weaponNeedsAmmo) ammoSlot.gameObject.SetActive(true);
            else ammoSlot.gameObject.SetActive(false);

            if (hasSavedAmmo)
            {
                ItemData item = Resources.Load<ItemData>($"Items/{data.equippedAmmoID}");
                if (item != null)
                {
                    ammoSlot.currentItem = item;
                    ammoSlot.UpdateVisual();
                }
            }
        }

        // 3. Грузим остальное
        LoadSlot(armorSlot, data.equippedArmorID);
        LoadSlot(axeSlot, data.equippedAxeID);
        LoadSlot(pickaxeSlot, data.equippedPickaxeID);
        LoadSlot(shovelSlot, data.equippedShovelID);
        
        // 4. ОБЯЗАТЕЛЬНО: Мгновенный пересчет для синхронизации FighterStats
        OnEquipmentChanged(); 
        
        // 5. Отложенная перестраховка
        Invoke(nameof(ForceStatsRecalculation), 0.2f);
    }

    private void ForceStatsRecalculation()
    {
        OnEquipmentChanged(); 
        var combat = FindObjectOfType<PlayerCombatController>();
        if (combat != null)
        {
            combat.enabled = false; 
            combat.enabled = true; 
        }
    }

    private void LoadSlot(EquipmentSlotUI slot, string itemID)
    {
        if (slot == null) return;
        SetItemToSlotVisualsOnly(slot, null);
        if (!string.IsNullOrEmpty(itemID))
        {
            ItemData item = Resources.Load<ItemData>($"Items/{itemID}");
            if (item != null) SetItemToSlotVisualsOnly(slot, item);
        }
    }

    private void SetItemToSlotVisualsOnly(EquipmentSlotUI slot, ItemData item)
    {
        slot.currentItem = item;
        slot.UpdateVisual(); 
    }
}