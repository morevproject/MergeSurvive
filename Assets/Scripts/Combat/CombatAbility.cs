using UnityEngine;

[CreateAssetMenu(fileName = "New Ability", menuName = "Combat/Ability")]
public class CombatAbility : ScriptableObject
{
    public string abilityName = "Fireball";
    public int directDamage = 15;
    
    [Header("Visuals")]
    public GameObject projectilePrefab; // <--- СЮДА ПЕРЕТАЩИМ ПРЕФАБ ФАЕРБОЛА

    [Header("Status Effect")]
    public bool appliesBurn = true;
    public int burnDamage = 5;
    public int burnDuration = 3; 
    [Range(0, 100)] public int burnChance = 50;

    [Header("AI Rules")]
    public int cooldownRounds = 3;

    // --- ГЛАВНЫЙ МЕТОД ЗАПУСКА ---
    public virtual void Execute(FighterStats caster, FighterStats target)
    {
        Debug.Log($"[CombatAbility] {caster.unitName} использует {abilityName} на {target.unitName}!");

        if (projectilePrefab != null)
        {
            // 1. Пытаемся найти точку стрельбы (FirePoint) внутри иерархии босса
            // Ищем рекурсивно (вглубь), так как FirePoint лежит внутри VisualModel
            Transform firePoint = caster.transform.Find("VisualModel/FirePoint");
            
            // Если вдруг назвали по-другому или не нашли — используем запасной вариант (от центра)
            Vector3 spawnPos;
            if (firePoint != null)
            {
                spawnPos = firePoint.position;
            }
            else
            {
                // Старая логика (от груди)
                spawnPos = caster.transform.position + Vector3.up * 1.5f + caster.transform.forward * 0.5f;
            }

            // 2. Спавним
            GameObject projGO = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            ProjectileMover mover = projGO.GetComponent<ProjectileMover>();
            
            if (mover != null)
            {
                mover.Initialize(target, directDamage);
            }
        }
        else
        {
            target.TakeDamage(directDamage);
        }
    }
}