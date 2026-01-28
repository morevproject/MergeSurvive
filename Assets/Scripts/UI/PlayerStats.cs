using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Base Stats")]
    public int baseDamage = 1; // Урон кулаком
    public int baseArmor = 0;  // Голый персонаж

    // Итоговые статы (База + Экипировка)
    public int TotalDamage { get; private set; }
    public int TotalArmor { get; private set; }

    public float CurrentAttackRange { get; private set; } = 2f;

    // События для UI
    public event Action<float> OnHealthChanged; // float для слайдера (0.0 - 1.0)
    public event Action OnStatsUpdated;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        currentHealth = maxHealth;
        // ИСПРАВЛЕНИЕ: Передаем 5 аргументов (все null), так как на старте мы голые
        RecalculateStats(null, null, null, null, null); 
        UpdateHealthUI();
    }

    public void TakeDamage(int damage)
    {
        int finalDamage = Mathf.Max(damage - TotalArmor, 1);
        
        currentHealth -= finalDamage;
        if (currentHealth < 0) currentHealth = 0;

        Debug.Log($"Игрок получил {finalDamage} урона. ХП: {currentHealth}/{maxHealth}");
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        float healthPercent = (float)currentHealth / maxHealth;
        OnHealthChanged?.Invoke(healthPercent);
    }

    // Обновленная сигнатура метода (5 аргументов)
    public void RecalculateStats(ItemData weapon, ItemData armor, ItemData ammo, ItemData axe, ItemData pickaxe)
    {
        TotalDamage = baseDamage;
        TotalArmor = baseArmor;
        CurrentAttackRange = 2f; // Базовая дальность (руки/инструмент)

        // Считаем урон и ДАЛЬНОСТЬ
        if (weapon != null) 
        {
            TotalDamage += weapon.GetRandomDamage();
            CurrentAttackRange = weapon.attackRange; // Берем дальность оружия
        }
        else if (axe != null) 
        {
            TotalDamage += axe.GetRandomDamage();
            CurrentAttackRange = axe.attackRange;
        }
        else if (pickaxe != null) 
        {
            TotalDamage += pickaxe.GetRandomDamage();
            CurrentAttackRange = pickaxe.attackRange;
        }

        if (armor != null) TotalArmor += armor.armorValue;
        
        Debug.Log($"Статы: Урон {TotalDamage}, Броня {TotalArmor}, Дальность {CurrentAttackRange}");
        OnStatsUpdated?.Invoke();
    }

    private void Die()
    {
        Debug.Log("Игрок погиб! (Тут будет экран Game Over)");
    }
}