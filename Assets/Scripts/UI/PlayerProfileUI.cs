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
}