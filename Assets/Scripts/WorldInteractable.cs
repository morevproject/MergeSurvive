using UnityEngine;
using System.Collections.Generic;

public class WorldInteractable : MonoBehaviour
{
    public static List<WorldInteractable> AllInteractables = new List<WorldInteractable>();

    [System.Serializable]
    public class DropRule
    {
        public ItemData item;             
        [Range(0f, 1f)] public float dropChance = 1f; 
        public int amountPerDamagePoint = 1; 
    }

    [Header("General Settings")]
    public string objectName;
    public float maxHealth = 3f;
    public float currentHealth;

    [Header("Gathering Requirements (НОВОЕ)")]
    // Какой инструмент нужен: "None" (руки), "Axe", "Pickaxe", "Shovel"
    public string requiredToolType = "None"; 

    [Header("Drops")]
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
        float damageToProcess = Mathf.Min(damage, currentHealth);
        int iterations = Mathf.FloorToInt(damageToProcess);

        if (InventoryManager.Instance != null && iterations > 0)
        {
            foreach (var rule in drops)
            {
                if (rule.item == null) continue;

                int totalAmountToGive = 0;
                for (int i = 0; i < iterations; i++)
                {
                    if (Random.value <= rule.dropChance)
                    {
                        totalAmountToGive += rule.amountPerDamagePoint;
                    }
                }

                if (totalAmountToGive > 0)
                {
                    InventoryManager.Instance.AddItem(rule.item, totalAmountToGive);
                    Debug.Log($"[Dobycha] Получено {totalAmountToGive} шт. {rule.item.ItemName}");
                }
            }
        }

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
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