using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    public enum TutorialStep
    {
        WaitNannyApproach,          // 0
        WelcomeDialogue,            // 1
        GatherWood,                 // 2
        GatherStoneDialogue,        // 3
        GatherStones,               // 4
        OpenInventoryDialogue,      // 5
        WaitForInventoryOpen,       // 6
        InventoryExplainedDialogue, // 7
        WaitForCraftingOpen,        // 8
        
        // --- ОБНОВЛЕННАЯ ЦЕПОЧКА КРАФТА ---
        CraftingOpenedDialogue,     // 9
        SimulateStoneDrag,          // 10
        WaitForSharpStoneRecipe,    // 11
        ArrowRecipeDialogue,        // 12 (Просим игрока догадаться самому)
        WaitForArrowRecipe,         // 13 (Ждем, пока скрафтит стрелу сам)
        SelectRecipeDialogue,       // 14
        WaitForRecipeClick,         // 15
        ExplainCraftButtonDialogue, // 16
        WaitForSharpStoneCraft,     // 17
        FinalCraftDialogue,         // 18
        WaitForArrowCraft,          // 19
        // ----------------------------------
        
        WaitForUIClosed,            // 20
        FinalDialogue,              // 21
        TransitionToMain            // 22
    }

    [Header("Current State")]
    public TutorialStep currentStep = TutorialStep.WaitNannyApproach;
    private bool hasCraftedSharpStone = false; 
    
    [Header("Scene Config")]
    public string tutorialSceneName = "Scene_00_Past";
    public string mainSceneName = "Main";

    [Header("Dialogues Config")]
    public List<DialogueNode> welcomeDialogue;
    public List<DialogueNode> gatherStoneDialogue;
    public List<DialogueNode> openInventoryDialogue;
    public List<DialogueNode> inventoryExplainedDialogue;
    
    [Header("Диалоги Крафта")]
    public List<DialogueNode> craftingOpenedDialogue;
    public List<DialogueNode> arrowRecipeDialogue;
    public List<DialogueNode> selectRecipeDialogue;
    public List<DialogueNode> explainCraftButtonDialogue;
    public List<DialogueNode> finalCraftDialogue;
    
    public List<DialogueNode> finalDialogue;

    [Header("Tutorial Targets (Поинтеры)")]
    public GameObject[] woodObjects;  
    public GameObject[] stoneObjects; 
    public GameObject pointerPrefab; 
    private List<GameObject> activePointers = new List<GameObject>();

    [Header("UI Control & Animation")]
    public GameObject dummyIconPrefab; 
    public Transform animationCanvas;  
    public GameObject craftButtonObj;  

    // --- НОВОЕ: Элементы, которые нужно спрятать в туториале ---
    [Header("Advanced UI (To Hide)")]
    public GameObject[] advancedUIElements;

    [Header("Transition")]
    public Image fadeOverlay; 
    public float fadeSpeed = 1.5f;

    private void Awake()
    {
        Instance = this; 
    }

    private IEnumerator Start()
    {
        if (SceneManager.GetActiveScene().name == tutorialSceneName)
        {
            yield return null; 

            if (PlayerPrefs.GetInt("NeedWipeSave", 0) == 1)
            {
                if (SaveManager.Instance != null) 
                {
                    SaveManager.Instance.DeleteSaveFile();
                }
                PlayerPrefs.SetInt("NeedWipeSave", 0); 
                PlayerPrefs.Save();
            }

            currentStep = (TutorialStep)PlayerPrefs.GetInt("TutorialStep", 0);
            hasCraftedSharpStone = PlayerPrefs.GetInt("TutorialCraftedStone", 0) == 1;

            ResumeCurrentStep(true);
        }
    }

    private void Update()
    {
        // --- ЖЕСТКАЯ БЛОКИРОВКА UI ---
        // Рубим на корню любые попытки других скриптов включить эти кнопки!
        if (advancedUIElements != null)
        {
            foreach (var uiObj in advancedUIElements)
            {
                if (uiObj != null && uiObj.activeSelf) 
                {
                    uiObj.SetActive(false);
                }
            }
        }
        // -----------------------------
        
        // --- НОВОЕ: ЖЕСТКАЯ БЛОКИРОВКА КНОПКИ КРАФТА ---
        // Прячем кнопку "Создать", начиная с открытия верстака и строго ДО шага,
        // где Няня разрешает крафтить Острый камень (WaitForSharpStoneCraft)
        if (currentStep >= TutorialStep.CraftingOpenedDialogue && currentStep < TutorialStep.WaitForSharpStoneCraft)
        {
            if (craftButtonObj != null && craftButtonObj.activeSelf)
            {
                craftButtonObj.SetActive(false);
            }
        }
        // ----------------------------------------------

        if (currentStep == TutorialStep.WaitForSharpStoneRecipe)
        {
            if (HasDiscoveredRecipe("Sharp_Stone")) 
            {
                AdvanceStep();
            }
        }
        else if (currentStep == TutorialStep.WaitForArrowRecipe)
        {
            if (HasDiscoveredRecipe("Stone_Arrow")) 
            {
                AdvanceStep();
            }
        }
        else if (currentStep == TutorialStep.WaitForSharpStoneCraft)
        {
            if (InventoryManager.Instance.GetTotalAmountByName("Sharp_Stone") >= 1)
            {
                hasCraftedSharpStone = true;
                PlayerPrefs.SetInt("TutorialCraftedStone", 1);
                AdvanceStep();
            }
        }
        else if (currentStep == TutorialStep.WaitForArrowCraft)
        {
            if (InventoryManager.Instance.GetTotalAmountByName("Stone_Arrow") >= 1) 
            {
                AdvanceStep();
            }
        }
    }

    public void AdvanceStep()
    {
        currentStep++;
        PlayerPrefs.SetInt("TutorialStep", (int)currentStep);
        PlayerPrefs.Save();
        ResumeCurrentStep(false); 
    }

    private void ResumeCurrentStep(bool isResuming = false)
    {
        UpdateUIButtonsVisibility();

        switch (currentStep)
        {
            case TutorialStep.WaitNannyApproach:
                SetInteractable(woodObjects, false);
                SetInteractable(stoneObjects, false);
                break;

            case TutorialStep.WelcomeDialogue:
                SetInteractable(woodObjects, false);
                SetInteractable(stoneObjects, false);
                PlayDialogue(welcomeDialogue);
                break;

            case TutorialStep.GatherWood:
                SetInteractable(stoneObjects, false);
                SetInteractable(woodObjects, true);
                TogglePointers(woodObjects, true);
                if (isResuming) PlayReminderDialogue(welcomeDialogue); 
                CheckGathering();
                break;

            case TutorialStep.GatherStoneDialogue:
                PlayDialogue(gatherStoneDialogue);
                break;

            case TutorialStep.GatherStones:
                SetInteractable(stoneObjects, true);
                TogglePointers(stoneObjects, true);
                if (isResuming) PlayReminderDialogue(gatherStoneDialogue); 
                CheckGathering();
                break;

            case TutorialStep.OpenInventoryDialogue:
                PlayDialogue(openInventoryDialogue);
                break;

            case TutorialStep.WaitForInventoryOpen:
                if (isResuming) PlayReminderDialogue(openInventoryDialogue); 
                break;

            case TutorialStep.InventoryExplainedDialogue:
                if (isResuming && UIManager.Instance != null) UIManager.Instance.ToggleInventory(true);
                PlayDialogue(inventoryExplainedDialogue);
                break;

            case TutorialStep.WaitForCraftingOpen:
                if (isResuming && UIManager.Instance != null) UIManager.Instance.ToggleInventory(true);
                if (isResuming) PlayReminderDialogue(inventoryExplainedDialogue);
                break;

            // --- НОВАЯ ЦЕПОЧКА КРАФТА ---
            case TutorialStep.CraftingOpenedDialogue:
                if (isResuming && UIManager.Instance != null) UIManager.Instance.ToggleCrafting(true);
                SetCraftButtonActive(false); 
                LockAllRecipesExcept(""); // Блокируем клики по всем рецептам (но без замков!)
                PlayDialogue(craftingOpenedDialogue);
                break;

            case TutorialStep.SimulateStoneDrag:
                // Принудительно открываем верстак при загрузке на этом шаге
                if (isResuming && UIManager.Instance != null) UIManager.Instance.ToggleCrafting(true);
                StartCoroutine(SimulateDragAnimation("Stone"));
                break;

            case TutorialStep.WaitForSharpStoneRecipe:
                if (isResuming && UIManager.Instance != null) UIManager.Instance.ToggleCrafting(true);
                // Ждет крафта Острого камня в Update()
                break;

            case TutorialStep.ArrowRecipeDialogue:
                if (isResuming && UIManager.Instance != null) UIManager.Instance.ToggleCrafting(true);
                PlayDialogue(arrowRecipeDialogue);
                break;

            case TutorialStep.WaitForArrowRecipe:
                if (isResuming && UIManager.Instance != null) UIManager.Instance.ToggleCrafting(true);
                // Ждет крафта Стрелы в Update()
                break;

            case TutorialStep.SelectRecipeDialogue:
                if (isResuming && UIManager.Instance != null) UIManager.Instance.ToggleCrafting(true);
                PlayDialogue(selectRecipeDialogue);
                break;

            case TutorialStep.WaitForRecipeClick:
                if (isResuming && UIManager.Instance != null) UIManager.Instance.ToggleCrafting(true);
                LockAllRecipesExcept("Sharp_Stone"); // Делаем кликабельным ТОЛЬКО Острый камень
                SetCraftButtonActive(false);
                break;

            case TutorialStep.ExplainCraftButtonDialogue:
                if (isResuming && UIManager.Instance != null) UIManager.Instance.ToggleCrafting(true);
                PlayDialogue(explainCraftButtonDialogue);
                break;

            case TutorialStep.WaitForSharpStoneCraft:
                if (isResuming && UIManager.Instance != null) UIManager.Instance.ToggleCrafting(true);
                SetCraftButtonActive(true); 
                break;

            case TutorialStep.FinalCraftDialogue:
                if (isResuming && UIManager.Instance != null) UIManager.Instance.ToggleCrafting(true);
                PlayDialogue(finalCraftDialogue);
                break;

            case TutorialStep.WaitForArrowCraft:
                if (isResuming && UIManager.Instance != null) UIManager.Instance.ToggleCrafting(true);
                LockAllRecipesExcept("Stone_Arrow"); // Разрешаем кликать на стрелы
                break;
            // -----------------------------------

            case TutorialStep.WaitForUIClosed:
                LockAllRecipesExcept("ALL"); 
                if (UIManager.Instance != null && !UIManager.Instance.IsMenuOpen) 
                {
                    AdvanceStep();
                }
                break;

            case TutorialStep.FinalDialogue:
                PlayDialogue(finalDialogue);
                break;

            case TutorialStep.TransitionToMain:
                StartCoroutine(FadeAndLoadScene());
                break;
        }
    }

    public void OnRecipeClicked(string recipeOutputName)
    {
        if (currentStep == TutorialStep.WaitForRecipeClick && recipeOutputName == "Sharp_Stone")
        {
            AdvanceStep(); 
        }
    }

    // --- УПРАВЛЕНИЕ UI И БЛОКИРОВКИ ---
    private void SetCraftButtonActive(bool isActive)
    {
        if (craftButtonObj != null) 
        {
            craftButtonObj.SetActive(isActive);
        }
    }

    private void LockAllRecipesExcept(string allowedResourceName)
    {
        SoulItemUI[] souls = FindObjectsOfType<SoulItemUI>();
        foreach (var soul in souls)
        {
            if (soul.itemData == null) continue;

            if (allowedResourceName == "ALL") 
            {
                soul.SetTutorialState(true); // Разрешаем всё
            }
            else 
            {
                // ИСПОЛЬЗУЕМ НЕВИДИМУЮ БЛОКИРОВКУ
                bool isAllowed = soul.itemData.ItemName == allowedResourceName;
                soul.SetTutorialState(isAllowed); 
            }
        }
    }

    private bool HasDiscoveredRecipe(string resourceName)
    {
        if (CraftingManager.Instance == null) return false;
        
        var discovered = CraftingManager.Instance.GetDiscoveredRecipes();
        return discovered.Any(r => r.outputResourceName == resourceName);
    }

    private void UpdateUIButtonsVisibility()
    {
        if (UIManager.Instance == null) return;
        
        bool canShowInventory = currentStep >= TutorialStep.OpenInventoryDialogue;
        if (UIManager.Instance.inventoryButton != null) 
        {
            UIManager.Instance.inventoryButton.gameObject.SetActive(canShowInventory);
        }
        
        bool canShowCrafting = currentStep >= TutorialStep.InventoryExplainedDialogue;
        if (UIManager.Instance.craftingButton != null) 
        {
            UIManager.Instance.craftingButton.gameObject.SetActive(canShowCrafting);
        }
    }

    private void SetInteractable(GameObject[] targets, bool isInteractable)
    {
        foreach (var obj in targets)
        {
            if (obj != null)
            {
                WorldInteractable interactable = obj.GetComponent<WorldInteractable>();
                if (interactable != null) 
                {
                    interactable.enabled = isInteractable;
                }
            }
        }
    }

    // --- АНИМАЦИЯ ПРИЗРАЧНЫХ ИКОНОК ---
    private IEnumerator SimulateDragAnimation(string targetResourceName)
    {
        SoulItemUI[] souls = FindObjectsOfType<SoulItemUI>();
        SoulItemUI targetSoul = souls.FirstOrDefault(s => s.itemData != null && s.itemData.ItemName == targetResourceName);

        if (targetSoul != null && CraftingUIManager.Instance != null && CraftingUIManager.Instance.craftingSlots.Count >= 2)
        {
            Vector3 startPos = targetSoul.transform.position;
            Sprite iconToFly = targetSoul.icon.sprite;

            yield return StartCoroutine(FlyIcon(iconToFly, startPos, CraftingUIManager.Instance.craftingSlots[0].transform.position, 1.0f));
            yield return new WaitForSeconds(0.2f);
            yield return StartCoroutine(FlyIcon(iconToFly, startPos, CraftingUIManager.Instance.craftingSlots[1].transform.position, 1.0f));
        }
        AdvanceStep(); 
    }

    private IEnumerator FlyIcon(Sprite sprite, Vector3 startPos, Vector3 endPos, float duration)
    {
        if (dummyIconPrefab == null || animationCanvas == null) yield break;
        
        GameObject iconObj = Instantiate(dummyIconPrefab, animationCanvas);
        iconObj.transform.position = startPos;
        
        Image img = iconObj.GetComponent<Image>();
        if (img != null) img.sprite = sprite;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 50f; 
            iconObj.transform.position = currentPos;
            yield return null;
        }

        float fadeDuration = 0.5f;
        elapsed = 0f;
        Color c = img.color;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = 1f - (elapsed / fadeDuration);
            img.color = c;
            yield return null;
        }
        
        Destroy(iconObj);
    }

    // --- ОБЩАЯ ЛОГИКА ТУТОРИАЛА ---
    private void PlayDialogue(List<DialogueNode> dialogue)
    {
        void OnDialogueDone() 
        { 
            DialogueManager.Instance.OnDialogueFinished -= OnDialogueDone; 
            AdvanceStep(); 
        }
        
        DialogueManager.Instance.OnDialogueFinished += OnDialogueDone;
        DialogueManager.Instance.StartDialogue(dialogue);
    }

    private void PlayReminderDialogue(List<DialogueNode> dialogue) 
    { 
        if (DialogueManager.Instance != null) 
        {
            DialogueManager.Instance.StartDialogue(dialogue); 
        }
    }

    private void TogglePointers(GameObject[] targets, bool isActive) 
    { 
        foreach(var p in activePointers) 
        { 
            if (p != null) Destroy(p); 
        }
        activePointers.Clear();
        
        if (isActive) 
        {
            foreach(var obj in targets) 
            {
                if(obj != null && obj.activeSelf) 
                {
                    Vector3 pointerPos = obj.transform.position;
                    Collider col = obj.GetComponent<Collider>();
                    
                    if (col != null) 
                    {
                        pointerPos = new Vector3(obj.transform.position.x, col.bounds.max.y + 1.5f, obj.transform.position.z);
                    }
                    else 
                    {
                        pointerPos += Vector3.up * 1.5f;
                    }
                    
                    GameObject newPointer = Instantiate(pointerPrefab, pointerPos, Quaternion.identity);
                    newPointer.transform.SetParent(obj.transform);
                    activePointers.Add(newPointer);
                }
            }
        }
    }

    public void CheckGathering() 
    {
        if (currentStep == TutorialStep.GatherWood && InventoryManager.Instance.GetTotalAmountByName("Wood") >= 1) 
        { 
            TogglePointers(woodObjects, false); 
            AdvanceStep(); 
        }
        else if (currentStep == TutorialStep.GatherStones && InventoryManager.Instance.GetTotalAmountByName("Stone") >= 2) 
        { 
            TogglePointers(stoneObjects, false); 
            AdvanceStep(); 
        }
    }

    public void TriggerUIOpened(string uiName) 
    {
        if (currentStep == TutorialStep.WaitForInventoryOpen && uiName == "Inventory") 
        {
            AdvanceStep();
        }
        else if (currentStep == TutorialStep.WaitForCraftingOpen && uiName == "Crafting") 
        {
            AdvanceStep();
        }
    }

    public void TriggerUIClosed() 
    { 
        if (currentStep == TutorialStep.WaitForUIClosed) 
        {
            AdvanceStep(); 
        }
    }

    public void CheckCrafting() 
    { 
        // Оставлено для обратной совместимости с InventoryManager
    }

    private IEnumerator FadeAndLoadScene() 
    {
        if (fadeOverlay != null) 
        {
            fadeOverlay.gameObject.SetActive(true);
            Color c = fadeOverlay.color;
            while (c.a < 1f) 
            { 
                c.a += Time.deltaTime * fadeSpeed; 
                fadeOverlay.color = c; 
                yield return null; 
            }
        }
        
        if (SaveManager.Instance != null) 
        {
            SaveManager.Instance.SaveGame(); 
        }
        
        PlayerPrefs.SetString("LastSavedScene", mainSceneName); 
        PlayerPrefs.Save();
        SceneManager.LoadScene(mainSceneName);
    }
}