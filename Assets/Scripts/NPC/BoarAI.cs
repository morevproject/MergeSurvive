using UnityEngine;
using System.Collections;

public class BoarAI : MonoBehaviour
{
    [Header("Stats")]
    public string enemyName = "Robot Enemy"; // Переименовали, раз это робот
    public int maxHealth = 10;
    private int currentHealth;

    // --- НОВОЕ: Ссылка на полоску здоровья ---
    public HealthBar healthBar;
    // ----------------------------------------
    
    [Header("Speed Settings")]
    public float patrolSpeed = 1.5f; 
    public float panicSpeed = 5f;    
    public float panicDuration = 2f; 

    [Header("Loot")]
    public ItemData lootItem;
    [Range(0, 100)] public int dropChance = 50; 

    [Header("Movement")]
    public float changeDirectionTime = 3f;
    public LayerMask obstacleMask; 
    public float obstacleCheckDistance = 1.5f; 
    
    private Vector3 moveDirection;
    private float moveTimer;
    private float currentSpeed;
    
    private Rigidbody rb;      
    private Renderer rend;     
    private Color originalColor;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Пытаемся найти рендерер на дочернем объекте (если робот составной)
        rend = GetComponentInChildren<Renderer>(); 
        if (rend != null) originalColor = rend.material.color;

        currentHealth = maxHealth;
        currentSpeed = patrolSpeed;

        // --- Инициализация полоски здоровья ---
        if (healthBar != null)
        {
            healthBar.Setup(maxHealth);
        }
        // --------------------------------------
        
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
        
        Debug.DrawRay(transform.position, moveDirection * obstacleCheckDistance, Color.yellow);
    }

    void FixedUpdate()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsMenuOpen) return;

        if (Physics.Raycast(transform.position, moveDirection, obstacleCheckDistance, obstacleMask))
        {
            PickNewDirection();
        }

        Vector3 movement = moveDirection * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);

        if (moveDirection.x != 0 || moveDirection.z != 0)
        {
            Quaternion lookRot = Quaternion.LookRotation(moveDirection);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, lookRot, Time.fixedDeltaTime * 5f));
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
        
        // 1. Вылетающая цифра
        DamagePopup.Create(transform.position + Vector3.up * 2f, damage);

        // 2. Обновление полоски
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth);
        }

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
        Debug.Log("Враг побежден!");

        if (lootItem != null)
        {
            int rolls = maxHealth / 2;
            int totalDropped = 0;

            for (int i = 0; i < rolls; i++)
            {
                if (Random.Range(0, 101) <= dropChance)
                {
                    totalDropped++;
                }
            }

            if (totalDropped > 0)
            {
                InventoryManager.Instance.AddItem(lootItem, totalDropped);
                Debug.Log($"Лут: +{totalDropped} {lootItem.DisplayName}");
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