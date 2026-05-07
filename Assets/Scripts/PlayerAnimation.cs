using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    public Animator animator;      
    public Rigidbody rb;           
    public PlayerMovement playerMovement; // Нужен для получения максимальной скорости

    // Точные названия параметров из нового Аниматора
    private const string SPEED = "Speed";
    private const string COLLECT = "doCollect";
    private const string HIT = "doHit"; 
    private const string PICKAXE = "doPickaxe";
    private const string SHOOT = "doShoot";
    private const string DEATH = "doDeath";

    void Update()
    {
        if (animator == null || rb == null || playerMovement == null) return;

        // Получаем текущую горизонтальную скорость
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        
        // Превращаем скорость в значение от 0 до 1 для нашего Blend Tree
        float currentSpeed01 = Mathf.Clamp01(horizontalVelocity.magnitude / playerMovement.moveSpeed);

        // Передаем значение в Аниматор
        animator.SetFloat(SPEED, currentSpeed01);
    }

    public void TriggerCollect() { if (animator != null) animator.SetTrigger(COLLECT); }
    public void TriggerHit() { if (animator != null) animator.SetTrigger(HIT); }
    public void TriggerPickaxe() { if (animator != null) animator.SetTrigger(PICKAXE); }
    public void TriggerShoot() { if (animator != null) animator.SetTrigger(SHOOT); }
    public void TriggerDeath() { if (animator != null) animator.SetTrigger(DEATH); }

    // Добавь этот метод куда-нибудь вниз, перед последней закрывающей скобкой
    public void TriggerSmartAttack(ItemData itemInHand)
    {
        if (animator == null) return;

        // Если в руке пусто - бьем обычным ударом (кулаками)
        if (itemInHand == null)
        {
            TriggerHit();
            return;
        }

        // Если это лук
        if (itemInHand.requiresAmmo)
        {
            TriggerShoot();
            return;
        }

        // Вот она, железобетонная проверка по Enum из твоего ItemData!
        // (Если в твоем файле GameEnums.cs кирка и лопата называются чуть иначе, просто поправь названия тут)
        if (itemInHand.toolType == ToolType.Pickaxe || itemInHand.toolType == ToolType.Shovel)
        {
            TriggerPickaxe();
        }
        else
        {
            // Для дубинок, мечей, топоров и всего остального
            TriggerHit();
        }
    }
}