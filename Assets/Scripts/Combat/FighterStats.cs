using UnityEngine;
using System; 
using System.Collections;

public class FighterStats : MonoBehaviour
{
    public event Action<int> OnHealthChanged;

    [Header("Base Stats")]
    public string unitName = "Unit";
    public int maxHealth = 100;
    
    [Header("Combat Stats")]
    public int minDamage = 1;
    public int maxDamage = 2;
    public float attackRange = 2.0f; 
    
    [Header("Defense")]
    public int baseArmor = 0; 

    [Header("Current State")]
    public int currentHealth;
    public bool isDead = false;

    private void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

    public int GetDamage()
    {
        return UnityEngine.Random.Range(minDamage, maxDamage + 1);
    }

    public void UpdateWeaponStats(int min, int max, float range) 
    {
        if (max <= 0) { minDamage = 1; maxDamage = 2; attackRange = 2.0f; }
        else { minDamage = min; maxDamage = max; attackRange = range > 0 ? range : 2.0f; }
    }

    // Метод теперь чистый и универсальный.
    // Для Игрока: сюда приходит уже уменьшенный урон из PlayerStats.
    // Для Мобов: сюда приходит полный урон, и мы вычитаем baseArmor.
    public virtual void TakeDamage(int incomingDamage)
    {
        if (isDead) return;

        int finalDamage = incomingDamage;

        // Если это НЕ игрок (обычный моб), применяем простую плоскую броню
        // (Для игрока baseArmor обычно 0, так что это не повредит)
        finalDamage = Mathf.Max(0, finalDamage - baseArmor);

        currentHealth -= finalDamage;

        // ВИЗУАЛ
        if (finalDamage > 0) DamagePopup.Create(transform.position + Vector3.up * 2f, finalDamage);
        else DamagePopup.Create(transform.position + Vector3.up * 2f, 0);

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public virtual void Heal(int amount)
    {
        if (isDead) return;
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

    protected virtual void Die()
    {
        isDead = true;
    }
}