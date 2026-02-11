using UnityEngine;
using System;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    [Header("UI References")]
    public TextMeshProUGUI armorStatText; 

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    // --- Прогрессия Верстака & Валюта ---
    public int Gears { get; private set; }
    public int WorkbenchTier { get; private set; } = 1;
    public int HintBulbs { get; private set; } = 0; // <--- НОВОЕ: Лампочки

    // Цена апгрейда: с 1 на 2 ур = 1 шестеренка, с 2 на 3 = 3 шестеренки
    public int[] upgradeCosts = new int[] { 1, 3, 5 }; 
    // ----------------------------------

    [Header("Base Stats")]
    public int baseDamage = 1; 
    public int baseArmor = 0; 

    public int TotalDamage { get; private set; }
    public int TotalArmor { get; private set; }
    public float CurrentAttackRange { get; private set; } = 2f;

    public event Action<float> OnHealthChanged;
    public event Action OnStatsUpdated;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        RecalculateStats(null, null, null, null, null, null); 
        UpdateHealthUI();
    }

    // --- МЕТОДЫ ДЛЯ ЛАМПОЧЕК (НОВЫЕ) ---
    public void AddBulbs(int amount)
    {
        HintBulbs += amount;
        Debug.Log($"[PlayerStats] +{amount} Лампочек. Всего: {HintBulbs}");
        OnStatsUpdated?.Invoke(); // Обновляем UI
    }

    public bool SpendBulbs(int amount)
    {
        if (HintBulbs >= amount)
        {
            HintBulbs -= amount;
            Debug.Log($"[PlayerStats] Потрачено {amount} Лампочек. Осталось: {HintBulbs}");
            OnStatsUpdated?.Invoke();
            return true;
        }
        return false;
    }
    // -----------------------------------

    // --- МЕТОДЫ ДЛЯ ВЕРСТАКА ---
    public void AddGears(int amount)
    {
        Gears += amount;
        Debug.Log($"[PlayerStats] +{amount} Шестерёнок. Всего: {Gears}");
        
        if (SoulListUI.Instance != null) 
        {
            SoulListUI.Instance.RefreshUI();
        }
    }

    public bool TryUpgradeWorkbench()
    {
        int nextCost = GetNextUpgradeCost();
        if (nextCost == -1) return false; // Макс уровень

        if (Gears >= nextCost)
        {
            Gears -= nextCost;
            WorkbenchTier++;
            Debug.Log($"[PlayerStats] Верстак улучшен до уровня {WorkbenchTier}!");
            
            // Сохраняем прогресс сразу
            if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
            
            return true;
        }
        return false;
    }

    public int GetNextUpgradeCost()
    {
        int index = WorkbenchTier - 1;
        if (index >= upgradeCosts.Length) return -1; // Больше нет апгрейдов
        return upgradeCosts[index];
    }

    // Обновленный метод загрузки (добавили bulbs)
    public void LoadProgress(int savedHp, int savedGears, int savedTier, int savedBulbs)
    {
        currentHealth = savedHp;
        Gears = savedGears;
        WorkbenchTier = (savedTier > 0) ? savedTier : 1;
        HintBulbs = savedBulbs; // Загружаем лампочки
        
        UpdateHealthUI();
        OnStatsUpdated?.Invoke();
    }
    // ----------------------------------

    public void TakeDamage(int damage)
    {
        float damageReduction = damage * (TotalArmor / 100f);
        int finalDamage = Mathf.RoundToInt(damage - damageReduction);
        if (finalDamage < 1 && damage > 0) finalDamage = 1;
        
        currentHealth -= finalDamage;
        if (currentHealth < 0) currentHealth = 0;

        UpdateHealthUI();

        if (currentHealth <= 0) Die();
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

    public void RecalculateStats(ItemData weapon, ItemData armor, ItemData ammo, ItemData axe, ItemData pickaxe, ItemData shovel)
    {
        TotalDamage = baseDamage;
        TotalArmor = baseArmor;
        CurrentAttackRange = 2f; 

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
        else if (shovel != null)
        {
            TotalDamage += shovel.GetRandomDamage();
            CurrentAttackRange = shovel.attackRange;
        }

        if (armor != null) 
        {
            TotalArmor += armor.armorValue; 
        }
        if (TotalArmor > 100) TotalArmor = 100;

        if (armorStatText != null)
        {
            armorStatText.text = $"Броня\n<color=blue>{TotalArmor}%</color>";
        }
        
        OnStatsUpdated?.Invoke();
    }

    private void Die()
    {
        Debug.Log("Игрок погиб!");
    }
}