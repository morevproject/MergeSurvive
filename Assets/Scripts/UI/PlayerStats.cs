using UnityEngine;
using System;
using TMPro;

public class PlayerStats : FighterStats
{
    public static PlayerStats Instance;

    [Header("UI References")]
    public TextMeshProUGUI armorStatText; 

    [Header("Progression")]
    public int Gears;
    public int WorkbenchTier = 1;
    public int HintBulbs = 0; 

    public int[] upgradeCosts = new int[] { 1, 3, 5 }; 

    [Header("Player Specifics")]
    public int basePlayerDamage = 1;
    
    public int TotalDamage { get; private set; }
    public int TotalArmor { get; private set; }
    public float CurrentAttackRange { get; private set; } = 2f;

    public event Action OnStatsUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        {
            Destroy(this); 
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        currentHealth = maxHealth;
        RecalculateStats(null, null, null, null, null, null); 
        UpdateHealthUI();

        // УДАЛИЛИ СТРОКУ, ВЫЗЫВАЮЩУЮ ОШИБКУ CS0070.
        // PlayerProfileUI сам обновит полоску в своем Start().
    }

    // --- БОЕВАЯ СИСТЕМА ---
    
    public override void TakeDamage(int damage)
    {
        float damageMultiplier = 1.0f - (TotalArmor / 100.0f);
        int finalDamage = Mathf.RoundToInt(damage * damageMultiplier);
        
        if (finalDamage < 1 && damage > 0 && TotalArmor < 100) finalDamage = 1;
        
        base.TakeDamage(finalDamage);
        OnStatsUpdated?.Invoke();
    }

    public override void Heal(int amount)
    {
        base.Heal(amount);
        OnStatsUpdated?.Invoke();
    }

    // --- ПРОГРЕССИЯ ---

    public void AddBulbs(int amount)
    {
        HintBulbs += amount;
        Debug.Log($"[PlayerStats] +{amount} Лампочек. Всего: {HintBulbs}");
        OnStatsUpdated?.Invoke();
    }

    public bool SpendBulbs(int amount)
    {
        if (HintBulbs >= amount)
        {
            HintBulbs -= amount;
            OnStatsUpdated?.Invoke();
            return true;
        }
        return false;
    }

    public void AddGears(int amount)
    {
        Gears += amount;
        if (SoulListUI.Instance != null) SoulListUI.Instance.RefreshUI();
    }

    public bool TryUpgradeWorkbench()
    {
        int nextCost = GetNextUpgradeCost();
        if (nextCost == -1) return false;

        if (Gears >= nextCost)
        {
            Gears -= nextCost;
            WorkbenchTier++;
            if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
            return true;
        }
        return false;
    }

    public int GetNextUpgradeCost()
    {
        int index = WorkbenchTier - 1;
        if (index >= upgradeCosts.Length) return -1;
        return upgradeCosts[index];
    }

    public void LoadProgress(int savedHp, int savedGears, int savedTier, int savedBulbs)
    {
        currentHealth = savedHp;
        Gears = savedGears;
        WorkbenchTier = (savedTier > 0) ? savedTier : 1;
        HintBulbs = savedBulbs;
        
        UpdateHealthUI();
        OnStatsUpdated?.Invoke();
    }

    public void RecalculateStats(ItemData weapon, ItemData armor, ItemData ammo, ItemData axe, ItemData pickaxe, ItemData shovel)
    {
        TotalDamage = basePlayerDamage;
        TotalArmor = baseArmor; 
        CurrentAttackRange = 2f; 

        if (weapon != null) { TotalDamage += weapon.GetRandomDamage(); CurrentAttackRange = weapon.attackRange; }
        else if (axe != null) { TotalDamage += axe.GetRandomDamage(); CurrentAttackRange = axe.attackRange; }
        else if (pickaxe != null) { TotalDamage += pickaxe.GetRandomDamage(); CurrentAttackRange = pickaxe.attackRange; }
        else if (shovel != null) { TotalDamage += shovel.GetRandomDamage(); CurrentAttackRange = shovel.attackRange; }

        if (armor != null) TotalArmor += armor.armorValue; 
        if (TotalArmor > 100) TotalArmor = 100;

        UpdateWeaponStats(TotalDamage, TotalDamage, CurrentAttackRange);

        if (armorStatText != null)
            armorStatText.text = $"Броня\n<color=blue>{TotalArmor}%</color>";
        
        OnStatsUpdated?.Invoke();
    }

    private void UpdateHealthUI()
    {
        // UI обновляется через события FighterStats
    }
}