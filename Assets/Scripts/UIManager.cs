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
    
    [Header("Action Buttons (To Hide)")]
    public GameObject attackButtonObject; 
    public GameObject gatherButtonObject; 

    // === НОВАЯ ПЕРЕМЕННАЯ ДЛЯ АВАТАРА ===
    [Header("Avatar Settings")]
    public GameObject avatarUI; // Перетащи сюда свой LeftAvatar (или панель с ним) из Иерархии

    public static event Action<bool> OnInventoryToggled; 
    public static event Action<bool> OnCraftingToggled;  
    
    public bool IsMenuOpen { get; private set; } 
    public bool IsBossMode { get; private set; }

    void Awake()
    {
        Instance = this;
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
        
        SetMenuState(false);
        SetBossMode(false);
    }

    void Update()
    {
        if (IsBossMode) return; 

        if (Input.GetKeyDown(KeyCode.I)) ToggleInventory(null);
        if (Input.GetKeyDown(KeyCode.C)) ToggleCrafting(null);
        if (Input.GetKeyDown(KeyCode.Escape)) CloseAllMenus();
    }
    
    // --- УПРАВЛЕНИЕ РЕЖИМОМ БОССА ---
    public void SetBossMode(bool active)
    {
        IsBossMode = active;

        // 1. Кнопки Меню
        if (inventoryButton != null) inventoryButton.gameObject.SetActive(!active);
        if (craftingButton != null) craftingButton.gameObject.SetActive(!active);
        
        // 2. Закрываем окна
        if (active) CloseAllMenus();
        
        // 3. Прячем кнопки действий
        if (active)
        {
            if (attackButtonObject != null) attackButtonObject.SetActive(false);
            if (gatherButtonObject != null) gatherButtonObject.SetActive(false);
        }
        else
        {
            // Бой окончен — ищем деревья заново
            if (InteractionHandler.Instance != null) InteractionHandler.Instance.ForceRefresh();
        }

        // --- 4. КИНЕМАТОГРАФИЧНОСТЬ ---
        
        // А) Скрываем/Показываем системный курсор
        Cursor.visible = !active;
        Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;

        // Б) Блокируем/Разблокируем слоты экипировки (чтобы нельзя было нажать)
        if (PlayerProfileUI.Instance != null)
        {
            PlayerProfileUI.Instance.SetInputLock(active);
        }
    }
    // ----------------------------------------

    public void ToggleInventory(bool? state = null)
    {
        if (IsBossMode && state != false) return; 

        if (InventoryPanel != null)
        {
            bool isActive = state.HasValue ? state.Value : !InventoryPanel.activeSelf;
            if (InventoryPanel.activeSelf == isActive) return;

            InventoryPanel.SetActive(isActive);
            OnInventoryToggled?.Invoke(isActive);

            //   Триггер тутора
            if (isActive && TutorialManager.Instance != null) TutorialManager.Instance.TriggerUIOpened("Inventory");

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
        if (IsBossMode && state != false) return;

        if (CraftingPanel != null)
        {
            bool isActive = state.HasValue ? state.Value : !CraftingPanel.activeSelf;
            if (CraftingPanel.activeSelf == isActive) return;

            CraftingPanel.SetActive(isActive);
            OnCraftingToggled?.Invoke(isActive);

            //   Триггер тутора
            if (isActive && TutorialManager.Instance != null) TutorialManager.Instance.TriggerUIOpened("Crafting");

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
        
        SetMenuState(isInventoryOpen || isCraftingOpen);

        // === ИЗЯЩНАЯ ПРОВЕРКА ДЛЯ АВАТАРА ===
        if (avatarUI != null)
        {
            // Аватар включен всегда (!isCraftingOpen означает "пока Верстак НЕ открыт")
            avatarUI.SetActive(!isCraftingOpen);
        }
    }
    
    void SetMenuState(bool isOpen)
    {
        IsMenuOpen = isOpen;

        if (isOpen)
        {
            GameStateManager.Instance.SetState(GameStateManager.GameState.MenuOpen);
            
            if (attackButtonObject != null) attackButtonObject.SetActive(false);
            if (gatherButtonObject != null) gatherButtonObject.SetActive(false);
        }
        else
        {
            GameStateManager.Instance.SetState(GameStateManager.GameState.Playing);
            
            // Меню закрыто — ищем деревья заново
            if (InteractionHandler.Instance != null) InteractionHandler.Instance.ForceRefresh();

            //   Триггер тутора
            if (TutorialManager.Instance != null) TutorialManager.Instance.TriggerUIClosed();
        }
    }

    public void CloseAllMenus()
    {
        ToggleInventory(false);
        ToggleCrafting(false);
    }
}