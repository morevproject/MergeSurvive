using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoarAI : MonoBehaviour
{
    [Header("Stats")]
    public string enemyName = "Robot Enemy"; 
    public int maxHealth = 10;
    private int currentHealth;

    public HealthBar healthBar;
    
    [Header("Speed Settings")]
    public float patrolSpeed = 1.5f; 
    public float panicSpeed = 5f;    
    public float panicDuration = 2f; 

    // --- НОВАЯ СИСТЕМА ЛУТА ---
    [System.Serializable]
    public class EnemyLoot
    {
        public string name; // Для удобства в инспекторе (не влияет на логику)
        public ItemData item;
        [Range(0, 100)] public float dropChance = 50f; // Шанс в %
        public int minAmount = 1;
        public int maxAmount = 1;
    }

    [Header("Loot Table")]
    // Список всех возможных предметов с этого врага
    public List<EnemyLoot> lootTable = new List<EnemyLoot>(); 
    // --------------------------

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
        rend = GetComponentInChildren<Renderer>(); 
        if (rend != null) originalColor = rend.material.color;

        currentHealth = maxHealth;
        currentSpeed = patrolSpeed;

        if (healthBar != null)
        {
            healthBar.Setup(maxHealth);
        }
        
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
        // Если открыто меню — стоим
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
        
        DamagePopup.Create(transform.position + Vector3.up * 2f, damage);

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

        // --- НОВАЯ ЛОГИКА ВЫДАЧИ ЛУТА ---
        if (InventoryManager.Instance != null && lootTable.Count > 0)
        {
            foreach (var lootRule in lootTable)
            {
                if (lootRule.item == null) continue;

                // Кидаем кубик (0..100)
                float roll = Random.Range(0f, 100f);

                if (roll <= lootRule.dropChance)
                {
                    // Определяем количество
                    int amountToGive = Random.Range(lootRule.minAmount, lootRule.maxAmount + 1);
                    
                    if (amountToGive > 0)
                    {
                        InventoryManager.Instance.AddItem(lootRule.item, amountToGive);
                        // Лог для проверки
                        Debug.Log($"Лут: +{amountToGive} {lootRule.item.ItemName}");
                    }
                }
            }
        }
        // --------------------------------

        var worldID = GetComponent<WorldObjectID>();
        if (worldID != null)
        {
            worldID.RegisterDestruction();
        }

        Destroy(gameObject); 
    }
}