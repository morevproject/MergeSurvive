using UnityEngine;
using UnityEngine.UI;

public class PlayerCombatController : MonoBehaviour
{
    [Header("UI")]
    // Мы убрали AddListener, поэтому переменная кнопки нужна только для скрытия/показа
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
        
        // --- УБРАЛИ ЭТУ СТРОКУ ---
        // if (attackButton != null) attackButton.onClick.AddListener(StartAttackSequence);
        // -------------------------
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
        // --- ФИКС: Если открыто Меню, мы прячем кнопку и не ищем врагов ---
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

    // --- СДЕЛАЛИ PUBLIC (чтобы видеть в Инспекторе) ---
    public void StartAttackSequence()
    {
        // На всякий случай защита: если кнопки нет на экране - не бьем
        if (attackButtonObject != null && !attackButtonObject.activeSelf) return;

        if (currentTarget == null) return;

        // 2. ВЗВОДИМ ФЛАГ: "Мы реально хотим ударить"
        isAttackRequested = true;
        
        transform.LookAt(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));

        if (playerAnim != null) 
        {
            playerAnim.TriggerAttack();
        }
    }

    // Этот метод вызывает анимация (Animation Event)
    public void ExecuteAttackLogic()
    {
        if (!this.enabled) return;

        // 3. ПРОВЕРЯЕМ ФЛАГ
        // Если флаг false — значит, этот ивент прилетел из анимации Сбора (или другой), 
        // а не по нажатию кнопки Атаки. ИГНОРИРУЕМ ЕГО.
        if (!isAttackRequested) 
        {
            // Debug.Log("Игнорирую атаку: кнопка не была нажата (скорее всего, рубим дерево)");
            return;
        }

        // СБРАСЫВАЕМ ФЛАГ (чтобы следующий случайный ивент снова не сработал)
        isAttackRequested = false;

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