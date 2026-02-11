using UnityEngine;

// 1. Типы инструментов (для взаимодействия с миром)
public enum ToolType
{
    None = 0,
    Axe = 1,       // Топор
    Pickaxe = 2,   // Кирка
    Hammer = 3,    // Молот
    Weapon = 4,    // Оружие
    Shovel = 5     // Лопата
}

// 2. Уровни предметов (для проверки "мощности")
public enum ItemTier
{
    None = 0,
    Tier1_Stone = 1, // Каменный
    Tier2_Iron = 2,  // Железный
    Tier3_Steel = 3, // Стальной
    Tier4_Gold = 4   // Золотой
}

// 3. Категории предметов (для сортировки в инвентаре) — ВОТ ЭТОГО НЕ ХВАТАЛО
public enum ItemCategory
{
    Resource = 0, // Ресурсы (Камень, Дерево)
    Tool = 1,     // Инструменты (Топор, Кирка)
    Weapon = 2,   // Оружие (Меч, Лук)
    Armor = 3,     // Броня
    Food = 4,      // Еда
    Ammo = 5      // Боеприпасы
}