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
    public HealthBar healthBar;

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

        if (healthBar != null && myStats != null)
        {
            healthBar.Setup(myStats.maxHealth);
            myStats.OnHealthChanged += UpdateHealthBar;
        }
        
        if (JustReloadedFromDeath)
        {
            StartCoroutine(GracePeriod());
            JustReloadedFromDeath = false;
        }
    }

    void UpdateHealthBar(int currentHealth)
    {
        if (healthBar != null) healthBar.UpdateHealth(currentHealth);
    }

    void OnDestroy()
    {
        if (myStats != null) myStats.OnHealthChanged -= UpdateHealthBar;
    }

    IEnumerator GracePeriod()
    {
        aggroDisabled = true;
        yield return new WaitForSeconds(5f);
        aggroDisabled = false;
    }

    void FixedUpdate()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsMenuOpen) return;
        
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

        // --- 1. ÂÊËÞ×ÀÅÌ ÐÅÆÈÌ ÁÎÑÑÀ (Ñêðûâàåì UI) ---
        if (UIManager.Instance != null) UIManager.Instance.SetBossMode(true);
        // ---------------------------------------------

        Debug.Log("!!! ÁÎÉ ÍÀ×ÀËÑß !!!");
        SaveManager.Instance.SaveGame();
        CombatManager.Instance.StartBattle(player.GetComponent<FighterStats>(), this);
    }
    
    public void OnDeath()
    {
        // --- 2. ÂÛÊËÞ×ÀÅÌ ÐÅÆÈÌ ÁÎÑÑÀ (Âîçâðàùàåì UI) ---
        if (UIManager.Instance != null) UIManager.Instance.SetBossMode(false);
        // -----------------------------------------------

        if (lootItem != null && Random.Range(0, 100) < dropChance)
        {
            InventoryManager.Instance.AddItem(lootItem, 1);
        }

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddGears(gearsDropAmount);
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