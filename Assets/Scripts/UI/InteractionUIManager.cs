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

    public void ShowInteraction(WorldInteractable interactable)
    {
        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.IsMenuOpen || UIManager.Instance.IsBossMode)
            {
                HideInteraction();
                return;
            }
        }
        
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
        var playerVisuals = PlayerEquipmentVisuals.Instance;
        var playerAnim = FindObjectOfType<PlayerAnimation>();

        // 1. ОПРЕДЕЛЯЕМ ТИП ИНСТРУМЕНТА (чтобы знать, где искать)
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

        // 2. ИЩЕМ КОНКРЕТНОЕ ИМЯ ПРЕДМЕТА В СЛОТАХ
        // (Нам нужно не просто "Pickaxe", а "Scrap_Pickaxe" или "Cyber_Pickaxe")
        string foundItemID = "";
        
        if (PlayerProfileUI.Instance != null)
        {
            // А. Проверяем оружие (вдруг держим инструмент как меч)
            if (CheckSlotForTool(PlayerProfileUI.Instance.weaponSlot, neededToolType)) 
            {
                foundItemID = PlayerProfileUI.Instance.weaponSlot.currentItem.ItemName;
            }
            // Б. Проверяем профильные слоты
            else if (neededToolType == "Axe" && CheckSlotForTool(PlayerProfileUI.Instance.axeSlot, "Axe")) 
            {
                foundItemID = PlayerProfileUI.Instance.axeSlot.currentItem.ItemName;
            }
            else if (neededToolType == "Pickaxe" && CheckSlotForTool(PlayerProfileUI.Instance.pickaxeSlot, "Pickaxe")) 
            {
                foundItemID = PlayerProfileUI.Instance.pickaxeSlot.currentItem.ItemName;
            }
            else if (neededToolType == "Shovel" && CheckSlotForTool(PlayerProfileUI.Instance.shovelSlot, "Shovel")) 
            {
                foundItemID = PlayerProfileUI.Instance.shovelSlot.currentItem.ItemName;
            }
        }
        // Fallback для тестов без UI (если нужно)
        else 
        {
            // Тут мы не знаем имя, поэтому ничего не покажем, но анимацию сыграем
        }

        // 3. ВИЗУАЛИЗАЦИЯ
        // Запускаем показ только если нашли КОНКРЕТНЫЙ предмет
        if (playerVisuals != null && !string.IsNullOrEmpty(foundItemID))
        {
            playerVisuals.ShowToolTemporarily(foundItemID, 1.5f);
        }

        // Анимацию руками играем всегда
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