using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CraftingSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public Image iconImage;     
    public Sprite defaultSprite; 

    [Header("Data")]
    public ItemData currentItem;

    void Start()
    {
        UpdateSlotUI();
    }

    // === Î×ÈÑÒÊÀ ÑËÎÒÀ ÏÎ ÒÀÏÓ ===
    public void OnPointerClick(PointerEventData eventData)
    {
        // === ÁËÎÊÈĞÓÅÌ ĞÓ×ÍÓŞ Î×ÈÑÒÊÓ ÂÎ ÂĞÅÌß ÎÒÊĞÛÒÈß ===
        if (CraftingUIManager.Instance != null && CraftingUIManager.Instance.IsCrafting) return;

        if (currentItem != null)
        {
            ClearSlot();
        }
    }

    public void SetItem(ItemData newItem)
    {
        // === ÁËÎÊÈĞÓÅÌ ÏÎßÂËÅÍÈÅ ÍÎÂÛÕ ÏĞÅÄÌÅÒÎÂ ===
        // Åñëè èãğîê ñïèäğàíèò, äóøà ïğîñòî îòñêî÷èò è íå ïîïàäåò â ñëîò
        if (CraftingUIManager.Instance != null && CraftingUIManager.Instance.IsCrafting) return;

        currentItem = newItem;
        UpdateSlotUI();
        NotifyManager();
    }

    public void ClearSlot()
    {
        currentItem = null;
        UpdateSlotUI();
        NotifyManager();
    }

    private void NotifyManager()
    {
        if (CraftingUIManager.Instance != null)
        {
            CraftingUIManager.Instance.OnSlotUpdated();
        }
        else if (CraftingPanel.Instance != null)
        {
            CraftingPanel.Instance.CheckCrafting();
        }
    }

    public ItemData GetResourceInSlot()
    {
        return currentItem;
    }

    private void UpdateSlotUI()
    {
        if (iconImage == null) return;

        if (currentItem != null)
        {
            iconImage.sprite = currentItem.Icon;
            iconImage.color = Color.white;
            iconImage.enabled = true;
        }
        else
        {
            if (defaultSprite != null)
            {
                iconImage.sprite = defaultSprite;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }
    }
}