using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    [Header("UI References")]
    public float turnDelay = 1f;

    [Header("Visuals")]
    public GameObject projectilePrefab; 
    public float projectileSpeed = 0.5f;

    private FighterStats hero;
    private BossAI boss;
    private FighterStats bossStats;
    
    // Ссылки на компоненты игрока
    private PlayerMovement playerMovement;
    private PlayerAnimation playerAnimScript; 
    private PlayerCombatController playerCombat; 
    
    private Animator heroAnim;

    private int turnCount = 0;
    
    public bool IsBattleInProgress { get; private set; } = false;

    private void Awake()
    {
        Instance = this;
    }

    public void StartBattle(FighterStats playerStats, BossAI bossRef)
    {
        hero = playerStats;
        boss = bossRef;
        bossStats = boss.GetComponent<FighterStats>();
        
        IsBattleInProgress = true;
        turnCount = 0;

        if (UIManager.Instance != null) UIManager.Instance.CloseAllMenus();
        if (BattleCamera.Instance != null) BattleCamera.Instance.SetBattleZoom(true);

        playerMovement = hero.GetComponent<PlayerMovement>();
        playerAnimScript = hero.GetComponent<PlayerAnimation>();
        playerCombat = hero.GetComponent<PlayerCombatController>(); 
        
        heroAnim = hero.GetComponent<Animator>();
        if (heroAnim == null) heroAnim = hero.GetComponentInChildren<Animator>();

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            Rigidbody rb = hero.GetComponent<Rigidbody>();
            if (rb != null) 
            {
                rb.velocity = Vector3.zero; 
                rb.isKinematic = true; // <--- ФИКС: Делаем мужика неуязвимым к толчкам
            }
        }

        if (playerAnimScript != null) playerAnimScript.enabled = false;
        if (playerCombat != null) playerCombat.enabled = false; 

        if (heroAnim != null) heroAnim.SetFloat("Speed", 0f); // Сбрасываем Blend Tree в 0

        if (boss != null) boss.enabled = false;
        var agent = boss.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.isStopped = true;

        StartCoroutine(BattleLoop());
    }

    IEnumerator BattleLoop()
    {
        Debug.Log("БОЙ: Начинаем фазу сближения...");
        
        float stopDistance = hero.attackRange - 0.5f;
        if (stopDistance < 1.5f) stopDistance = 1.5f; 

        float timeout = 3.0f;
        bool isMoving = false; 

        Vector3 startTarget = boss.transform.position;
        Vector3 startHero = hero.transform.position;
        
        // ФИКС ВЫСОТЫ 1: Используем строго собственную высоту Y модельки для поворота!
        if (playerMovement != null && playerMovement.characterModel != null)
        {
            float modelY = playerMovement.characterModel.position.y;
            playerMovement.characterModel.LookAt(new Vector3(startTarget.x, modelY, startTarget.z));
        }
        else
        {
            hero.transform.LookAt(new Vector3(startTarget.x, hero.transform.position.y, startTarget.z));
        }
        
        // Босс тоже смотрит строго по своей высоте
        boss.transform.LookAt(new Vector3(startHero.x, boss.transform.position.y, startHero.z));

        if (heroAnim != null) 
        {
            heroAnim.SetFloat("Speed", 0.5f); // 0.5 - это шаг в нашем новом Blend Tree
            isMoving = true;
        }
        
        while (Vector3.Distance(hero.transform.position, boss.transform.position) > stopDistance && timeout > 0)
        {
            Vector3 target = boss.transform.position;
            Vector3 heroPos = hero.transform.position;
            
            // ФИКС ВЫСОТЫ 2: Поворот в движении
            if (playerMovement != null && playerMovement.characterModel != null)
            {
                float modelY = playerMovement.characterModel.position.y;
                playerMovement.characterModel.LookAt(new Vector3(target.x, modelY, target.z));
            }
            else
            {
                hero.transform.LookAt(new Vector3(target.x, hero.transform.position.y, target.z));
            }

            boss.transform.LookAt(new Vector3(heroPos.x, boss.transform.position.y, heroPos.z));

            // Двигаем капсулу (тут высоту не трогаем, чтобы не взлетел)
            hero.transform.position = Vector3.MoveTowards(heroPos, new Vector3(target.x, heroPos.y, target.z), 5f * Time.deltaTime);

            timeout -= Time.deltaTime;
            yield return null; 
        }

        if (heroAnim != null && isMoving) heroAnim.SetFloat("Speed", 0f);
        yield return new WaitForSeconds(0.5f); 

        Debug.Log("БОЙ: Дистанция сокращена. Начало раундов.");

        while (IsBattleInProgress)
        {
            turnCount++;
            Debug.Log($"--- РАУНД {turnCount} ---");

            // ================= ХОД ГЕРОЯ =================
            if (hero.currentHealth > 0)
            {
                boss.transform.LookAt(new Vector3(hero.transform.position.x, boss.transform.position.y, hero.transform.position.z));
                
                // ФИКС ВЫСОТЫ 3: Корректировка поворота перед ударом
                if (playerMovement != null && playerMovement.characterModel != null)
                {
                    float modelY = playerMovement.characterModel.position.y;
                    playerMovement.characterModel.LookAt(new Vector3(boss.transform.position.x, modelY, boss.transform.position.z));
                }
                
                yield return new WaitForSeconds(turnDelay);
                
                bool isRanged = hero.attackRange > 3.0f; 

                if (heroAnim != null) 
                {
                    if (isRanged) heroAnim.SetTrigger("doShoot");
                    else heroAnim.SetTrigger("doAttack");
                }

                if (isRanged && projectilePrefab != null)
                {
                    yield return new WaitForSeconds(0.3f); 
                    
                    // Спавним стрелу (чуть выше головы героя, чтобы из лука вылетала)
                    Vector3 spawnPos = hero.transform.position + Vector3.up * 1.5f;
                    GameObject arrowObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
                    
                    // === ФИКС: Используем наш умный Projectile ===
                    Projectile projScript = arrowObj.GetComponent<Projectile>();
                    if (projScript != null)
                    {
                        int heroDmg = hero.GetDamage();
                        // Передаем босса как цель для самонаведения!
                        projScript.Setup(heroDmg, boss.transform);
                        Debug.Log($"Герой выстрелил в босса на {heroDmg}!");
                    }
                    
                    yield return new WaitForSeconds(projectileSpeed);
                }
                else
                {
                    // Логика ближнего боя осталась без изменений
                    yield return new WaitForSeconds(0.5f); 
                    int heroDmg = hero.GetDamage();
                    bossStats.TakeDamage(heroDmg);
                    Debug.Log($"Герой ударил мечом на {heroDmg}!");
                }
                
                yield return new WaitForSeconds(0.5f);

                if (bossStats.currentHealth <= 0)
                {
                    if (boss != null) boss.enabled = true;
                    boss.OnDeath();
                    yield break;
                }
            }

            // ================= ХОД БОССА =================
            if (bossStats.currentHealth > 0)
            {
                boss.transform.LookAt(new Vector3(hero.transform.position.x, boss.transform.position.y, hero.transform.position.z));

                yield return new WaitForSeconds(turnDelay);
                
                if (boss.abilities != null && boss.abilities.Count > 0)
                {
                    CombatAbility ability = boss.abilities[0];
                    
                    if (turnCount % ability.cooldownRounds == 0)
                    {
                        Debug.Log($"<color=orange>Босс кастует {ability.abilityName}!</color>");
                        ability.Execute(bossStats, hero);
                        yield return new WaitForSeconds(1.0f); 
                    }
                }

                int bossDmg = bossStats.GetDamage();
                hero.TakeDamage(bossDmg);
                Debug.Log($"Босс ударил рукой на {bossDmg}.");
                
                yield return new WaitForSeconds(0.5f);

                if (hero.currentHealth <= 0)
                {
                    HandlePlayerDeath();
                    yield break;
                }
            }
        }
    }

    void HandlePlayerDeath()
    {
        Debug.Log("<color=red>ГЕРОЙ ПОГИБ!</color>");
        IsBattleInProgress = false;
        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        yield return new WaitForSeconds(2f);
        BossAI.JustReloadedFromDeath = true; 
        SaveManager.Instance.LoadGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void EndBattle(bool playerWon)
    {
        IsBattleInProgress = false;
        
        if (BattleCamera.Instance != null) BattleCamera.Instance.SetBattleZoom(false);

        if (playerMovement != null) playerMovement.enabled = true;
        if (playerAnimScript != null) playerAnimScript.enabled = true;
        if (playerCombat != null) playerCombat.enabled = true;

        if (boss != null) 
        {
            boss.enabled = true;
            var agent = boss.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.isStopped = false;
        }

        Rigidbody rb = hero.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false; // <--- ВОЗВРАЩАЕМ ФИЗИКУ
    }
}