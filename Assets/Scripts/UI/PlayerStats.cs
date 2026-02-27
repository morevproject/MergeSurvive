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
    }
    
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

    public void AddBulbs(int amount)
    {
        HintBulbs += amount;
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

    // === ФИКС: ТЕПЕРЬ ДЛЯ БОЁВКИ УЧИТЫВАЕМ ТОЛЬКО СЛОТ ОРУЖИЯ ===
    public void RecalculateStats(ItemData weapon, ItemData armor, ItemData ammo, ItemData axe, ItemData pickaxe, ItemData shovel)
    {
        int minDmg = basePlayerDamage;
        int maxDmg = basePlayerDamage + 1; // Урон голыми руками: 1-2
        CurrentAttackRange = 2f; 

        // Игнорируем рабочие инструменты (axe, pickaxe, shovel) в общей стате урона.
        // Оружие дает урон только если оно лежит в слоте ОРУЖИЯ!
        if (weapon != null) 
        { 
            minDmg += weapon.minDamage; 
            maxDmg += weapon.maxDamage; 
            CurrentAttackRange = weapon.attackRange; 
        }

        TotalDamage = maxDmg; // Записываем максимальный урон для UI инвентаря

        TotalArmor = baseArmor; 
        if (armor != null) TotalArmor += armor.armorValue; 
        if (TotalArmor > 100) TotalArmor = 100;

        UpdateWeaponStats(minDmg, maxDmg, CurrentAttackRange);

        if (armorStatText != null)
            armorStatText.text = $"Броня\n<color=blue>{TotalArmor}%</color>";
        
        OnStatsUpdated?.Invoke();
    }

    // === ФИКС: БРОСОК КУБИКА ТОЛЬКО ДЛЯ ОРУЖИЯ ===
    public int RollCurrentDamage()
    {
        int min = basePlayerDamage;
        int max = basePlayerDamage + 1;

        if (PlayerProfileUI.Instance != null)
        {
            // Бьем ТОЛЬКО тем, что в слоте оружия. Никаких скрытых топоров!
            ItemData activeWeapon = PlayerProfileUI.Instance.weaponSlot.currentItem;
            
            if (activeWeapon != null)
            {
                min += activeWeapon.minDamage;
                max += activeWeapon.maxDamage;
            }
        }

        return UnityEngine.Random.Range(min, max + 1);
    }

    private void UpdateHealthUI()
    {
    }
}