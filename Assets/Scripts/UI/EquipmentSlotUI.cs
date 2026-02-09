using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum EquipmentSlotType
{
    Weapon, Armor, Ammo, ToolAxe, ToolPickaxe, ToolShovel
}

public class EquipmentSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public EquipmentSlotType slotType;
    public Image iconImage;
    public Sprite emptyIcon;
    public Text amountText; 
    public Canvas canvas; 

    [HideInInspector] public ItemData currentItem;

    private GameObject draggingObject;
    private CanvasGroup canvasGroup; 

    private void Start()
    {
        UpdateVisual();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();

        // ИСПРАВЛЕНО: Используем правильное имя события из InventoryManager
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChange += UpdateVisual;
        }
    }

    private void OnDestroy()
    {
        // ИСПРАВЛЕНО: Отписываемся от правильного события
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChange -= UpdateVisual;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null) return;

        UIDraggableItem draggable = droppedObj.GetComponent<UIDraggableItem>();
        if (draggable != null && draggable.currentSlot.itemData != null)
        {
            ItemData newItem = draggable.currentSlot.itemData;
            if (ValidateItem(newItem))
            {
                draggable.ForceEndDrag();
                EquipItem(newItem);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData) 
    { 
        if (currentItem == null || canvas == null) return;
        
        draggingObject = new GameObject("Dragging Equip Icon");
        draggingObject.transform.SetParent(canvas.transform, false);
        draggingObject.transform.SetAsLastSibling();
        
        Image dragImage = draggingObject.AddComponent<Image>();
        dragImage.raycastTarget = false; 
        dragImage.sprite = currentItem.Icon;
        dragImage.rectTransform.sizeDelta = iconImage.rectTransform.sizeDelta;
        
        iconImage.enabled = false;
    }

    public void OnDrag(PointerEventData eventData) 
    { 
        if (draggingObject != null) draggingObject.transform.position = eventData.position; 
    }

    public void OnEndDrag(PointerEventData eventData) 
    { 
        if (draggingObject != null) Destroy(draggingObject); 
        Unequip(); 
    }

    private bool ValidateItem(ItemData item)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Weapon: return item.itemType == ItemType.Weapon || item.itemType == ItemType.Tool;
            case EquipmentSlotType.Armor: return item.itemType == ItemType.Armor;
            case EquipmentSlotType.Ammo: return item.itemType == ItemType.Ammo;
            case EquipmentSlotType.ToolAxe: return item.itemType == ItemType.Tool && item.toolType == ToolType.Axe;
            case EquipmentSlotType.ToolPickaxe: return item.itemType == ItemType.Tool && item.toolType == ToolType.Pickaxe;
            case EquipmentSlotType.ToolShovel: return item.itemType == ItemType.Tool && item.toolType == ToolType.Shovel;
        }
        return false;
    }

    public void EquipItem(ItemData item)
    {
        currentItem = item;
        UpdateVisual();
        if (PlayerProfileUI.Instance != null) PlayerProfileUI.Instance.OnEquipmentChanged();

        // Если это слот Оружия — передаем статы предмета в FighterStats
        if (slotType == EquipmentSlotType.Weapon)
        {
            var playerStats = GameObject.FindGameObjectWithTag("Player").GetComponent<FighterStats>();
            if (playerStats != null)
            {
                playerStats.UpdateWeaponStats(item.minDamage, item.maxDamage, item.attackRange);
            }
        }
    }

    public void Unequip()
    {
        currentItem = null;
        UpdateVisual();
        if (PlayerProfileUI.Instance != null) PlayerProfileUI.Instance.OnEquipmentChanged();

        // Если сняли предмет из слота Оружия — возвращаем урон к кулакам
        if (slotType == EquipmentSlotType.Weapon)
        {
            var playerStats = GameObject.FindGameObjectWithTag("Player").GetComponent<FighterStats>();
            if (playerStats != null)
            {
                playerStats.UpdateWeaponStats(0, 0, 0); // 0 сбросит на базовый урон
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!eventData.dragging) Unequip();
    }

    private void UpdateVisual()
    {
        if (currentItem != null)
        {
            iconImage.sprite = currentItem.Icon;
            iconImage.enabled = true;
            iconImage.color = Color.white;

            // ДЕБАГ: Проверяем, видит ли скрипт Текст и сколько там предметов
            if (amountText != null)
            {
                int count = InventoryManager.Instance.GetItemCount(currentItem);
                Debug.Log($"[SlotUI] Обновляю слот {slotType}. Предмет: {currentItem.ItemName}, Насчитал: {count}");
                
                amountText.text = count.ToString(); // Просто показываем число, даже если 0
                amountText.gameObject.SetActive(true);
            }
            /*else
            {
                // Если в консоли появится это сообщение — значит ты забыл перетащить Текст в инспекторе!
                Debug.LogError($"[SlotUI] ОШИБКА: В слоте {slotType} не назначено поле Amount Text!");
            }*/
        }
        else
        {
            if (emptyIcon != null) iconImage.sprite = emptyIcon;
            iconImage.enabled = (emptyIcon != null);
            if (emptyIcon == null) iconImage.enabled = false;

            if (amountText != null) amountText.gameObject.SetActive(false);
        }
    }
}