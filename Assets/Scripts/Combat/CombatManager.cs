using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    [Header("UI References")]
    public float turnDelay = 1f;

    [Header("Visuals")]
    public GameObject projectilePrefab; // Префаб стрелы/магии ГЕРОЯ
    public float projectileSpeed = 0.5f;

    private FighterStats hero;
    private BossAI boss;
    private FighterStats bossStats;
    
    // Ссылки на компоненты игрока, которые мы будем отключать
    private PlayerMovement playerMovement;
    private PlayerAnimation playerAnimScript; 
    private PlayerCombatController playerCombat; // Скрипт авто-атаки в открытом мире
    
    private Animator heroAnim;

    private int turnCount = 0;
    private bool battleInProgress = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartBattle(FighterStats playerStats, BossAI bossRef)
    {
        hero = playerStats;
        boss = bossRef;
        bossStats = boss.GetComponent<FighterStats>();
        
        battleInProgress = true;
        turnCount = 0;

        // 1. ВКЛЮЧАЕМ ЗУМ БОЯ
        if (BattleCamera.Instance != null) BattleCamera.Instance.SetBattleZoom(true);

        // 2. ПОЛУЧАЕМ КОМПОНЕНТЫ
        playerMovement = hero.GetComponent<PlayerMovement>();
        playerAnimScript = hero.GetComponent<PlayerAnimation>();
        playerCombat = hero.GetComponent<PlayerCombatController>(); // Находим контроллер боя
        
        heroAnim = hero.GetComponent<Animator>();
        if (heroAnim == null) heroAnim = hero.GetComponentInChildren<Animator>();

        // 3. ОТКЛЮЧАЕМ УПРАВЛЕНИЕ И ЛИШНИЕ СКРИПТЫ
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            Rigidbody rb = hero.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero; // Полная остановка
        }

        if (playerAnimScript != null) playerAnimScript.enabled = false;
        
        // ВАЖНО: Отключаем скрипт атаки, чтобы герой не стрелял в пролетающих роботов
        if (playerCombat != null) 
        {
            playerCombat.enabled = false; 
        }

        if (heroAnim != null) heroAnim.SetBool("IsMoving", false);

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
            
            // Смотрим друг на друга
            hero.transform.LookAt(new Vector3(target.x, heroPos.y, target.z));
            boss.transform.LookAt(new Vector3(heroPos.x, target.y, heroPos.z));

            // Двигаем героя
            hero.transform.position = Vector3.MoveTowards(heroPos, new Vector3(target.x, heroPos.y, target.z), 5f * Time.deltaTime);

            timeout -= Time.deltaTime;
            yield return null; 
        }

        if (heroAnim != null && isMoving) heroAnim.SetBool("IsMoving", false);
        yield return new WaitForSeconds(0.5f); // Пауза перед первым ударом

        Debug.Log("БОЙ: Дистанция сокращена. Начало раундов.");

        // --- ФАЗА 2: ЦИКЛ БОЯ ---
        while (battleInProgress)
        {
            turnCount++;
            Debug.Log($"--- РАУНД {turnCount} ---");

            // ================= ХОД ГЕРОЯ =================
            if (hero.currentHealth > 0)
            {
                Debug.Log("Ход Героя!");
                yield return new WaitForSeconds(turnDelay);
                
                // Запускаем анимацию (Ивент в ней сработает, но PlayerCombatController его проигнорирует)
                if (heroAnim != null) heroAnim.SetTrigger("Attack");

                bool isRanged = hero.attackRange > 3.0f; 

                // Логика нанесения урона (Менеджер сам создает снаряд или наносит урон)
                if (isRanged && projectilePrefab != null)
                {
                    yield return new WaitForSeconds(0.3f); // Задержка для синхронизации с анимацией
                    GameObject arrowObj = Instantiate(projectilePrefab, hero.transform.position + Vector3.up, Quaternion.identity);
                    
                    ProjectileMover projScript = arrowObj.GetComponent<ProjectileMover>();
                    if (projScript == null) projScript = arrowObj.AddComponent<ProjectileMover>();
                    
                    projScript.Initialize(bossStats, hero.GetDamage());
                    yield return new WaitForSeconds(projectileSpeed);
                }
                else
                {
                    // Ближний бой
                    yield return new WaitForSeconds(0.5f); 
                    int heroDmg = hero.GetDamage();
                    bossStats.TakeDamage(heroDmg);
                    Debug.Log($"Герой ударил мечом на {heroDmg}!");
                }
                
                yield return new WaitForSeconds(0.5f);

                // Проверка победы
                if (bossStats.currentHealth <= 0)
                {
                    boss.OnDeath();
                    yield break;
                }
            }

            // ================= ХОД БОССА =================
            if (bossStats.currentHealth > 0)
            {
                Debug.Log("Ход Босса!");
                yield return new WaitForSeconds(turnDelay);
                
                // ШАГ A: ПРОВЕРКА АБИЛКИ (ДОПОЛНИТЕЛЬНАЯ АТАКА)
                if (boss.abilities != null && boss.abilities.Count > 0)
                {
                    CombatAbility ability = boss.abilities[0];
                    
                    // Каждый N-й ход (например, каждый 3-й)
                    if (turnCount % ability.cooldownRounds == 0)
                    {
                        Debug.Log($"<color=orange>Босс кастует {ability.abilityName}!</color>");
                        
                        // Запускаем фаербол (он сам найдет FirePoint)
                        ability.Execute(bossStats, hero);
                        
                        // Ждем секунду, пока фаербол летит и взрывается
                        yield return new WaitForSeconds(1.0f); 
                    }
                }

                // ШАГ B: ОБЫЧНАЯ АТАКА (ПРОИСХОДИТ ВСЕГДА ПОСЛЕ МАГИИ)
                int bossDmg = bossStats.GetDamage();
                hero.TakeDamage(bossDmg);
                Debug.Log($"Босс ударил рукой на {bossDmg}.");
                
                yield return new WaitForSeconds(0.5f);

                // Проверка поражения
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
        battleInProgress = false;
        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        yield return new WaitForSeconds(2f);
        // Флаг для сохранения, если нужно
        BossAI.JustReloadedFromDeath = true; 
        SaveManager.Instance.LoadGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void EndBattle(bool playerWon)
    {
        battleInProgress = false;
        
        // 1. ВЫКЛЮЧАЕМ ЗУМ (возвращаем камеру)
        if (BattleCamera.Instance != null) BattleCamera.Instance.SetBattleZoom(false);

        // 2. ВКЛЮЧАЕМ УПРАВЛЕНИЕ ОБРАТНО
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerAnimScript != null) playerAnimScript.enabled = true;
        
        // 3. РАЗРЕШАЕМ СНОВА СТРЕЛЯТЬ В МИРЕ (Включаем авто-атаку)
        if (playerCombat != null) playerCombat.enabled = true;
    }
}