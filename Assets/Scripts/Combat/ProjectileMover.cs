using UnityEngine;

public class ProjectileMover : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 10f;
    public GameObject hitEffectPrefab; // Сюда потом кинем эффект взрыва

    private FighterStats target;
    private int damage;
    private bool hasTarget = false;

    // Этот метод вызовет Абилка при выстреле
    public void Initialize(FighterStats _target, int _damage)
    {
        target = _target;
        damage = _damage;
        hasTarget = true;
        
        // Самоуничтожение через 5 сек, если промазал
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        if (!hasTarget) return;

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // 1. Летим к центру цели (чуть выше ног)
        Vector3 targetPos = target.transform.position + Vector3.up * 1.0f;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
        transform.LookAt(targetPos);

        // 2. Проверка попадания
        if (Vector3.Distance(transform.position, targetPos) < 0.2f)
        {
            Hit();
        }
    }

    void Hit()
    {
        // Наносим урон
        if (target != null)
        {
            target.TakeDamage(damage);
        }

        // Эффект взрыва
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}