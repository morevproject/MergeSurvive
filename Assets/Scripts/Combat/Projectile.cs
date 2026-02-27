using UnityEngine;

public class Projectile : MonoBehaviour
{
    private int damage;
    private float speed = 35f; 
    private Transform targetTransform;
    private Vector3 lastKnownPosition;

    public void Setup(int dmg, Transform target)
    {
        damage = dmg;
        targetTransform = target;
        
        if (targetTransform != null)
        {
            // Берем точку чуть выше центра, чтобы стрела летела в грудь, а не в пятки
            lastKnownPosition = targetTransform.position + Vector3.up * 1.0f;
        }
        else
        {
            lastKnownPosition = transform.position + transform.forward * 10f; 
        }
        
        Destroy(gameObject, 5f); 
    }

    void Update()
    {
        if (targetTransform != null)
        {
            lastKnownPosition = targetTransform.position + Vector3.up * 1.0f;
        }

        transform.LookAt(lastKnownPosition);
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Проверяем обычных роботов (открытый мир)
        BoarAI enemy = other.GetComponentInParent<BoarAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // 2. === ФИКС: ПРОВЕРЯЕМ БОССА ===
        BossAI boss = other.GetComponentInParent<BossAI>();
        if (boss != null)
        {
            // У босса здоровье лежит в FighterStats
            FighterStats bossStats = boss.GetComponent<FighterStats>();
            if (bossStats != null)
            {
                bossStats.TakeDamage(damage);
            }
            Destroy(gameObject);
            return;
        }

        // 3. Уничтожаем стрелу о стены и землю
        if (!other.isTrigger && other.gameObject.layer != LayerMask.NameToLayer("Player"))
        {
            Destroy(gameObject);
        }
    }
}