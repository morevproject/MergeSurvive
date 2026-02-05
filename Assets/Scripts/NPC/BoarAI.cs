using UnityEngine;
using System.Collections;

public class BoarAI : MonoBehaviour
{
    [Header("Stats")]
    public string enemyName = "Wild Boar";
    public int maxHealth = 10;
    private int currentHealth;
    
    [Header("Speed Settings")]
    public float patrolSpeed = 1.5f; 
    public float panicSpeed = 5f;    
    public float panicDuration = 2f; 

    [Header("Loot")]
    public ItemData lootItem;
    [Range(0, 100)] public int dropChance = 50; // Шанс выпадения для КАЖДОГО куска (50% например)

    [Header("Movement")]
    public float changeDirectionTime = 3f;
    public LayerMask obstacleMask; // Слой препятствий (Деревья, Камни)
    public float obstacleCheckDistance = 1.5f; // Как далеко смотрим вперед
    
    private Vector3 moveDirection;
    private float moveTimer;
    private float currentSpeed;
    
    private Rigidbody rb;      
    private Renderer rend;     
    private Color originalColor;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
        if (rend != null) originalColor = rend.material.color;

        currentHealth = maxHealth;
        currentSpeed = patrolSpeed;
        
        moveTimer = changeDirectionTime;
        PickNewDirection();
    }

    void Update()
    {
        moveTimer -= Time.deltaTime;
        if (moveTimer <= 0)
        {
            PickNewDirection();
            moveTimer = changeDirectionTime;
        }
        
        // Визуализация луча в редакторе (чтобы ты видел, куда он смотрит)
        Debug.DrawRay(transform.position, moveDirection * obstacleCheckDistance, Color.yellow);
    }

    void FixedUpdate()
    {
        // 1. ПРОВЕРКА НА СТЕНУ (Raycast)
        // Пускаем луч от центра кабана в сторону движения
        if (Physics.Raycast(transform.position, moveDirection, obstacleCheckDistance, obstacleMask))
        {
            // Если уперлись во что-то — СРОЧНО меняем направление
            PickNewDirection();
        }

        // 2. Движение
        Vector3 movement = moveDirection * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);

        // 3. Поворот (лежачий капсюль)
        if (moveDirection.x != 0 || moveDirection.z != 0)
        {
            Quaternion lookRot = Quaternion.LookRotation(moveDirection);
            Quaternion correction = Quaternion.Euler(90, 0, 0); 
            Quaternion targetRotation = lookRot * correction;
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f));
        }
    }

    void PickNewDirection()
    {
        float x = Random.Range(-1f, 1f);
        float z = Random.Range(-1f, 1f);
        moveDirection = new Vector3(x, 0, z).normalized;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        StartCoroutine(FlashColor());
        StartCoroutine(PanicRun()); 
        Debug.Log($"{enemyName} получил {damage} урона!");

        if (currentHealth <= 0) Die();
    }

    IEnumerator PanicRun()
    {
        currentSpeed = panicSpeed;
        PickNewDirection(); 
        yield return new WaitForSeconds(panicDuration);
        currentSpeed = patrolSpeed;
    }

    IEnumerator FlashColor()
    {
        if (rend != null)
        {
            rend.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            rend.material.color = originalColor;
        }
    }

    void Die()
    {
        Debug.Log("Кабанчик побежден!");

        if (lootItem != null)
        {
            // Рассчитываем количество попыток дропа
            // Например: 10 ХП / 2 = 5 попыток.
            int rolls = maxHealth / 2;
            int totalDropped = 0;

            for (int i = 0; i < rolls; i++)
            {
                // Кидаем кубик для каждого кусочка
                if (Random.Range(0, 101) <= dropChance)
                {
                    totalDropped++;
                }
            }

            if (totalDropped > 0)
            {
                InventoryManager.Instance.AddItem(lootItem, totalDropped);
                Debug.Log($"Лутаем кабана: +{totalDropped} {lootItem.DisplayName}");
            }
            else
            {
                Debug.Log("С кабана ничего не выпало (не повезло).");
            }
        }

        var worldID = GetComponent<WorldObjectID>();
        if (worldID != null)
        {
            worldID.RegisterDestruction();
        }

        Destroy(gameObject); 
    }
}