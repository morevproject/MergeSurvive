using UnityEngine;

public class InteractionHandler : MonoBehaviour
{
    public static InteractionHandler Instance;

    [Header("Settings")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private LayerMask interactableLayer;

    private WorldInteractable currentTarget;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        // Блокировка поиска, если открыто меню или идет бой
        if (UIManager.Instance != null && (UIManager.Instance.IsMenuOpen || UIManager.Instance.IsBossMode))
        {
            if (currentTarget != null) ClearTarget();
            return;
        }

        // --- ФИКС 3: ЗАМОРОЗКА РАДАРА ---
        // Если проигрывается анимация сбора, вообще не проверяем дистанцию!
        if (InteractionUIManager.Instance != null && InteractionUIManager.Instance.isGathering)
        {
            return;
        }

        if (currentTarget != null)
        {
            if (currentTarget.gameObject == null || !currentTarget.gameObject.activeInHierarchy)
            {
                ClearTarget();
            }
        }

        FindNearestInteractable();
    }

    public void ForceRefresh()
    {
        ClearTarget();
    }

    private void FindNearestInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRadius, interactableLayer);
        
        WorldInteractable nearest = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit == null || hit.gameObject == null || !hit.gameObject.activeInHierarchy) continue;

            WorldInteractable interactable = hit.GetComponent<WorldInteractable>();
            if (interactable == null) interactable = hit.GetComponentInParent<WorldInteractable>();

            if (interactable != null && interactable.isActiveAndEnabled)
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = interactable;
                }
            }
        }

        if (nearest != currentTarget)
        {
            currentTarget = nearest;

            if (currentTarget != null)
            {
                if (InteractionUIManager.Instance != null)
                    InteractionUIManager.Instance.ShowInteraction(currentTarget);
            }
            else
            {
                if (InteractionUIManager.Instance != null)
                    InteractionUIManager.Instance.HideInteraction();
            }
        }
    }

    // === НОВЫЙ МЕТОД ДЛЯ ПОВОРОТА К РЕСУРСУ ===
    public void FaceCurrentTarget()
    {
        if (currentTarget != null)
        {
            PlayerMovement pm = GetComponent<PlayerMovement>();
            if (pm != null && pm.characterModel != null)
            {
                // Поворачиваем только визуал, сохраняя высоту (чтобы не клевал носом)
                pm.characterModel.LookAt(new Vector3(currentTarget.transform.position.x, pm.characterModel.position.y, currentTarget.transform.position.z));
            }
            else
            {
                // Резервный вариант
                transform.LookAt(new Vector3(currentTarget.transform.position.x, transform.position.y, currentTarget.transform.position.z));
            }
        }
    }

    public void InteractWithCurrentTarget()
    {
        if (currentTarget != null)
        {
            // --- НОВАЯ ЛОГИКА: ПРОВЕРКА ИНСТРУМЕНТА ---
            string requiredTool = currentTarget.requiredToolType;
            bool canGather = true;

            // Если инструмент НУЖЕН (не "None")
            if (!string.IsNullOrEmpty(requiredTool) && requiredTool != "None")
            {
                canGather = false; // Сначала запрещаем, пока не найдем инструмент

                if (PlayerProfileUI.Instance != null)
                {
                    // Проверяем все возможные слоты: Оружие, Топор, Кирка, Лопата
                    if (CheckIfSlotHasTool(PlayerProfileUI.Instance.weaponSlot, requiredTool)) canGather = true;
                    else if (CheckIfSlotHasTool(PlayerProfileUI.Instance.axeSlot, requiredTool)) canGather = true;
                    else if (CheckIfSlotHasTool(PlayerProfileUI.Instance.pickaxeSlot, requiredTool)) canGather = true;
                    else if (CheckIfSlotHasTool(PlayerProfileUI.Instance.shovelSlot, requiredTool)) canGather = true;
                }
                else
                {
                    // Без UI (тесты) разрешаем
                    canGather = true;
                }
            }

            if (!canGather)
            {
                Debug.Log($"[Interaction] Нечем собрать! Требуется: {requiredTool}");
                // Тут можно вызвать UI подсказку: "Нужен топор!"
                return; // ПРЕРЫВАЕМ СБОР, УРОН НЕ НАНОСИТСЯ
            }
            // ------------------------------------------

            int damageToDeal = 1; 

            if (PlayerProfileUI.Instance != null)
            {
                string targetName = currentTarget.gameObject.name;
                ItemData toolItem = null;

                if (targetName.Contains("Clay") || targetName.Contains("Dirt") || targetName.Contains("Mound"))
                    toolItem = PlayerProfileUI.Instance.shovelSlot.currentItem;
                else if (targetName.Contains("Tree") || targetName.Contains("Wood") || targetName.Contains("Log"))
                    toolItem = PlayerProfileUI.Instance.axeSlot.currentItem;
                else if (targetName.Contains("Rock") || targetName.Contains("Stone") || targetName.Contains("Ore"))
                    toolItem = PlayerProfileUI.Instance.pickaxeSlot.currentItem;

                if (toolItem != null) damageToDeal = Random.Range(toolItem.minDamage, toolItem.maxDamage);
                else if (PlayerStats.Instance != null) damageToDeal = PlayerStats.Instance.TotalDamage;
            }

            Debug.Log($"[Interaction] Бьем {currentTarget.name}. Урон: {damageToDeal}");
            currentTarget.Interact(damageToDeal);

            if (currentTarget == null || currentTarget.gameObject == null || !currentTarget.gameObject.activeInHierarchy)
            {
                ClearTarget();
            }
        }
    }

    // Вспомогательный метод для проверки
    private bool CheckIfSlotHasTool(EquipmentSlotUI slot, string keyword)
    {
        if (slot != null && slot.currentItem != null)
        {
            return slot.currentItem.ItemName.Contains(keyword);
        }
        return false;
    }

    private void ClearTarget()
    {
        currentTarget = null;
        if (InteractionUIManager.Instance != null)
            InteractionUIManager.Instance.HideInteraction();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}