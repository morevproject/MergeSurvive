using UnityEngine;
using System; // Нужен для работы со временем (DateTime)

public class RobotNannyBrain : MonoBehaviour
{
    [Header("Tracking & Movement")]
    public Transform player;
    public float maxDistance = 5f; // Если игрок дальше этого расстояния - робот летит за ним
    public float stopDistance = 2.5f; // Ближе этого расстояния робот не подлетает (держит дистанцию)
    public float moveSpeed = 3f; // Скорость полета
    public float rotationSpeed = 5f; // Скорость поворота "головы"

    private bool hasTriggeredTutorial = false; // Флаг, чтобы запустить тутор только один раз

    [Header("Future Feature: Auto-Gathering")]
    public bool isAutoGatheringActive = false;
    public string gatherSubscriptionEndTime; // Храним дату окончания текстом или через DateTime

    private void Start()
    {
        // Авто-поиск игрока, если забыли назначить в Инспекторе
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    private void Update()
    {
        if (player == null) return;

        HandleTrackingAndMovement();
    }

    private void HandleTrackingAndMovement()
    {
        // 1. ПЛАВНЫЙ ПОВОРОТ (Смотрим на игрока)
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0; // Игнорируем высоту, чтобы робот смотрел ровно, а не "кивал" в пол
        
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            // Slerp делает поворот мягким и естественным
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // 2. ПОЛЕТ ЗА ИГРОКОМ
        // Считаем дистанцию только по "земле" (оси X и Z), игнорируя высоту полета Няни!
        Vector3 flatRobotPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flatPlayerPos = new Vector3(player.position.x, 0, player.position.z);
        float distance = Vector3.Distance(flatRobotPos, flatPlayerPos);
        
        // Проверяем, ждет ли туториал нашего подлета
        bool isTutorialWaiting = (TutorialManager.Instance != null && TutorialManager.Instance.currentStep == TutorialManager.TutorialStep.WaitNannyApproach);

        // ЛЕТИМ, если игрок слишком далеко ИЛИ если мы в начале туториала!
        if (distance > maxDistance || isTutorialWaiting)
        {
            // Вычисляем точку, где робот должен остановиться (stopDistance от игрока)
            Vector3 targetPosition = player.position - directionToPlayer.normalized * stopDistance;
            
            // Фиксируем текущую высоту робота, чтобы он летел ровно и не врезался в землю
            targetPosition.y = transform.position.y; 

            // Lerp делает полет плавным: робот ускоряется при старте и замедляется при торможении
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
        }

        // --- ТРИГГЕР ТУТОРИАЛА ---
        // Если робот подлетел достаточно близко (stopDistance + небольшая погрешность)
        if (!hasTriggeredTutorial && distance <= stopDistance + 0.5f)
        {
            // Проверяем, существует ли менеджер и ждет ли он подлета Няни
            if (TutorialManager.Instance != null && TutorialManager.Instance.currentStep == TutorialManager.TutorialStep.WaitNannyApproach)
            {
                hasTriggeredTutorial = true; // Запоминаем, что уже запустили
                TutorialManager.Instance.AdvanceStep(); // Переключаем шаг туториала на диалог!
            }
        }
    }

    // ==========================================
    // 🏗️ АРХИТЕКТУРА ПОД БУДУЩИЕ ФИЧИ
    // ==========================================

    [ContextMenu("TEST: Activate Offline Gathering")]
    public void ActivateGathering(int daysAmount)
    {
        isAutoGatheringActive = true;
        // Запоминаем время, когда робот перестанет собирать лут
        DateTime endTime = DateTime.Now.AddDays(daysAmount);
        gatherSubscriptionEndTime = endTime.ToString();
        
        Debug.Log($"[RobotNanny] Режим оффлайн-сбора активирован до: {gatherSubscriptionEndTime}");
    }

    public void ProcessOfflineLoot()
    {
        // Эта функция будет вызываться GameManager'ом при заходе в игру.
        // Здесь мы будем парсить gatherSubscriptionEndTime, сравнивать с DateTime.Now
        // и выдавать ресурсы в инвентарь игроку.
    }

    public void ShowSpeechBubble()
    {
        // Здесь будем включать WorldSpace Canvas с облачком "..." над роботом
        Debug.Log("[RobotNanny] *Показывает облачко мыслей*");
    }

    public void Interact()
    {
        // Метод для клика по роботу.
        // Он будет передавать данные в DialogueManager, который включит затемнение
        // и покажет аватарки в стиле Ghost Town Adventures.
        Debug.Log("[RobotNanny] Игрок кликнул! Запускаю систему диалогов.");
        
        // В будущем тут будет что-то вроде:
        // DialogueManager.Instance.StartDialogue(nannyDialogueTree);
    }
}