using UnityEngine;
using UnityEngine.UI;

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance;
    
    [SerializeField] private GameObject interactButton; 
    [SerializeField] private Text interactText;         

    private WorldInteractable currentInteractable;

    private void Awake()
    {
        Instance = this;
        HideInteraction(); 
    }

    // Вызывается, когда мы подошли к ресурсу
    public void ShowInteraction(WorldInteractable interactable)
    {
        // --- ФИКС: ЖЕЛЕЗНАЯ ЗАЩИТА ---
        // Если UIManager говорит, что сейчас бой или меню — ИГНОРИРУЕМ просьбу показать кнопку
        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.IsMenuOpen || UIManager.Instance.IsBossMode)
            {
                // На всякий случай убеждаемся, что она выключена
                HideInteraction();
                return;
            }
        }
        // -----------------------------
        
        if (interactButton == null) return;

        currentInteractable = interactable;
        interactButton.SetActive(true);
    }

    public void HideInteraction()
    {
        currentInteractable = null; 
        if (interactButton != null) interactButton.SetActive(false);
    }

    public void OnInteractButtonPressed()
    {
        if (currentInteractable != null)
        {
            PlayGatheringEffects();
        }

        InteractionHandler.Instance.InteractWithCurrentTarget();
    }

    private void PlayGatheringEffects()
    {
        var playerVisuals = FindObjectOfType<PlayerEquipmentVisuals>();
        var playerAnim = FindObjectOfType<PlayerAnimation>();

        // 1. ОПРЕДЕЛЯЕМ, ЧТО НУЖНО
        string neededToolType = "Axe"; 
        string targetName = currentInteractable.gameObject.name;
        
        if (targetName.Contains("Rock") || targetName.Contains("Stone") || targetName.Contains("Ore"))
        {
            neededToolType = "Pickaxe";
        }
        else if (targetName.Contains("Dirt") || targetName.Contains("Clay") || targetName.Contains("Mound") || targetName.Contains("Земл"))
        {
            neededToolType = "Shovel";
        }

        // 2. ПРОВЕРЯЕМ, ЕСТЬ ЛИ ЭТО У НАС
        bool hasCorrectTool = false;
        
        if (PlayerProfileUI.Instance != null)
        {
            if (CheckSlotForTool(PlayerProfileUI.Instance.weaponSlot, neededToolType)) hasCorrectTool = true;
            else if (neededToolType == "Axe" && CheckSlotForTool(PlayerProfileUI.Instance.axeSlot, "Axe")) hasCorrectTool = true;
            else if (neededToolType == "Pickaxe" && CheckSlotForTool(PlayerProfileUI.Instance.pickaxeSlot, "Pickaxe")) hasCorrectTool = true;
            else if (neededToolType == "Shovel" && CheckSlotForTool(PlayerProfileUI.Instance.shovelSlot, "Shovel")) hasCorrectTool = true;
        }
        else 
        {
            hasCorrectTool = true; 
        }

        // 3. ВИЗУАЛИЗАЦИЯ
        if (playerVisuals != null)
        {
            playerVisuals.ShowToolTemporarily(neededToolType, 1.5f);
        }

        if (playerAnim != null)
        {
            playerAnim.TriggerGather();
        }
    }

    private bool CheckSlotForTool(EquipmentSlotUI slot, string toolKeyword)
    {
        if (slot != null && slot.currentItem != null)
        {
            if (slot.currentItem.ItemName.Contains(toolKeyword))
            {
                return true;
            }
        }
        return false;
    }
}