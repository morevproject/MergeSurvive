using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class HintManager : MonoBehaviour
{
    public static HintManager Instance;

    [Header("Settings")]
    public ItemData hintItem;

    [Header("Visuals")]
    public GameObject flyingIconPrefab; 
    public Transform animationParent; 
    public Transform soulsContentParent; 

    [Header("UI Main")]
    public Button hintButton;        
    public Text hintCountText;       
    public CanvasGroup craftingCanvasGroup; 

    [Header("UI Shop")]
    public GameObject shopPanel;     
    public Button buy1Btn; 
    public Button buy5Btn; 
    public Button buy10Btn;
    public Button closeShopBtn;

    private bool isAnimating = false;
    private List<GameObject> activeFlyingIcons = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        
        if (hintButton != null) hintButton.onClick.AddListener(OnHintClicked);
        if (closeShopBtn != null) closeShopBtn.onClick.AddListener(CloseShop);
        
        if (buy1Btn != null) buy1Btn.onClick.AddListener(() => BuyPack(1));
        if (buy5Btn != null) buy5Btn.onClick.AddListener(() => BuyPack(10));
        if (buy10Btn != null) buy10Btn.onClick.AddListener(() => BuyPack(100));

        if (shopPanel != null) shopPanel.SetActive(false);
    }

    void Start() { UpdateUI(); }

    void OnEnable() { if(PlayerStats.Instance) PlayerStats.Instance.OnStatsUpdated += UpdateUI; }
    
    // === БЕЗОПАСНЫЙ ОТКАТ (БЕЗ МАХИНАЦИЙ С ВАЛЮТОЙ) ===
    void OnDisable() 
    { 
        if(PlayerStats.Instance) PlayerStats.Instance.OnStatsUpdated -= UpdateUI; 

        if (isAnimating)
        {
            // Мы больше не возвращаем лампочку здесь, потому что мы её ЕЩЕ НЕ ТРАТИЛИ!

            foreach (var icon in activeFlyingIcons)
            {
                if (icon != null) Destroy(icon);
            }
            activeFlyingIcons.Clear();

            List<CraftingSlotUI> allSlots = GetCraftingSlots();
            foreach (var slot in allSlots) 
            {
                if (slot != null) slot.ClearSlot(); 
            }

            isAnimating = false;
            if (hintButton != null) hintButton.interactable = true;
            if (craftingCanvasGroup != null) craftingCanvasGroup.interactable = true;

            // Нам даже не нужно вызывать SaveProgress() при прерывании, 
            // так как мы ничего не меняли в инвентаре или статах!
            Debug.LogWarning("Верстак закрыт во время подсказки! Анимация прервана, лампочка сохранена.");
        }
    }

    void Update() { UpdateUI(); }

    private List<CraftingSlotUI> GetCraftingSlots()
    {
        if (CraftingUIManager.Instance != null && CraftingUIManager.Instance.craftingSlots != null)
        {
            return CraftingUIManager.Instance.craftingSlots;
        }
        if (CraftingPanel.Instance != null && CraftingPanel.Instance.slots != null)
        {
            return CraftingPanel.Instance.slots.ToList();
        }
        return new List<CraftingSlotUI>();
    }

    public void UpdateUI()
    {
        if (hintCountText == null || PlayerStats.Instance == null) return;
        int count = PlayerStats.Instance.HintBulbs;
        hintCountText.text = count.ToString();
    }

    public void OnHintClicked()
    {
        if (isAnimating) return; 
        if (CraftingUIManager.Instance != null && CraftingUIManager.Instance.IsCrafting) return; 

        isAnimating = true; 
        if (hintButton != null) hintButton.interactable = false;
        
        if (PlayerStats.Instance != null && PlayerStats.Instance.HintBulbs > 0)
        {
            TryUseHint();
        }
        else
        {
            OpenShop();
        }
    }

    private void TryUseHint()
    {
        Recipe recipe = FindKnownBasedRecipe();
        if (recipe != null) 
        {
            StartCoroutine(AnimateHintRoutine(recipe));
        }
        else 
        {
            Debug.Log("Нет доступных комбинаций!");
            isAnimating = false; 
            if (hintButton != null) hintButton.interactable = true;
        }
    }

    private Recipe FindKnownBasedRecipe()
    {
        var all = CraftingManager.Instance.allRecipes;
        var discoveredRecipes = CraftingManager.Instance.GetDiscoveredRecipes();
        List<Recipe> candidates = new List<Recipe>();

        foreach (var r in all)
        {
            if (discoveredRecipes.Contains(r)) continue; 
            if (r.isBaseResource) continue;  
            if (AreIngredientsUnlocked(r, discoveredRecipes)) candidates.Add(r);
        }

        if (candidates.Count > 0) return candidates[Random.Range(0, candidates.Count)];
        return null;
    }

    private bool AreIngredientsUnlocked(Recipe r, List<Recipe> knownRecipes)
    {
        foreach (var ing in r.requiredIngredients)
        {
            Recipe sourceRecipe = CraftingManager.Instance.GetRecipeByOutputName(ing.resourceName);
            if (sourceRecipe != null)
            {
                if (!knownRecipes.Contains(sourceRecipe)) return false;
            }
            else return false; 
        }
        return true;
    }

    IEnumerator AnimateHintRoutine(Recipe recipe)
    {
        if (craftingCanvasGroup != null) craftingCanvasGroup.interactable = false;

        // УБРАЛИ СПИСАНИЕ ЛАМПОЧКИ ОТСЮДА!

        ClearCraftingSlots();
        yield return new WaitForSeconds(0.1f);

        List<CraftingSlotUI> allSlots = GetCraftingSlots();
        List<CraftingSlotUI> availableSlots = new List<CraftingSlotUI>();
        
        foreach(var s in allSlots) {
             if(s != null && s.GetResourceInSlot() == null) availableSlots.Add(s);
        }

        foreach (var ing in recipe.requiredIngredients)
        {
            for (int i = 0; i < ing.amount; i++)
            {
                if (availableSlots.Count > 0)
                {
                    CraftingSlotUI targetSlot = availableSlots[0]; 
                    availableSlots.RemoveAt(0); 

                    Sprite iconToFly = null;
                    ItemData finalData = null;

                    Recipe sourceRecipe = CraftingManager.Instance.GetRecipeByOutputName(ing.resourceName);
                    if (sourceRecipe != null) iconToFly = sourceRecipe.icon;

                    if (iconToFly == null)
                    {
                        ItemData resItem = Resources.Load<ItemData>("Items/" + ing.resourceName);
                        if (resItem == null) resItem = Resources.Load<ItemData>(ing.resourceName);
                        if (resItem != null) { iconToFly = resItem.Icon; finalData = resItem; }
                    }

                    if (iconToFly == null && hintItem != null) iconToFly = hintItem.Icon; 

                    Vector3 startPos = GetSoulPositionOnWorkbench(ing.resourceName);

                    if (iconToFly != null)
                    {
                        yield return StartCoroutine(FlyIconToSlot(iconToFly, startPos, targetSlot, 0.6f));
                        
                        if (finalData == null) finalData = Resources.Load<ItemData>("Items/" + ing.resourceName);
                        if (finalData == null) finalData = InventoryManager.Instance.GetItemDataByName(ing.resourceName);

                        if (finalData != null) targetSlot.SetItem(finalData); 
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.2f);
        
        if (CraftingUIManager.Instance != null)
        {
            while (CraftingUIManager.Instance.IsCrafting)
            {
                yield return null;
            }
        }

        // === ИДЕАЛЬНЫЙ ФИНАЛ: ОПЛАТА ПРИ ПОЛУЧЕНИИ ===
        
        // 1. Открываем рецепт
        CraftingManager.Instance.DiscoverRecipe(recipe); 
        
        // 2. Списываем лампочку ТОЛЬКО СЕЙЧАС
        if (PlayerStats.Instance != null) PlayerStats.Instance.SpendBulbs(1);
        
        // 3. Сохраняем весь прогресс разом (И рецепт, и списание)
        SaveProgress();
        // ==============================================

        foreach (var slot in allSlots) 
        {
            if (slot != null) slot.ClearSlot(); 
        }

        if (craftingCanvasGroup != null) craftingCanvasGroup.interactable = true;
        if (hintButton != null) hintButton.interactable = true;
        
        isAnimating = false;
    }

    private Vector3 GetSoulPositionOnWorkbench(string resourceName)
    {
        Vector3 fallbackPos = hintButton.transform.position;
        if (soulsContentParent == null) return fallbackPos;

        foreach (Transform child in soulsContentParent)
        {
            SoulItemUI soulUI = child.GetComponent<SoulItemUI>();
            if (soulUI != null)
            {
                if (soulUI.itemData != null && soulUI.itemData.ItemName == resourceName) return child.position;
                if (soulUI.soulNameText != null && soulUI.soulNameText.text == resourceName) return child.position;
            }
        }
        return fallbackPos; 
    }

    public IEnumerator FlyIconToSlot(Sprite sprite, Vector3 startWorldPos, CraftingSlotUI targetSlot, float duration)
    {
        if (flyingIconPrefab == null || animationParent == null) yield break;

        GameObject iconObj = Instantiate(flyingIconPrefab, animationParent);
        activeFlyingIcons.Add(iconObj);

        iconObj.transform.localScale = Vector3.one; 
        iconObj.transform.position = startWorldPos; 
        
        Vector3 localPos = iconObj.transform.localPosition;
        localPos.z = 0;
        iconObj.transform.localPosition = localPos;

        Image img = iconObj.GetComponent<Image>();
        if (img != null) img.sprite = sprite;

        float elapsed = 0f;
        Vector3 endPos = targetSlot.transform.position;
        endPos.z = 0; 
        Vector3 realStartPos = iconObj.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t); 
            if (iconObj != null) iconObj.transform.position = Vector3.Lerp(realStartPos, endPos, t);
            yield return null;
        }
        
        if (iconObj != null) 
        {
            activeFlyingIcons.Remove(iconObj);
            Destroy(iconObj);
        }
    }

    private void ClearCraftingSlots()
    {
        List<CraftingSlotUI> allSlots = GetCraftingSlots();
        foreach (var slot in allSlots) {
            if (slot == null) continue; 
            
            if (slot.GetResourceInSlot() != null) {
                if (InventoryManager.Instance != null) {
                    InventoryManager.Instance.AddItem(slot.GetResourceInSlot(), 1); 
                }
                slot.ClearSlot(); 
            }
        }
    }

    private void OpenShop() { 
        if (shopPanel != null) shopPanel.SetActive(true); 
        isAnimating = false; 
    }
    
    public void CloseShop() 
    { 
        if (shopPanel != null) shopPanel.SetActive(false); 
        isAnimating = false; 
        if (hintButton != null) hintButton.interactable = true;
    }

    private void BuyPack(int amount)
    {
        if (PlayerStats.Instance != null) PlayerStats.Instance.AddBulbs(amount);
        SaveProgress();
        CloseShop();
    }

    private void SaveProgress() { if (SaveManager.Instance != null) SaveManager.Instance.SaveGame(); }
}