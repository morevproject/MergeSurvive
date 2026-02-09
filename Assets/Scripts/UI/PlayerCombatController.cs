using UnityEngine;
using UnityEngine.UI;

public class PlayerCombatController : MonoBehaviour
{
    [Header("UI")]
    public Button attackButton; 
    public GameObject attackButtonObject; 

    [Header("Settings")]
    public LayerMask enemyLayer; 
    
    [Header("Ranged Combat")]
    public GameObject arrowPrefab; 
    public Transform firePoint;    

    [Header("Animation")]
    public PlayerAnimation playerAnim;

    private Transform currentTarget; 

    void Start()
    {
        if (attackButtonObject != null) attackButtonObject.SetActive(false);
        if (attackButton != null) attackButton.onClick.AddListener(StartAttackSequence);
    }

    void Update()
    {
        FindNearestEnemy();
    }

    // --- ДОБАВЛЕНО: Прячем кнопку, если скрипт отключает CombatManager ---
    void OnDisable()
    {
        if (attackButtonObject != null) attackButtonObject.SetActive(false);
    }

    void FindNearestEnemy()
    {
        if (PlayerStats.Instance == null) return;
        float range = PlayerStats.Instance.CurrentAttackRange;
        Collider[] enemies = Physics.OverlapSphere(transform.position, range, enemyLayer);
        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        foreach (var enemyCollider in enemies)
        {
            if (enemyCollider.GetComponentInParent<BoarAI>() != null) 
            {
                float distance = Vector3.Distance(transform.position, enemyCollider.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestEnemy = enemyCollider.transform;
                }
            }
        }

        if (nearestEnemy != null)
        {
            currentTarget = nearestEnemy;
            if (attackButtonObject != null) attackButtonObject.SetActive(true);
        }
        else
        {
            currentTarget = null;
            if (attackButtonObject != null) attackButtonObject.SetActive(false);
        }
    }

    void StartAttackSequence()
    {
        if (currentTarget == null) return;
        
        transform.LookAt(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));

        if (playerAnim != null) 
        {
            playerAnim.TriggerAttack();
        }
    }

    public void ExecuteAttackLogic()
    {
        // --- ВАЖНЫЙ ФИКС ---
        // Игнорируем ивент анимации, если скрипт выключен (например, идет бой с боссом)
        if (!this.enabled) return;
        // -------------------

        if (currentTarget == null || PlayerProfileUI.Instance == null) return;
        
        transform.LookAt(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));

        ItemData weapon = PlayerProfileUI.Instance.weaponSlot.currentItem;
        ItemData ammo = PlayerProfileUI.Instance.ammoSlot.currentItem;
        int damage = PlayerStats.Instance.TotalDamage;

        if (weapon != null && weapon.requiresAmmo)
        {
            if (ammo == null || InventoryManager.Instance.GetItemCount(ammo) <= 0)
            {
                Debug.Log("Нет стрел!"); 
                return;
            }

            InventoryManager.Instance.RemoveItem(ammo, 1);

            if (arrowPrefab != null && firePoint != null)
            {
                GameObject arrowObj = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);
                Projectile projectile = arrowObj.GetComponent<Projectile>();
                Vector3 targetSpot = currentTarget.position + Vector3.up * 0.5f;
                projectile.Setup(damage, targetSpot);
            }
        }
        else
        {
            BoarAI enemyScript = currentTarget.GetComponentInParent<BoarAI>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(damage);
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (PlayerStats.Instance != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, PlayerStats.Instance.CurrentAttackRange);
        }
    }
}