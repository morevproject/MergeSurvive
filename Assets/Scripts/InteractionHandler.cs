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
        // --- ФИКС 1: БЛОКИРОВКА ПОИСКА ---
        // Если открыто меню или идет бой с боссом — сбрасываем цель и не ищем новую
        if (UIManager.Instance != null && (UIManager.Instance.IsMenuOpen || UIManager.Instance.IsBossMode))
        {
            if (currentTarget != null) ClearTarget();
            return;
        }
        // ---------------------------------

        // Проверка: не исчезла ли цель (уничтожена/выключена)
        if (currentTarget != null)
        {
            if (currentTarget.gameObject == null || !currentTarget.gameObject.activeInHierarchy)
            {
                ClearTarget();
            }
        }

        FindNearestInteractable();
    }

    // --- ФИКС 2: МЕТОД ДЛЯ СБРОСА ---
    // Вызывается из UIManager, когда меню закрывается, чтобы мгновенно найти дерево рядом
    public void ForceRefresh()
    {
        ClearTarget();
    }
    // -------------------------------

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

    public void InteractWithCurrentTarget()
    {
        if (currentTarget != null)
        {
            int damageToDeal = 1; // Базовый урон рукой

            // Если есть доступ к профилю игрока (слотам)
            if (PlayerProfileUI.Instance != null)
            {
                string targetName = currentTarget.gameObject.name;
                ItemData toolItem = null;

                // 1. Проверяем, что именно мы бьем и какой инструмент нужен

                // ЛОПАТА (Глина, Земля, Кучи) -> Лезем в слот лопаты
                if (targetName.Contains("Clay") || targetName.Contains("Dirt") || targetName.Contains("Mound"))
                {
                    toolItem = PlayerProfileUI.Instance.shovelSlot.currentItem;
                }
                // ТОПОР (Дерево) -> Лезем в слот топора
                else if (targetName.Contains("Tree") || targetName.Contains("Wood") || targetName.Contains("Log"))
                {
                    toolItem = PlayerProfileUI.Instance.axeSlot.currentItem;
                }
                // КИРКА (Камень, Руда) -> Лезем в слот кирки
                else if (targetName.Contains("Rock") || targetName.Contains("Stone") || targetName.Contains("Ore"))
                {
                    toolItem = PlayerProfileUI.Instance.pickaxeSlot.currentItem;
                }

                // 2. Если нашли правильный инструмент в слоте — берем ЕГО урон
                if (toolItem != null)
                {
                    damageToDeal = Random.Range(toolItem.minDamage, toolItem.maxDamage);
                }
                // 3. Если инструмента нет, берем общую силу (например, оружие в руке)
                else if (PlayerStats.Instance != null)
                {
                    damageToDeal = PlayerStats.Instance.TotalDamage;
                }
            }

            // Отладочный вывод
            Debug.Log($"[Interaction] Бьем {currentTarget.name}. Урон: {damageToDeal}");

            // Наносим удар
            currentTarget.Interact(damageToDeal);

            // Проверка после удара (вдруг ресурс уничтожен)
            if (currentTarget == null || currentTarget.gameObject == null || !currentTarget.gameObject.activeInHierarchy)
            {
                ClearTarget();
            }
        }
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