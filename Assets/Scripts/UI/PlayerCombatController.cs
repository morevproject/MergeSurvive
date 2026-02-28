using UnityEngine;
using UnityEngine.UI;

public class PlayerCombatController : MonoBehaviour
{
    [Header("UI")]
    public GameObject attackButtonObject; 

    [Header("Settings")]
    public LayerMask enemyLayer; 
    
    [Header("Combat Cooldowns (Anti-Spam)")]
    public float meleeAttackCooldown = 1.1f;  
    public float rangedAttackCooldown = 1.6f; 
    private float nextAttackTime = 0f;        

    [Header("Ranged Combat")]
    public GameObject arrowPrefab; 
    public Transform firePoint;    

    [Header("Animation")]
    public PlayerAnimation playerAnim;

    private bool isAttackRequested = false;
    private Transform currentTarget; 

    void Start()
    {
        if (attackButtonObject != null) attackButtonObject.SetActive(false);
    }

    void OnDisable()
    {
        if (attackButtonObject != null) attackButtonObject.SetActive(false);
    }

    void Update()
    {
        if (!isAttackRequested)
        {
            FindNearestEnemy();
        }
    }

    void FindNearestEnemy()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsMenuOpen)
        {
            currentTarget = null;
            if (attackButtonObject != null) attackButtonObject.SetActive(false);
            return;
        }

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
            
            // === ÔÈÊÑ ÎÒÎÁÐÀÆÅÍÈß ÊÍÎÏÊÈ ===
            // Ïîêàçûâàåì êíîïêó ÒÎËÜÊÎ åñëè âðàã ðÿäîì È ìû íå â êóëäàóíå
            if (attackButtonObject != null) 
            {
                bool isCooldownFinished = Time.time >= nextAttackTime;
                attackButtonObject.SetActive(isCooldownFinished);
            }
        }
        else
        {
            currentTarget = null;
            if (attackButtonObject != null) attackButtonObject.SetActive(false);
        }
    }

    private void FaceTarget()
    {
        if (currentTarget == null) return;
        
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null && pm.characterModel != null)
        {
            pm.characterModel.LookAt(new Vector3(currentTarget.position.x, pm.characterModel.position.y, currentTarget.position.z));
        }
        else
        {
            transform.LookAt(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));
        }
    }

    public void StartAttackSequence()
    {
        if (attackButtonObject != null && !attackButtonObject.activeSelf) return;
        if (currentTarget == null) return;
        if (Time.time < nextAttackTime) return;

        isAttackRequested = true;
        
        // === ÌÃÍÎÂÅÍÍÎ ÏÐß×ÅÌ ÊÍÎÏÊÓ ===
        if (attackButtonObject != null) attackButtonObject.SetActive(false);
        
        FaceTarget(); 

        bool isRanged = false;
        if (PlayerProfileUI.Instance != null && PlayerProfileUI.Instance.weaponSlot.currentItem != null)
        {
            isRanged = PlayerProfileUI.Instance.weaponSlot.currentItem.requiresAmmo;
        }

        nextAttackTime = Time.time + (isRanged ? rangedAttackCooldown : meleeAttackCooldown);

        if (playerAnim != null) 
        {
            if (isRanged)
            {
                playerAnim.animator.SetTrigger("doShoot"); 
            }
            else
            {
                playerAnim.TriggerAttack();
            }
        }
    }

    public void ExecuteAttackLogic()
    {
        if (!this.enabled) return;

        if (!isAttackRequested) 
        {
            return;
        }

        isAttackRequested = false;

        if (currentTarget == null || PlayerProfileUI.Instance == null) return;
        
        FaceTarget(); 

        ItemData weapon = PlayerProfileUI.Instance.weaponSlot.currentItem;
        ItemData ammo = PlayerProfileUI.Instance.ammoSlot.currentItem;
        
        int damage = PlayerStats.Instance.RollCurrentDamage();

        if (weapon != null && weapon.requiresAmmo)
        {
            if (ammo == null || InventoryManager.Instance.GetItemCount(ammo) <= 0)
            {
                Debug.Log("Íåò ñòðåë!"); 
                return;
            }

            InventoryManager.Instance.RemoveItem(ammo, 1);

            if (arrowPrefab != null && firePoint != null)
            {
                GameObject arrowObj = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);
                Projectile projectile = arrowObj.GetComponent<Projectile>();
                
                projectile.Setup(damage, currentTarget);
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