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

        // Закрываем UI
        if (UIManager.Instance != null) UIManager.Instance.CloseAllMenus();

        // 1. ВКЛЮЧАЕМ ЗУМ
        if (BattleCamera.Instance != null) BattleCamera.Instance.SetBattleZoom(true);

        // 2. ПОЛУЧАЕМ КОМПОНЕНТЫ
        playerMovement = hero.GetComponent<PlayerMovement>();
        playerAnimScript = hero.GetComponent<PlayerAnimation>();
        playerCombat = hero.GetComponent<PlayerCombatController>(); 
        
        heroAnim = hero.GetComponent<Animator>();
        if (heroAnim == null) heroAnim = hero.GetComponentInChildren<Animator>();

        // 3. ОТКЛЮЧАЕМ УПРАВЛЕНИЕ ИГРОКА
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            Rigidbody rb = hero.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero; 
        }

        if (playerAnimScript != null) playerAnimScript.enabled = false;
        if (playerCombat != null) playerCombat.enabled = false; 

        if (heroAnim != null) heroAnim.SetBool("IsMoving", false);

        // --- ВАЖНОЕ ИСПРАВЛЕНИЕ: ОТКЛЮЧАЕМ МОЗГИ БОССА ---
        // Чтобы его Update не мешал нам вращать его модель
        if (boss != null) boss.enabled = false;
        // Если у босса есть NavMeshAgent, его тоже лучше остановить
        var agent = boss.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.isStopped = true;
        // --------------------------------------------------

        StartCoroutine(BattleLoop());
    }

    IEnumerator BattleLoop()
    {
        Debug.Log("БОЙ: Начинаем фазу сближения...");
        
        // --- ФАЗА 1: СБЛИЖЕНИЕ ---
        float stopDistance = hero.attackRange - 0.5f;
        if (stopDistance < 1.5f) stopDistance = 1.5f; 

        float timeout = 3.0f;
        bool isMoving = false; 

        // 1. СРАЗУ ПОВОРАЧИВАЕМ ИХ ДРУГ К ДРУГУ (Для красоты старта)
        Vector3 startTarget = boss.transform.position;
        Vector3 startHero = hero.transform.position;
        hero.transform.LookAt(new Vector3(startTarget.x, startHero.y, startTarget.z));
        boss.transform.LookAt(new Vector3(startHero.x, startTarget.y, startHero.z));
        // --------------------------------------------------------

        if (heroAnim != null) 
        {
            heroAnim.SetBool("IsMoving", true); 
            isMoving = true;
        }
        
        // Бежим к боссу
        while (Vector3.Distance(hero.transform.position, boss.transform.position) > stopDistance && timeout > 0)
        {
            Vector3 target = boss.transform.position;
            Vector3 heroPos = hero.transform.position;
            
            // Постоянно обновляем поворот (на случай если босс или герой сместились)
            hero.transform.LookAt(new Vector3(target.x, heroPos.y, target.z));
            
            // Теперь это сработает, так как boss.enabled = false
            boss.transform.LookAt(new Vector3(heroPos.x, target.y, heroPos.z));

            // Двигаем героя
            hero.transform.position = Vector3.MoveTowards(heroPos, new Vector3(target.x, heroPos.y, target.z), 5f * Time.deltaTime);

            timeout -= Time.deltaTime;
            yield return null; 
        }

        if (heroAnim != null && isMoving) heroAnim.SetBool("IsMoving", false);
        yield return new WaitForSeconds(0.5f); 

        Debug.Log("БОЙ: Дистанция сокращена. Начало раундов.");

        // --- ФАЗА 2: ЦИКЛ БОЯ ---
        while (IsBattleInProgress)
        {
            turnCount++;
            Debug.Log($"--- РАУНД {turnCount} ---");

            // ================= ХОД ГЕРОЯ =================
            if (hero.currentHealth > 0)
            {
                // Еще раз убедимся, что босс смотрит на героя перед получением урона
                boss.transform.LookAt(new Vector3(hero.transform.position.x, boss.transform.position.y, hero.transform.position.z));
                
                yield return new WaitForSeconds(turnDelay);
                
                if (heroAnim != null) heroAnim.SetTrigger("Attack");

                bool isRanged = hero.attackRange > 3.0f; 

                if (isRanged && projectilePrefab != null)
                {
                    yield return new WaitForSeconds(0.3f); 
                    GameObject arrowObj = Instantiate(projectilePrefab, hero.transform.position + Vector3.up, Quaternion.identity);
                    
                    ProjectileMover projScript = arrowObj.GetComponent<ProjectileMover>();
                    if (projScript == null) projScript = arrowObj.AddComponent<ProjectileMover>();
                    
                    projScript.Initialize(bossStats, hero.GetDamage());
                    yield return new WaitForSeconds(projectileSpeed);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f); 
                    int heroDmg = hero.GetDamage();
                    bossStats.TakeDamage(heroDmg);
                    Debug.Log($"Герой ударил мечом на {heroDmg}!");
                }
                
                yield return new WaitForSeconds(0.5f);

                if (bossStats.currentHealth <= 0)
                {
                    // Включаем босса обратно перед смертью (если там есть логика рэгдолла или дропа)
                    if (boss != null) boss.enabled = true;
                    boss.OnDeath();
                    yield break;
                }
            }

            // ================= ХОД БОССА =================
            if (bossStats.currentHealth > 0)
            {
                // Поворачиваем босса к герою перед атакой
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

        // --- ВКЛЮЧАЕМ БОССА ОБРАТНО ---
        // Если игрок убежал или бой закончился скриптом, возвращаем боссу интеллект
        if (boss != null) 
        {
            boss.enabled = true;
            var agent = boss.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.isStopped = false;
        }
    }
}