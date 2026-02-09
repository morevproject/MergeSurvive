using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(FighterStats))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(WorldObjectID))]
public class BossAI : MonoBehaviour
{
    [Header("Setup")]
    public PatrolPath patrolPath;
    public float moveSpeed = 2f;
    public float rotationSpeed = 5f;
    public HealthBar healthBar; // Сюда кидай префаб полоски

    [Header("Combat Settings")]
    public float aggroRadius = 5f; 
    public List<CombatAbility> abilities; 

    [Header("Loot")]
    public ItemData lootItem;
    [Range(0, 100)] public int dropChance = 100;
    
    public int gearsDropAmount = 1; 

    [Header("Patrol Settings")]
    public float patrolWaitTime = 2f; 
    private float waitTimer = 0f;     

    private int currentPathIndex = 0;
    private Rigidbody rb;
    private bool isInCombat = false;
    private FighterStats myStats;

    public static bool JustReloadedFromDeath = false;
    private bool aggroDisabled = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        myStats = GetComponent<FighterStats>();

        // --- НОВОЕ: Настройка полоски ХП ---
        if (healthBar != null && myStats != null)
        {
            healthBar.Setup(myStats.maxHealth);
            // Подписываемся на изменение здоровья
            myStats.OnHealthChanged += UpdateHealthBar;
        }
        // ------------------------------------
        
        if (JustReloadedFromDeath)
        {
            StartCoroutine(GracePeriod());
            JustReloadedFromDeath = false;
        }
    }

    // --- Метод для обновления полоски (вызывается событием) ---
    void UpdateHealthBar(int currentHealth)
    {
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth);
        }
    }

    // Обязательно отписываемся при уничтожении, чтобы не было ошибок
    void OnDestroy()
    {
        if (myStats != null)
        {
            myStats.OnHealthChanged -= UpdateHealthBar;
        }
    }

    IEnumerator GracePeriod()
    {
        aggroDisabled = true;
        Debug.Log("[BossAI] Игрок воскрес. Босс временно не атакует.");
        yield return new WaitForSeconds(5f);
        aggroDisabled = false;
        Debug.Log("[BossAI] Босс снова опасен!");
    }

    void FixedUpdate()
    {
        if (isInCombat || myStats.isDead) return;

        PatrolLogic();
        CheckAggro();
    }

    void PatrolLogic()
    {
        if (patrolPath == null || patrolPath.Length == 0) return;

        if (waitTimer > 0)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        Transform targetPoint = patrolPath.GetPoint(currentPathIndex);
        Vector3 targetPositionFixed = new Vector3(targetPoint.position.x, transform.position.y, targetPoint.position.z);
        float distance = Vector3.Distance(transform.position, targetPositionFixed);

        transform.position = Vector3.MoveTowards(transform.position, targetPositionFixed, moveSpeed * Time.deltaTime);

        if (distance > 0.1f)
        {
            Vector3 direction = (targetPositionFixed - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
            }
        }

        if (distance < 0.01f)
        {
            waitTimer = patrolWaitTime; 
            currentPathIndex++;         
        }
    }

    void CheckAggro()
    {
        if (aggroDisabled) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (dist <= aggroRadius)
        {
            StartCombat(player);
        }
    }

    public void StartCombat(GameObject player)
    {
        if (isInCombat) return;
        isInCombat = true;

        Debug.Log("!!! БОЙ НАЧАЛСЯ !!!");
        SaveManager.Instance.SaveGame();
        CombatManager.Instance.StartBattle(player.GetComponent<FighterStats>(), this);
    }
    
    public void OnDeath()
    {
        if (lootItem != null && Random.Range(0, 100) < dropChance)
        {
            InventoryManager.Instance.AddItem(lootItem, 1);
        }

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddGears(gearsDropAmount);
            Debug.Log($"[BossAI] Босс побежден! Игрок получил {gearsDropAmount} шестерёнок.");
        }

        var worldID = GetComponent<WorldObjectID>();
        if (worldID != null) worldID.RegisterDestruction();

        Destroy(gameObject);
        
        CombatManager.Instance.EndBattle(true);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
    }
}