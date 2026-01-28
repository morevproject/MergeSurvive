using UnityEngine;
using UnityEngine.UI;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance; // Синглтон

    [Header("Inventory")]
    public GameObject InventoryPanel; 
    public Button inventoryButton;

    [Header("Crafting")]
    public GameObject CraftingPanel; 
    public Button craftingButton;

    // События
    public static event Action<bool> OnInventoryToggled; 
    public static event Action<bool> OnCraftingToggled;  

    void Awake()
    {
        // Инициализация Синглтона
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Настройка кнопок
        if (inventoryButton != null)
        {
            inventoryButton.onClick.RemoveAllListeners();
            inventoryButton.onClick.AddListener(() => ToggleInventory(null)); // null = переключить
        }
    
        if (craftingButton != null)
        {
            craftingButton.onClick.RemoveAllListeners();
            craftingButton.onClick.AddListener(() => ToggleCrafting(null));
        }

        // Скрываем панели при старте
        if (InventoryPanel != null) InventoryPanel.SetActive(false);
        if (CraftingPanel != null) CraftingPanel.SetActive(false);
    }

    void Update()
    {
        // Горячие клавиши
        if (Input.GetKeyDown(KeyCode.I)) ToggleInventory(null);
        if (Input.GetKeyDown(KeyCode.C)) ToggleCrafting(null);
        if (Input.GetKeyDown(KeyCode.Escape)) CloseAllMenus();
    }

    // state = true (открыть), false (закрыть), null (переключить)
    public void ToggleInventory(bool? state = null)
    {
        if (InventoryPanel != null)
        {
            bool isActive = state.HasValue ? state.Value : !InventoryPanel.activeSelf;
            
            // Если состояние не меняется — выходим
            if (InventoryPanel.activeSelf == isActive) return;

            InventoryPanel.SetActive(isActive);
            OnInventoryToggled?.Invoke(isActive);

            // Если открыли инвентарь — закрываем верстак
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
        if (CraftingPanel != null)
        {
            bool isActive = state.HasValue ? state.Value : !CraftingPanel.activeSelf;

            if (CraftingPanel.activeSelf == isActive) return;

            CraftingPanel.SetActive(isActive);
            OnCraftingToggled?.Invoke(isActive);

            // Если открыли верстак — закрываем инвентарь
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