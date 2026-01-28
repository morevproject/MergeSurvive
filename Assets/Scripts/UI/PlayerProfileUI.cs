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
    public EquipmentSlotUI weaponSlot;   // Слот оружия
    public EquipmentSlotUI armorSlot;    // Слот брони
    public EquipmentSlotUI ammoSlot;     // Слот боеприпасов (если оружие требует)
    
    // НОВЫЕ ССЫЛКИ
    public EquipmentSlotUI axeSlot;      // Слот топора
    public EquipmentSlotUI pickaxeSlot;  // Слот кирки
    public EquipmentSlotUI shovelSlot;   // Слот лопаты

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
            UpdateHealthBar((float)PlayerStats.Instance.currentHealth / PlayerStats.Instance.maxHealth);
        }

        if (avatarButton != null)
            avatarButton.onClick.AddListener(TogglePanel);

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

    public void OnEquipmentChanged()
    {
        // 1. Логика стрел (как и была)
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

        // 2. Пересчитываем статы (добавляем новые слоты в расчет)
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.RecalculateStats(
                weaponSlot.currentItem, 
                armorSlot.currentItem, 
                ammoSlot != null ? ammoSlot.currentItem : null,
                axeSlot != null ? axeSlot.currentItem : null,      // Передаем топор
                pickaxeSlot != null ? pickaxeSlot.currentItem : null // Передаем кирку
            );
        }
    }
}