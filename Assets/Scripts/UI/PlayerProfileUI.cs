using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))] 
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
    private CanvasGroup canvasGroup; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (PlayerStats.Instance != null)
        {
            // Теперь типы совпадают (int -> int)
            PlayerStats.Instance.OnHealthChanged += UpdateHealthBar;
            
            // Сразу вызываем обновление с текущим здоровьем
            UpdateHealthBar(PlayerStats.Instance.currentHealth);
        }

        if (avatarButton != null)
            avatarButton.onClick.AddListener(TogglePanel);

        if (ammoSlot != null) ammoSlot.gameObject.SetActive(false);
        if (equipmentPanel != null) equipmentPanel.gameObject.SetActive(false); 
    }

    // --- БЛОКИРОВКА ВВОДА (ДЛЯ БОССА) ---
    public void SetInputLock(bool isLocked)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = !isLocked;
        }
    }

    // --- ИСПРАВЛЕНИЕ: Принимаем int, считаем процент сами ---
    private void UpdateHealthBar(int currentHealth)
    {
        if (PlayerStats.Instance == null) return;

        // Считаем процент (0.0 ... 1.0)
        float percent = (float)currentHealth / PlayerStats.Instance.maxHealth;

        if (healthBarFill != null) 
            healthBarFill.fillAmount = percent;
    }
    // --------------------------------------------------------

    public void TogglePanel()
    {
        isPanelExpanded = !isPanelExpanded;
        if (equipmentPanel != null) equipmentPanel.gameObject.SetActive(isPanelExpanded);
    }

    // --- ГЛАВНЫЙ МЕТОД ОБНОВЛЕНИЯ ---
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

            // --- ФИКС БАГА С ХП ---
            // Принудительно обновляем визуал здоровья сразу после пересчета
            UpdateHealthBar(PlayerStats.Instance.currentHealth);
            // ----------------------
        }

        // 3. СИНХРОНИЗАЦИЯ БОЕВЫХ СТАТОВ ИГРОКА
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

        // 4. ОБНОВЛЕНИЕ 3D-МОДЕЛЕЙ НА ПЕРСОНАЖЕ
        if (PlayerEquipmentVisuals.Instance != null)
        {
            PlayerEquipmentVisuals.Instance.UpdateWeaponVisuals(weaponSlot.currentItem);
            
            string armorID = (armorSlot.currentItem != null) ? armorSlot.currentItem.ItemName : "";
            PlayerEquipmentVisuals.Instance.EquipArmor(armorID);
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
        LoadSlot(weaponSlot, data.equippedWeaponID);
        
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

        LoadSlot(armorSlot, data.equippedArmorID);
        LoadSlot(axeSlot, data.equippedAxeID);
        LoadSlot(pickaxeSlot, data.equippedPickaxeID);
        LoadSlot(shovelSlot, data.equippedShovelID);
        
        OnEquipmentChanged(); 
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