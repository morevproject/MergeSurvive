using UnityEngine;
using UnityEngine.UI;

public class PlayerCombatController : MonoBehaviour
{
    [Header("UI")]
    public GameObject attackButtonObject; 

    [Header("Settings")]
    public LayerMask enemyLayer; 
    
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
        FindNearestEnemy();
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
            if (attackButtonObject != null) attackButtonObject.SetActive(true);
        }
        else
        {
            currentTarget = null;
            if (attackButtonObject != null) attackButtonObject.SetActive(false);
        }
    }

    // === НОВЫЙ МЕТОД ДЛЯ ПОВОРОТА МОДЕЛЬКИ ===
    private void FaceTarget()
    {
        if (currentTarget == null) return;
        
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null && pm.characterModel != null)
        {
            // Поворачиваем саму модельку, сохраняя её высоту
            pm.characterModel.LookAt(new Vector3(currentTarget.position.x, pm.characterModel.position.y, currentTarget.position.z));
        }
        else
        {
            // Резервный вариант (крутим капсулу)
            transform.LookAt(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));
        }
    }

    public void StartAttackSequence()
    {
        if (attackButtonObject != null && !attackButtonObject.activeSelf) return;
        if (currentTarget == null) return;

        isAttackRequested = true;
        
        FaceTarget(); // <--- Разворачиваемся ПЕРЕД началом анимации

        bool isRanged = false;
        if (PlayerProfileUI.Instance != null && PlayerProfileUI.Instance.weaponSlot.currentItem != null)
        {
            isRanged = PlayerProfileUI.Instance.weaponSlot.currentItem.requiresAmmo;
        }

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
        
        FaceTarget(); // <--- Еще раз корректируем прицел В МОМЕНТ выстрела/удара

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