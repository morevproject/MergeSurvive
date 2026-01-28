using UnityEngine;
using UnityEngine.UI;

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance;
    
    [SerializeField] private GameObject interactButton; 
    [SerializeField] private Text interactText;         

    // 1. Храним ссылку на то, с чем сейчас взаимодействуем
    private WorldInteractable currentInteractable;

    private void Awake()
    {
        Instance = this;
        HideInteraction(); 
    }

    // Вызывается, когда мы подошли к ресурсу
    public void ShowInteraction(WorldInteractable interactable)
    {
        if (interactButton == null) return;

        // Запоминаем объект, чтобы потом проверить его имя/тег
        currentInteractable = interactable;

        interactButton.SetActive(true);
    }

    public void HideInteraction()
    {
        currentInteractable = null; // Очищаем ссылку
        if (interactButton != null) interactButton.SetActive(false);
    }

    // Эту функцию вызывает кнопка UI
    public void OnInteractButtonPressed()
    {
        // 1. Сначала ЗАПУСКАЕМ АНИМАЦИЮ (пока объект еще жив)
        if (currentInteractable != null)
        {
            PlayGatheringEffects();
        }

        // 2. Потом ЛОГИКУ (добавление в инвентарь / уничтожение объекта)
        InteractionHandler.Instance.InteractWithCurrentTarget();
    }

    // Метод для визуализации
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

        // 2. ПРОВЕРЯЕМ, ЕСТЬ ЛИ ЭТО У НАС (Ищем везде!)
        bool hasCorrectTool = false;
        
        if (PlayerProfileUI.Instance != null)
        {
            // А. Проверяем Слот ОРУЖИЯ (вдруг мы держим топор как оружие)
            if (CheckSlotForTool(PlayerProfileUI.Instance.weaponSlot, neededToolType))
            {
                hasCorrectTool = true;
            }
            // Б. Проверяем СЛОТЫ ИНСТРУМЕНТОВ (если они есть)
            // ВАЖНО: Убедись, что в PlayerProfileUI эти переменные называются именно так!
            else if (neededToolType == "Axe" && CheckSlotForTool(PlayerProfileUI.Instance.axeSlot, "Axe"))
            {
                hasCorrectTool = true;
            }
            else if (neededToolType == "Pickaxe" && CheckSlotForTool(PlayerProfileUI.Instance.pickaxeSlot, "Pickaxe"))
            {
                hasCorrectTool = true;
            }
        }

        // 3. ВИЗУАЛИЗАЦИЯ
        if (playerVisuals != null && hasCorrectTool)
        {
            playerVisuals.ShowToolTemporarily(neededToolType, 1.5f);
        }

        if (playerAnim != null)
        {
            playerAnim.TriggerGather();
        }
    }

    // Вспомогательный метод, чтобы не дублировать код
    private bool CheckSlotForTool(EquipmentSlotUI slot, string toolKeyword)
    {
        if (slot != null && slot.currentItem != null)
        {
            // Проверка: содержит ли имя предмета ключевое слово (например "Axe" в "Stone_Axe")
            if (slot.currentItem.ItemName.Contains(toolKeyword))
            {
                return true;
            }
        }
        return false;
    }
}