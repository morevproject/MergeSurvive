using System.Collections; // Обязательно для работы корутин
using UnityEngine;
using UnityEngine.UI;

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance;
    
    [SerializeField] private GameObject interactButton; 
    [SerializeField] private Text interactText;         

    [Header("Настройки анимации")]
    public float handGatherDelay = 3.15f; // Для сбора руками (мелкий)
    public float toolGatherDelay = 0.5f;  // Для удара топором/киркой (мужик)

    private WorldInteractable currentInteractable;
    public bool isGathering = false; // Защита от спама кнопкой

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
        
        // Если уже собираем предмет, не переключаемся на другой
        if (isGathering) return; 
        
        if (interactButton == null) return;

        currentInteractable = interactable;
        interactButton.SetActive(true);
    }

    public void HideInteraction()
    {
        // Блокируем сброс таргета, если анимация уже началась
        if (isGathering) return; 
        
        currentInteractable = null; 
        if (interactButton != null) interactButton.SetActive(false);
    }

    public void OnInteractButtonPressed()
    {
        // Проверяем, что таргет есть и мы еще не в процессе сбора
        if (currentInteractable != null && !isGathering)
        {
            StartCoroutine(GatherWithDelayRoutine());
        }
    }

    // ТА САМАЯ МАГИЯ ЗАДЕРЖКИ
    private IEnumerator GatherWithDelayRoutine()
    {
        isGathering = true; 
        
        if (interactButton != null) interactButton.SetActive(false);

        if (InteractionHandler.Instance != null)
        {
            InteractionHandler.Instance.FaceCurrentTarget();
        }

        // === УМНАЯ ЗАДЕРЖКА ===
        // По умолчанию берем долгую (для рук)
        float currentDelay = handGatherDelay; 
        
        // Если это ресурс, который требует инструмент (не "None") — берем быструю!
        if (currentInteractable != null)
        {
            string tool = currentInteractable.requiredToolType;
            if (!string.IsNullOrEmpty(tool) && tool != "None")
            {
                currentDelay = toolGatherDelay;
            }
        }

        PlayGatheringEffects();

        // Ждем ровно столько, сколько требует текущее действие
        yield return new WaitForSeconds(currentDelay);

        if (InteractionHandler.Instance != null)
        {
            InteractionHandler.Instance.InteractWithCurrentTarget();
        }

        isGathering = false;
        
        if (InteractionHandler.Instance != null)
        {
            InteractionHandler.Instance.ForceRefresh();
        }
    }

    private void PlayGatheringEffects()
    {
        var playerVisuals = PlayerEquipmentVisuals.Instance;
        var playerAnim = FindObjectOfType<PlayerAnimation>();

        // 1. БЕРЕМ ТИП ИНСТРУМЕНТА ИЗ НАСТРОЕК САМОГО ОБЪЕКТА!
        // (Больше не гадаем по имени. Что указано в префабе ресурса, то и требуем)
        string neededToolType = currentInteractable.requiredToolType;
        if (string.IsNullOrEmpty(neededToolType)) neededToolType = "None";

        // 2. ИЩЕМ КОНКРЕТНОЕ ИМЯ ПРЕДМЕТА В СЛОТАХ (если инструмент вообще нужен)
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

        // 3. ВИЗУАЛИЗАЦИЯ ИНСТРУМЕНТА
        if (playerVisuals != null && !string.IsNullOrEmpty(foundItemID))
        {
            playerVisuals.ShowToolTemporarily(foundItemID, 1.5f);
        }

        // 4. УМНАЯ АНИМАЦИЯ (Бьем или Нагибаемся)
        if (playerAnim != null)
        {
            if (neededToolType == "Axe" || neededToolType == "Pickaxe" || neededToolType == "Shovel")
            {
                playerAnim.TriggerAttack(); // Взмах инструментом
            }
            else
            {
                playerAnim.TriggerGather(); // Наклон (для "None")
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