using UnityEngine;

public class HarvestableResource : MonoBehaviour
{
    [Header("Main Settings")]
    [SerializeField] private ItemData mainResource;
    [SerializeField] private int maxHealth = 20;

    [Header("Rare Drop Settings")]
    [SerializeField] private ItemData rareResource;
    [Range(0, 100)]
    [SerializeField] private float rareDropChance = 20f;

    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void Harvest(int damageAmount)
    {
        if (currentHealth <= 0) return;

        int actualDamage = Mathf.Min(damageAmount, currentHealth);
        currentHealth -= actualDamage;

        // Ссылка на инвентарь для краткости
        var inventory = InventoryManager.Instance;
        if (inventory == null) 
        {
            Debug.LogError("InventoryManager не найден на сцене!");
            return;
        }

        // Логика дропа
        for (int i = 0; i < actualDamage; i++)
        {
            // 1. Основной ресурс
            if (mainResource != null)
                inventory.AddItem(mainResource, 1);

            // 2. Редкий ресурс (шанс)
            if (rareResource != null)
            {
                if (Random.Range(0f, 100f) < rareDropChance)
                {
                    inventory.AddItem(rareResource, 1);
                    Debug.Log($"<color=green>Удача! Выпал {rareResource.ItemName}</color>");
                }
            }
        }

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}