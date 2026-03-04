using UnityEngine;
using System.Collections.Generic;
using System.Collections; 

public class CraftingUIManager : MonoBehaviour
{
    public static CraftingUIManager Instance;

    [Header("UI References")]
    public List<CraftingSlotUI> craftingSlots; // <--- ÈÑÏĞÀÂËÅÍÎ ÒÓÒ
    public CraftingResultSlot resultSlot;    

    public bool IsCrafting { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void OnSlotUpdated()
    {
        if (IsCrafting) return;

        List<ItemData> ingredients = new List<ItemData>();
        bool hasItems = false;

        foreach (var slot in craftingSlots)
        {
            if (slot == null) continue;

            ItemData item = slot.GetResourceInSlot();

            if (item != null)
            {
                hasItems = true;
                ingredients.Add(item);
            }
        }

        if (!hasItems)
        {
            if (resultSlot != null) resultSlot.ClearSlot();
            return;
        }

        Recipe foundRecipe = CraftingManager.Instance.TryMergeItems(ingredients);

        if (foundRecipe != null)
        {
            StartCoroutine(ProcessDiscovery(foundRecipe));
        }
        else
        {
            if (resultSlot != null) resultSlot.ClearSlot();
        }
    }

    IEnumerator ProcessDiscovery(Recipe recipe)
    {
        IsCrafting = true;

        if (resultSlot != null) resultSlot.SetResult(recipe);

        yield return new WaitForSeconds(1.0f);

        CraftingManager.Instance.DiscoverRecipe(recipe);

        foreach (var slot in craftingSlots)
        {
            if (slot != null) slot.ClearSlot();
        }

        if (resultSlot != null) resultSlot.ClearSlot();

        IsCrafting = false;
        
        OnSlotUpdated();
    }

    // =========================================================
    // ÍÎÂÛÉ ÌÅÒÎÄ ÄËß ÊÍÎÏÊÈ "Î×ÈÑÒÈÒÜ"
    // =========================================================
    public void ClearAllSlots()
    {
        if (IsCrafting) return;

        foreach (var slot in craftingSlots)
        {
            if (slot != null)
            {
                slot.ClearSlot(); 
            }
        }

        if (resultSlot != null)
        {
            resultSlot.ClearSlot();
        }

        Debug.Log("UI Âåğñòàêà î÷èùåí ïî êíîïêå.");
    }
}