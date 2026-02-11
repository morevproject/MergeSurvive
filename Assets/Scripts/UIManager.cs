using UnityEngine;
using UnityEngine.UI;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance; 

    [Header("Inventory")]
    public GameObject InventoryPanel; 
    public Button inventoryButton;

    [Header("Crafting")]
    public GameObject CraftingPanel; 
    public Button craftingButton;

    public static event Action<bool> OnInventoryToggled; 
    public static event Action<bool> OnCraftingToggled;  

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (inventoryButton != null)
        {
            inventoryButton.onClick.RemoveAllListeners();
            inventoryButton.onClick.AddListener(() => ToggleInventory(null)); 
        }
    
        if (craftingButton != null)
        {
            craftingButton.onClick.RemoveAllListeners();
            craftingButton.onClick.AddListener(() => ToggleCrafting(null));
        }

        if (InventoryPanel != null) InventoryPanel.SetActive(false);
        if (CraftingPanel != null) CraftingPanel.SetActive(false);
    }

    void Update()
    {
        // Блокируем горячие клавиши в бою!
        if (CombatManager.Instance != null && CombatManager.Instance.IsBattleInProgress)
        {
             return; 
        }

        if (Input.GetKeyDown(KeyCode.I)) ToggleInventory(null);
        if (Input.GetKeyDown(KeyCode.C)) ToggleCrafting(null);
        if (Input.GetKeyDown(KeyCode.Escape)) CloseAllMenus();
    }

    public void ToggleInventory(bool? state = null)
    {
        // --- ФИКС БАГА 2: Блокировка в бою ---
        // Разрешаем только ЗАКРЫТЬ инвентарь (state == false), 
        // но не разрешаем его открывать или переключать, если идет бой.
        if (CombatManager.Instance != null && CombatManager.Instance.IsBattleInProgress)
        {
            if (state != false) // Пытаемся открыть или переключить (по кнопке)
            {
                Debug.LogWarning("Нельзя открыть инвентарь во время боя с боссом!");
                // Тут можно вызвать UI текст: ShowWarningMessage("Драться надо, а не в рюкзаке копаться!");
                return;
            }
        }
        // ------------------------------------

        if (InventoryPanel != null)
        {
            bool isActive = state.HasValue ? state.Value : !InventoryPanel.activeSelf;
            if (InventoryPanel.activeSelf == isActive) return;

            InventoryPanel.SetActive(isActive);
            OnInventoryToggled?.Invoke(isActive);

            if (isActive && CraftingPanel != null) 
            {
                CraftingPanel.SetActive(false);
                OnCraftingToggled?.Invoke(false);
            }
            UpdateGameState();
        }
    }

    public void ToggleCrafting(bool? state = null)
    {
        // --- ФИКС БАГА 2: Блокировка в бою ---
        if (CombatManager.Instance != null && CombatManager.Instance.IsBattleInProgress)
        {
            if (state != false) 
            {
                Debug.LogWarning("Верстак недоступен во время битвы!");
                return;
            }
        }
        // ------------------------------------

        if (CraftingPanel != null)
        {
            bool isActive = state.HasValue ? state.Value : !CraftingPanel.activeSelf;
            if (CraftingPanel.activeSelf == isActive) return;

            CraftingPanel.SetActive(isActive);
            OnCraftingToggled?.Invoke(isActive);

            if (isActive && InventoryPanel != null)
            {
                InventoryPanel.SetActive(false);
                OnInventoryToggled?.Invoke(false);
            }
            UpdateGameState();
        }
    }

    void UpdateGameState()
    {
        bool isInventoryOpen = (InventoryPanel != null && InventoryPanel.activeSelf);
        bool isCraftingOpen = (CraftingPanel != null && CraftingPanel.activeSelf);

        if (isInventoryOpen || isCraftingOpen)
            GameStateManager.Instance.SetState(GameStateManager.GameState.MenuOpen);
        else
            GameStateManager.Instance.SetState(GameStateManager.GameState.Playing);
    }

    public void CloseAllMenus()
    {
        ToggleInventory(false);
        ToggleCrafting(false);
    }
}