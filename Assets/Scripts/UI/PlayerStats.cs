using UnityEngine;
using System;
using TMPro; // 1. Добавили пространство имен для Текста

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    [Header("UI References")]
    // Сюда перетащишь свой TextMeshProUGUI под полоской ХП
    public TextMeshProUGUI armorStatText; 

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Base Stats")]
    public int baseDamage = 1; 
    public int baseArmor = 0; 

    // Итоговые статы
    public int TotalDamage { get; private set; }
    public int TotalArmor { get; private set; } // Теперь это ПРОЦЕНТЫ (0-100)

    public float CurrentAttackRange { get; private set; } = 2f;

    // События
    public event Action<float> OnHealthChanged;
    public event Action OnStatsUpdated;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        currentHealth = maxHealth;
        // На старте мы голые
        RecalculateStats(null, null, null, null, null, null); 
        UpdateHealthUI();
    }

    public void TakeDamage(int damage)
    {
        // 2. НОВАЯ ФОРМУЛА: Считаем процент блокировки
        // Если TotalArmor = 10, то damage * 0.1
        float damageReduction = damage * (TotalArmor / 100f);
        
        // Вычитаем процент из урона
        int finalDamage = Mathf.RoundToInt(damage - damageReduction);
        
        // Минимум 1 урон должен пройти (если удар был сильным), но если 0, то 0
        if (finalDamage < 1 && damage > 0) finalDamage = 1;

        Debug.Log($"Удар: {damage}. Броня: {TotalArmor}%. Итог: {finalDamage}");
        
        currentHealth -= finalDamage;
        if (currentHealth < 0) currentHealth = 0;

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
        // Если у тебя есть ссылки на слайдер внутри этого скрипта, можно обновить их тут.
        // Но сейчас мы используем событие:
        float healthPercent = (float)currentHealth / maxHealth;
        OnHealthChanged?.Invoke(healthPercent);
    }

    // Обновленная сигнатура (6 аргументов)
    public void RecalculateStats(ItemData weapon, ItemData armor, ItemData ammo, ItemData axe, ItemData pickaxe, ItemData shovel)
    {
        TotalDamage = baseDamage;
        TotalArmor = baseArmor;
        CurrentAttackRange = 2f; 

        // 1. Расчет Урона и Дальности (Приоритет: Оружие -> Топор -> Кирка -> Лопата)
        if (weapon != null) 
        {
            TotalDamage += weapon.GetRandomDamage();
            CurrentAttackRange = weapon.attackRange;
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
        else if (shovel != null) // <--- НОВАЯ ЛОГИКА ДЛЯ ЛОПАТЫ
        {
            TotalDamage += shovel.GetRandomDamage();
            CurrentAttackRange = shovel.attackRange;
        }

        // 2. Расчет Брони (Проценты)
        if (armor != null) 
        {
            TotalArmor += armor.armorValue; // Берем % защиты из предмета
        }

        // Ограничение (макс 100%)
        if (TotalArmor > 100) TotalArmor = 100;

        // 3. Обновляем текст на экране (ВСЕГДА)
        if (armorStatText != null)
        {
            // Мы убрали проверку > 0. Теперь 0% тоже отображается.
            // <color=blue> окрасит только цифры
            armorStatText.text = $"Броня\n<color=blue>{TotalArmor}%</color>";
        }
        
        Debug.Log($"Stats Recalculated: DMG {TotalDamage}, ARM {TotalArmor}%, RNG {CurrentAttackRange}");
        OnStatsUpdated?.Invoke();
    }

    private void Die()
    {
        Debug.Log("Игрок погиб!");
    }
}