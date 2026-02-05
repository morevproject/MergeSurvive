using UnityEngine;
using System.Collections.Generic;

public class WorldInteractable : MonoBehaviour
{
    public static List<WorldInteractable> AllInteractables = new List<WorldInteractable>();

    [System.Serializable]
    public class DropRule
    {
        public ItemData item;             // Сам предмет
        [Range(0f, 1f)] public float dropChance = 1f; // Шанс (1 = 100%, 0.1 = 10%)
        public int amountPerDamagePoint = 1; // Сколько давать за 1 ед. урона (обычно 1)
    }

    [Header("General Settings")]
    public string objectName;
    public float maxHealth = 3f;
    public float currentHealth;

    [Header("Drops (Настройка добычи)")]
    // Теперь это список правил, а не просто предметов
    public List<DropRule> drops = new List<DropRule>();

    [Header("Visuals")]
    public GameObject selectionCircle;

    private void OnEnable()
    {
        AllInteractables.Add(this);
        currentHealth = maxHealth;
        if (selectionCircle != null) selectionCircle.SetActive(false);
    }

    private void OnDisable()
    {
        AllInteractables.Remove(this);
    }

    public void Interact(int damage)
    {
        TakeDamage(damage);
    }

    private void TakeDamage(float damage)
    {
        // 1. Рассчитываем, сколько РЕАЛЬНО здоровья мы сняли
        // Если у дерева 1 ХП, а мы бьем на 100, мы не должны получить 100 бревен.
        // Мы получаем только за то здоровье, которое было.
        float damageToProcess = Mathf.Min(damage, currentHealth);

        // 2. Выдаем ресурсы ЗА КАЖДУЮ единицу снятого здоровья
        // Округляем до целого (например, 3.5 урона = 3 просчета лута)
        int iterations = Mathf.FloorToInt(damageToProcess);

        if (InventoryManager.Instance != null && iterations > 0)
        {
            foreach (var rule in drops)
            {
                if (rule.item == null) continue;

                int totalAmountToGive = 0;

                // Просчитываем шанс для каждой единицы урона отдельно
                // (Чтобы при ударе на 5 урона у нас было 5 попыток выбить яблоко)
                for (int i = 0; i < iterations; i++)
                {
                    // Бросаем кубик
                    if (Random.value <= rule.dropChance)
                    {
                        totalAmountToGive += rule.amountPerDamagePoint;
                    }
                }

                if (totalAmountToGive > 0)
                {
                    InventoryManager.Instance.AddItem(rule.item, totalAmountToGive);
                    // Лог можно закомментировать, чтобы не спамил
                    Debug.Log($"[Dobycha] Получено {totalAmountToGive} шт. {rule.item.ItemName}");
                }
            }
        }

        // 3. Отнимаем здоровье
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // В методе Die больше не выдаем ресурсы, так как мы выдали их 
        // последним ударом в TakeDamage (потому что damageToProcess учел остаток ХП).

        var worldID = GetComponent<WorldObjectID>();
        if (worldID != null)
        {
            worldID.RegisterDestruction();
        }
        
        Destroy(gameObject);
    }

    public void ToggleHighlight(bool state)
    {
        if (selectionCircle != null) selectionCircle.SetActive(state);
    }
}