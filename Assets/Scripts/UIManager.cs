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
    
    // === ÕŒ¬€≈ œ≈–≈Ã≈ÕÕ€≈ ƒÀﬂ —œ–¿…“Œ¬  ÕŒœŒ  ===
    [Header("Button Sprites")]
    public Sprite inventoryClosedSprite;
    public Sprite inventoryOpenSprite;
    
    public Sprite craftingClosedSprite;
    public Sprite craftingOpenSprite;
    // ===========================================

    [Header("Action Buttons (To Hide)")]
    public GameObject attackButtonObject; 
    public GameObject gatherButtonObject; 

    [Header("Avatar Settings")]
    public GameObject avatarUI; 

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
        
        // œËÌÛ‰ËÚÂÎ¸ÌÓ Ó·ÌÓ‚ÎˇÂÏ Í‡ÚËÌÍË Ì‡ ÒÚ‡ÚÂ
        UpdateGameState();
    }

    void Update()
    {
        if (IsBossMode) return; 

        if (Input.GetKeyDown(KeyCode.I)) ToggleInventory(null);
        if (Input.GetKeyDown(KeyCode.C)) ToggleCrafting(null);
        if (Input.GetKeyDown(KeyCode.Escape)) CloseAllMenus();
    }
    
    public void SetBossMode(bool active)
    {
        IsBossMode = active;

        if (inventoryButton != null) inventoryButton.gameObject.SetActive(!active);
        if (craftingButton != null) craftingButton.gameObject.SetActive(!active);
        
        if (active) CloseAllMenus();
        
        if (active)
        {
            if (attackButtonObject != null) attackButtonObject.SetActive(false);
            if (gatherButtonObject != null) gatherButtonObject.SetActive(false);
        }
        else
        {
            if (InteractionHandler.Instance != null) InteractionHandler.Instance.ForceRefresh();
        }

        Cursor.visible = !active;
        Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;

        if (PlayerProfileUI.Instance != null)
        {
            PlayerProfileUI.Instance.SetInputLock(active);
        }
    }

    public void ToggleInventory(bool? state = null)
    {
        if (IsBossMode && state != false) return; 

        if (InventoryPanel != null)
        {
            bool isActive = state.HasValue ? state.Value : !InventoryPanel.activeSelf;
            if (InventoryPanel.activeSelf == isActive) return;

            InventoryPanel.SetActive(isActive);
            OnInventoryToggled?.Invoke(isActive);

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

        if (avatarUI != null)
        {
            avatarUI.SetActive(!isCraftingOpen);
        }

        // === Ã¿√»ﬂ —Ã≈Õ€  ¿–“»ÕŒ  ===
        if (inventoryButton != null)
        {
            Image invImage = inventoryButton.GetComponent<Image>();
            if (invImage != null)
            {
                invImage.sprite = isInventoryOpen ? inventoryOpenSprite : inventoryClosedSprite;
            }
        }

        if (craftingButton != null)
        {
            Image craftImage = craftingButton.GetComponent<Image>();
            if (craftImage != null)
            {
                craftImage.sprite = isCraftingOpen ? craftingOpenSprite : craftingClosedSprite;
            }
        }
        // ============================
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
            
            if (InteractionHandler.Instance != null) InteractionHandler.Instance.ForceRefresh();

            if (TutorialManager.Instance != null) TutorialManager.Instance.TriggerUIClosed();
        }
    }

    public void CloseAllMenus()
    {
        ToggleInventory(false);
        ToggleCrafting(false);
    }
}