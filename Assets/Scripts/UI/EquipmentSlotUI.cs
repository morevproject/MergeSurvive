using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum EquipmentSlotType
{
    Weapon, Armor, Ammo, ToolAxe, ToolPickaxe, ToolShovel
}

public class EquipmentSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Settings")]
    public EquipmentSlotType slotType;
    public Image iconImage;     // В ИНСПЕКТОРЕ СНИМИ RAYCAST TARGET!
    public Sprite emptyIcon;
    public Text amountText; 
    public Canvas canvas; 

    [HideInInspector] public ItemData currentItem;

    private GameObject draggingObject;
    private CanvasGroup canvasGroup; 

    private void Awake()
    {
        currentItem = null;
    }

    private void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChange += UpdateVisual;
        }
        
        // Это запустит проверку. Так как currentItem == null, 
        // скрипт подтвердит отключение RaycastTarget.
        Invoke(nameof(UpdateVisual), 0.1f);
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChange -= UpdateVisual;
        }
    }

    // --- ЛОГИКА ПЕРЕТАСКИВАНИЯ ---

    public void OnBeginDrag(PointerEventData eventData) 
    { 
        // 1. ЗАЩИТА (Страховка)
        // Если галочка RaycastTarget выключена в инспекторе, этот метод ДАЖЕ НЕ ВЫЗОВЕТСЯ.
        // Но если вдруг вызовется — мы мягко выходим.
        if (currentItem == null) return;
        
        // Если мы видим пустую иконку — тоже выходим.
        if (emptyIcon != null && iconImage.sprite == emptyIcon) return;
        
        if (canvas == null) return;
        
        // --- СОЗДАНИЕ ВИЗУАЛА ---
        // Раз мы дошли сюда, значит предмет точно есть и Raycast был включен скриптом.
        
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
        iconImage.enabled = true; 
        UpdateVisual();
    }

    // --- ЛОГИКА ПРИЕМА ПРЕДМЕТА ---

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

    private bool ValidateItem(ItemData item)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Weapon: return item.itemType == ItemType.Weapon;
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
        if (slotType == EquipmentSlotType.Ammo)
        {
            currentItem = item;
            UpdateVisual();
            if (PlayerProfileUI.Instance != null) PlayerProfileUI.Instance.OnEquipmentChanged();
            return; 
        }

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
            var playerStats = GameObject.FindGameObjectWithTag("Player").GetComponent<FighterStats>();
            if (playerStats != null)
            {
                playerStats.UpdateWeaponStats(item.minDamage, item.maxDamage, item.attackRange);
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
            var playerStats = GameObject.FindGameObjectWithTag("Player").GetComponent<FighterStats>();
            if (playerStats != null) playerStats.UpdateWeaponStats(0, 0, 0); 
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!eventData.dragging) Unequip();
    }

    // --- ОБНОВЛЕНИЕ ВИЗУАЛА ---
    public void UpdateVisual()
    {
        if (currentItem != null)
        {
            iconImage.sprite = currentItem.Icon;
            iconImage.enabled = true;
            iconImage.color = Color.white;
            
            // ВАЖНО: Включаем клики, только если есть предмет
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
            // ВАЖНО: Выключаем клики для пустоты
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