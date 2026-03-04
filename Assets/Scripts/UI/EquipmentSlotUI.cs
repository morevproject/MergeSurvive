using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// === ВОТ ОН, НАШ ПОТЕРЯШКА! ===
public enum EquipmentSlotType
{
    Weapon, Armor, Ammo, ToolAxe, ToolPickaxe, ToolShovel
}

public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    // Умный словарь: Слот сам записывает себя сюда при включении, 
    // чтобы кнопка "Надеть" мгновенно его находила!
    public static Dictionary<EquipmentSlotType, EquipmentSlotUI> ActiveSlots = new Dictionary<EquipmentSlotType, EquipmentSlotUI>();

    [Header("Settings")]
    public EquipmentSlotType slotType;
    public Image iconImage;     
    public Sprite emptyIcon;
    public Text amountText; 

    [HideInInspector] public ItemData currentItem;

    private void OnEnable()
    {
        ActiveSlots[slotType] = this;
    }

    private void OnDisable()
    {
        if (ActiveSlots.ContainsKey(slotType) && ActiveSlots[slotType] == this)
        {
            ActiveSlots.Remove(slotType);
        }
    }

    private void Awake()
    {
        currentItem = null;
    }

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChange += UpdateVisual;
        }
        Invoke(nameof(UpdateVisual), 0.1f);
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChange -= UpdateVisual;
        }
    }

    // --- ЛОГИКА НАДЕВАНИЯ (ВЫЗЫВАЕТСЯ ПО КНОПКЕ) ---
    public void EquipItem(ItemData item)
    {
        if (slotType == EquipmentSlotType.Ammo)
        {
            currentItem = item;
            UpdateVisual();
            if (PlayerProfileUI.Instance != null) PlayerProfileUI.Instance.OnEquipmentChanged();
            return; 
        }

        // Если слот уже занят — возвращаем старый предмет в инвентарь
        if (currentItem != null)
        {
            InventoryManager.Instance.AddItem(currentItem, 1);
        }

        currentItem = item;
        InventoryManager.Instance.RemoveItem(item, 1);

        UpdateVisual();
        if (PlayerProfileUI.Instance != null) PlayerProfileUI.Instance.OnEquipmentChanged();

        if (slotType == EquipmentSlotType.Weapon)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                var playerStats = playerObj.GetComponent<FighterStats>();
                if (playerStats != null) playerStats.UpdateWeaponStats(item.minDamage, item.maxDamage, item.attackRange);
            }
        }
    }

    public void Unequip()
    {
        if (currentItem == null) return;

        if (slotType == EquipmentSlotType.Ammo)
        {
            currentItem = null;
            UpdateVisual();
            if (PlayerProfileUI.Instance != null) PlayerProfileUI.Instance.OnEquipmentChanged();
            return;
        }

        InventoryManager.Instance.AddItem(currentItem, 1);
        currentItem = null;
        
        UpdateVisual();
        if (PlayerProfileUI.Instance != null) PlayerProfileUI.Instance.OnEquipmentChanged();

        if (slotType == EquipmentSlotType.Weapon)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                var playerStats = playerObj.GetComponent<FighterStats>();
                if (playerStats != null) playerStats.UpdateWeaponStats(0, 0, 0); 
            }
        }
    }

    // Снимаем экипировку просто по тапу
    public void OnPointerClick(PointerEventData eventData)
    {
        Unequip();
    }

    // --- ОБНОВЛЕНИЕ ВИЗУАЛА ---
    public void UpdateVisual()
    {
        if (currentItem != null)
        {
            iconImage.sprite = currentItem.Icon;
            iconImage.enabled = true;
            iconImage.color = Color.white;
            iconImage.raycastTarget = true; 

            if (amountText != null)
            {
                if (slotType == EquipmentSlotType.Ammo)
                {
                    int totalArrows = InventoryManager.Instance.GetItemCount(currentItem);
                    amountText.text = totalArrows.ToString();
                    amountText.gameObject.SetActive(true);
                }
                else
                {
                    amountText.gameObject.SetActive(false); 
                }
            }
        }
        else
        {
            iconImage.raycastTarget = false;

            if (emptyIcon != null) 
            {
                iconImage.sprite = emptyIcon;
                iconImage.enabled = true; 
            }
            else
            {
                iconImage.enabled = false;
            }

            if (amountText != null) amountText.gameObject.SetActive(false);
        }
    }
}