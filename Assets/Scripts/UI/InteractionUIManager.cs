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
        string neededToolType = "Axe"; // По умолчанию топор (дерево)
        string targetName = currentInteractable.gameObject.name;
        
        // Проверка на КИРКУ
        if (targetName.Contains("Rock") || targetName.Contains("Stone") || targetName.Contains("Ore"))
        {
            neededToolType = "Pickaxe";
        }
        // --- ДОБАВЛЕНО: Проверка на ЛОПАТУ ---
        else if (targetName.Contains("Dirt") || targetName.Contains("Clay") || targetName.Contains("Mound") || targetName.Contains("Земл"))
        {
            neededToolType = "Shovel";
        }

        // 2. ПРОВЕРЯЕМ, ЕСТЬ ЛИ ЭТО У НАС (Ищем везде!)
        bool hasCorrectTool = false;
        
        if (PlayerProfileUI.Instance != null)
        {
            // А. Проверяем Слот ОРУЖИЯ (вдруг мы держим инструмент как оружие)
            if (CheckSlotForTool(PlayerProfileUI.Instance.weaponSlot, neededToolType))
            {
                hasCorrectTool = true;
            }
            // Б. Проверяем СЛОТЫ ИНСТРУМЕНТОВ
            else if (neededToolType == "Axe" && CheckSlotForTool(PlayerProfileUI.Instance.axeSlot, "Axe"))
            {
                hasCorrectTool = true;
            }
            else if (neededToolType == "Pickaxe" && CheckSlotForTool(PlayerProfileUI.Instance.pickaxeSlot, "Pickaxe"))
            {
                hasCorrectTool = true;
            }
            // --- ДОБАВЛЕНО: Проверка слота ЛОПАТЫ ---
            else if (neededToolType == "Shovel" && CheckSlotForTool(PlayerProfileUI.Instance.shovelSlot, "Shovel"))
            {
                hasCorrectTool = true;
            }
        }
        // Если профиля UI нет (тест), считаем, что инструмент есть
        else 
        {
            hasCorrectTool = true; 
        }

        // 3. ВИЗУАЛИЗАЦИЯ
        // Показываем инструмент, только если определили правильный тип
        if (playerVisuals != null)
        {
            // Если инструмента нет в слоте, можно либо не показывать ничего, 
            // либо показывать дефолтную (я оставил показ, чтобы видно было анимацию)
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