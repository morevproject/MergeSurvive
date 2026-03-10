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

    // === УМНОЕ ОБНОВЛЕНИЕ КНОПКИ ===
    private void Update()
    {
        if (interactButton == null) return;

        // Если у нас есть цель для сбора (мы стоим рядом с ресурсом)
        if (currentInteractable != null)
        {
            // Проверяем все блокирующие факторы
            bool isMenuOpen = UIManager.Instance != null && (UIManager.Instance.IsMenuOpen || UIManager.Instance.IsBossMode);
            bool isAttacking = PlayerCombatController.Instance != null && PlayerCombatController.Instance.IsAttacking;

            // Кнопка должна быть видна ТОЛЬКО если мы не в меню, не собираем и не атакуем
            bool shouldBeVisible = !isMenuOpen && !isGathering && !isAttacking;

            // Включаем или выключаем кнопку только если её текущее состояние не совпадает с нужным
            if (interactButton.activeSelf != shouldBeVisible)
            {
                interactButton.SetActive(shouldBeVisible);
            }
        }
        else
        {
            // Если цели нет, железобетонно прячем кнопку
            if (interactButton.activeSelf)
            {
                interactButton.SetActive(false);
            }
        }
    }

    public void ShowInteraction(WorldInteractable interactable)
    {
        // Просто запоминаем цель. Метод Update сам включит кнопку, когда будет безопасно!
        currentInteractable = interactable;
    }

    public void HideInteraction()
    {
        if (isGathering) return; 
        
        // Забываем цель. Update сам спрячет кнопку.
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
        
        // Кнопка спрячется сама через Update, так как isGathering стало true

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

        isGathering = false; // Как только закончили, Update снова покажет кнопку!
        
        if (InteractionHandler.Instance != null)
        {
            InteractionHandler.Instance.ForceRefresh();
        }
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