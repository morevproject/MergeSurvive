using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

public class SoulItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public Text soulNameText;
    public GameObject lockedOverlay; 
    
    // === ТОТ САМЫЙ МОСТИК ДЛЯ UIDraggableItem ===
    // Теперь скрипт перетаскивания не будет ругаться, он просто 
    // возьмет картинку из ResourceSlotUI.
    public Image icon 
    { 
        get 
        { 
            if (resourceSlotUI == null) resourceSlotUI = GetComponent<ResourceSlotUI>();
            return resourceSlotUI != null ? resourceSlotUI.iconImage : null; 
        } 
    }
    // ==============================================

    [Header("Data")]
    public ItemData itemData; 
    private Recipe myRecipe; 

    private bool isLocked = false;
    private int requiredTier = 1;

    public bool isTutorialLocked = false; 

    private UIDraggableItem dragComponent;
    private ResourceSlotUI resourceSlotUI; 

    private void Awake()
    {
        dragComponent = GetComponent<UIDraggableItem>();
        resourceSlotUI = GetComponent<ResourceSlotUI>(); 
    }
    
    public void Setup(Recipe recipe, bool locked)
    {
        myRecipe = recipe;
        requiredTier = recipe.requiredWorkbenchTier; 
        
        FindItemData(recipe);

        if (resourceSlotUI == null) resourceSlotUI = GetComponent<ResourceSlotUI>();
        
        if (resourceSlotUI != null)
        {
            resourceSlotUI.UpdateSlot(recipe.recipeName, recipe.icon, 0, itemData);
        }
        
        if (soulNameText != null) soulNameText.text = recipe.recipeName;

        SetLocked(locked);
    }

    private void FindItemData(Recipe recipe)
    {
        if (string.IsNullOrEmpty(recipe.outputResourceName)) return;

        itemData = Resources.Load<ItemData>("Items/" + recipe.outputResourceName);
        if (itemData == null) itemData = Resources.Load<ItemData>(recipe.outputResourceName);
        if (itemData == null)
        {
            var allItems = Resources.LoadAll<ItemData>(""); 
            itemData = allItems.FirstOrDefault(x => x.name == recipe.outputResourceName || x.ItemName == recipe.outputResourceName);
        }
    }

    public void SetLocked(bool locked)
    {
        // Раньше тут была визуальная блокировка. Теперь мы её ИГНОРИРУЕМ!
        isLocked = false; 
        
        // Навсегда прячем замочек
        if (lockedOverlay != null) lockedOverlay.SetActive(false); 
        
        // Возвращаем нормальный цвет иконке
        if (resourceSlotUI != null && resourceSlotUI.iconImage != null)
        {
            resourceSlotUI.iconImage.color = Color.white; 
        }

        // Всегда разрешаем перетаскивать (мёрджить) душу!
        if (dragComponent != null)
        {
            dragComponent.enabled = true; 
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Убрали старую проверку if (isLocked), теперь душа кликается всегда!

        if (isTutorialLocked) return; 

        if (CraftingDetailsUI.Instance != null)
        {
            CraftingDetailsUI.Instance.ShowRecipe(myRecipe);
            
            if (TutorialManager.Instance != null && itemData != null)
            {
                TutorialManager.Instance.OnRecipeClicked(itemData.ItemName);
            }
        }
        else
        {
            Debug.LogError("CraftingDetailsUI не найден на сцене! Проверь GameManager.");
        }
    }

    public void SetTutorialState(bool isInteractive)
    {
        isTutorialLocked = !isInteractive;
        
        if (dragComponent != null)
        {
            dragComponent.enabled = isInteractive && !isLocked;
        }
    }
}