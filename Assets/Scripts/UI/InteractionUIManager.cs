using System.Collections; 
using UnityEngine;
using UnityEngine.UI;

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance;
    
    [SerializeField] private GameObject interactButton; 
    [SerializeField] private Text interactText;         

    [Header("Настройки анимации")]
    public float handGatherDelay = 3.15f; 
    public float toolGatherDelay = 0.5f;  
    public float toolGatherFinishDelay = 0.6f; 

    private WorldInteractable currentInteractable;
    public bool isGathering = false; 

    private void Awake()
    {
        Instance = this;
        HideInteraction(); 
    }

    private void Update()
    {
        if (interactButton == null) return;

        if (currentInteractable != null)
        {
            bool isMenuOpen = UIManager.Instance != null && (UIManager.Instance.IsMenuOpen || UIManager.Instance.IsBossMode);
            bool isAttacking = PlayerCombatController.Instance != null && PlayerCombatController.Instance.IsAttacking;

            bool shouldBeVisible = !isMenuOpen && !isGathering && !isAttacking;

            if (interactButton.activeSelf != shouldBeVisible)
            {
                interactButton.SetActive(shouldBeVisible);
            }
        }
        else
        {
            if (interactButton.activeSelf)
            {
                interactButton.SetActive(false);
            }
        }
    }

    public void ShowInteraction(WorldInteractable interactable)
    {
        currentInteractable = interactable;
    }

    public void HideInteraction()
    {
        if (isGathering) return; 
        currentInteractable = null; 
    }

    public void OnInteractButtonPressed()
    {
        if (PlayerCombatController.Instance != null && PlayerCombatController.Instance.IsAttacking) return;

        if (currentInteractable != null && !isGathering)
        {
            StartCoroutine(GatherWithDelayRoutine());
        }
    }

    private IEnumerator GatherWithDelayRoutine()
    {
        if (isGathering) yield break; 
        
        isGathering = true; 

        if (InteractionHandler.Instance != null)
        {
            InteractionHandler.Instance.FaceCurrentTarget();
        }

        float currentDelay = handGatherDelay; 
        float finishDelay = 0f; 
        
        if (currentInteractable != null)
        {
            string tool = currentInteractable.requiredToolType;
            if (!string.IsNullOrEmpty(tool) && tool != "None")
            {
                currentDelay = toolGatherDelay;
                finishDelay = toolGatherFinishDelay; 
            }
        }

        PlayGatheringEffects();

        yield return new WaitForSeconds(currentDelay);

        if (InteractionHandler.Instance != null)
        {
            InteractionHandler.Instance.InteractWithCurrentTarget();
        }

        if (finishDelay > 0f)
        {
            yield return new WaitForSeconds(finishDelay);
        }
        
        if (InteractionHandler.Instance != null)
        {
            InteractionHandler.Instance.ForceRefresh();
        }

        // === ПРЕДОХРАНИТЕЛЬ ОТ "СОСТОЯНИЯ ГОНКИ" ===
        // Ждем 0.2 секунды, чтобы дать Аниматору и скриптам передвижения 
        // 100% времени на полное возвращение управления игроку ДО того, 
        // как мы снимем флаг isGathering и покажем кнопку атаки.
        yield return new WaitForSeconds(1.9f);
        // ============================================

        isGathering = false; 
    }

    private void PlayGatheringEffects()
    {
        var playerVisuals = PlayerEquipmentVisuals.Instance;
        var playerAnim = FindObjectOfType<PlayerAnimation>();

        string neededToolType = currentInteractable.requiredToolType;
        if (string.IsNullOrEmpty(neededToolType)) neededToolType = "None";

        string foundItemID = "";
        
        if (PlayerProfileUI.Instance != null && neededToolType != "None")
        {
            if (CheckSlotForTool(PlayerProfileUI.Instance.weaponSlot, neededToolType)) 
            {
                foundItemID = PlayerProfileUI.Instance.weaponSlot.currentItem.ItemName;
            }
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

        if (playerVisuals != null && !string.IsNullOrEmpty(foundItemID))
        {
            playerVisuals.ShowToolTemporarily(foundItemID, 1.5f);
        }

        if (playerAnim != null)
        {
            if (neededToolType == "Axe" || neededToolType == "Pickaxe" || neededToolType == "Shovel")
            {
                playerAnim.TriggerAttack(); 
            }
            else
            {
                playerAnim.TriggerGather(); 
            }
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