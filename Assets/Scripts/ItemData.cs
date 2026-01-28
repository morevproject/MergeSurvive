using UnityEngine;

public enum ItemType
{
    Resource,
    Tool,
    Weapon,
    Armor,
    Food,
    Ammo
}

// УДАЛЕНО: enum ToolType, так как он уже есть в GameEnums.cs

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("General Info")]
    public string ItemName;      // Внутренний ID (Apple, Stone). НЕ МЕНЯТЬ!
    public string DisplayName;   // Имя для игрока (Яблоко, Камень)
    [TextArea] public string Description; 
    public Sprite Icon;          
    public ItemType itemType;
    
    // Ссылка на Enum из файла GameEnums.cs
    [Header("Equipment Settings")]
    public ToolType toolType = ToolType.None;

    [Header("Ranged Settings")]
    public bool requiresAmmo; // Ставь галочку для Лука

    [Header("Stack Settings")]
    public bool IsStackable;     
    public int MaxStackSize = 99;

    [Header("Combat Stats")]
    public int minDamage = 1;
    public int maxDamage = 3;
    public float attackRange = 2f;

    [Header("Defense & Sustain")]
    public int armorValue;
    public int healAmount;      

    public string GetName()
    {
        return string.IsNullOrEmpty(DisplayName) ? ItemName : DisplayName;
    }

    public int GetRandomDamage()
    {
        return Random.Range(minDamage, maxDamage + 1);
    }
}