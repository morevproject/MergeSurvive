using UnityEngine;

public class InteractionHandler : MonoBehaviour
{
    public static InteractionHandler Instance;

    [Header("Settings")]
    [SerializeField] private float interactionRadius = 2f; // Радиус поиска
    [SerializeField] private LayerMask interactableLayer;   // Слой ресурсов

    private WorldInteractable currentTarget; // То, что сейчас нашли

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        // 1. ПРОВЕРКА НА МЕРТВУЮ ЦЕЛЬ (Самое важное!)
        // Если цель была, но её уничтожили (null) или выключили (activeInHierarchy == false)
        if (currentTarget != null)
        {
            if (currentTarget.gameObject == null || !currentTarget.gameObject.activeInHierarchy)
            {
                ClearTarget(); // Мгновенно убираем кнопку
            }
        }

        FindNearestInteractable();
    }

    private void FindNearestInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRadius, interactableLayer);
        
        WorldInteractable nearest = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            // Пропускаем удаленные или выключенные объекты
            if (hit == null || hit.gameObject == null || !hit.gameObject.activeInHierarchy) continue;

            WorldInteractable interactable = hit.GetComponent<WorldInteractable>();
            if (interactable == null) interactable = hit.GetComponentInParent<WorldInteractable>();

            // Доп. проверка: скрипт должен быть включен (enabled)
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

        // Логика переключения
        if (nearest != currentTarget)
        {
            currentTarget = nearest;

            if (currentTarget != null)
            {
                // Нашли новую цель -> Показываем кнопку
                if (InteractionUIManager.Instance != null)
                    InteractionUIManager.Instance.ShowInteraction(currentTarget);
            }
            else
            {
                // Целей нет -> Прячем кнопку
                if (InteractionUIManager.Instance != null)
                    InteractionUIManager.Instance.HideInteraction();
            }
        }
    }

    // Этот метод вызывает кнопка "Собрать"
    // Этот метод вызывает кнопка "Собрать"
    public void InteractWithCurrentTarget()
    {
        if (currentTarget != null)
        {
            // 1. Считаем реальный урон
            int damageToDeal = 1; // По умолчанию (если статов нет)
            
            if (PlayerStats.Instance != null)
            {
                damageToDeal = PlayerStats.Instance.TotalDamage;
            }

            // 2. Бьем ресурса/врага сильным ударом
            currentTarget.Interact(damageToDeal);
            
            // ПРИНУДИТЕЛЬНАЯ ПРОВЕРКА ПОСЛЕ УДАРА
            if (currentTarget == null || currentTarget.gameObject == null || !currentTarget.gameObject.activeInHierarchy)
            {
                ClearTarget();
            }
        }
    }

    // Вспомогательный метод для очистки
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