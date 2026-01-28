using UnityEngine;

public class Projectile : MonoBehaviour
{
    private int damage;
    private float speed = 20f;
    private float lifeTime = 1f; // Чтобы стрела не летела вечно

    public void Setup(int dmg, Vector3 targetPosition)
    {
        damage = dmg;
        
        // Поворачиваем стрелу носом к цели
        transform.LookAt(targetPosition);
        
        // Уничтожаем через 5 сек, если ни в кого не попала
        Destroy(gameObject, lifeTime); 
    }

    void Update()
    {
        // Просто летим вперед
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, попали ли мы во врага
        // Ищем скрипт BoarAI на объекте или его родителях
        BoarAI enemy = other.GetComponentInParent<BoarAI>();

        if (enemy != null)
        {
            // Наносим урон
            enemy.TakeDamage(damage);
            
            // Уничтожаем стрелу
            Destroy(gameObject);
        }
        else if (!other.isTrigger && other.gameObject.layer != LayerMask.NameToLayer("Player"))
        {
            Destroy(gameObject);
        }
    }
}