using UnityEngine;
using System; 

public class RobotNannyBrain : MonoBehaviour
{
    [Header("Tracking & Movement")]
    public Transform player;
    public float maxDistance = 5f; 
    public float stopDistance = 2.5f; 
    public float rotationSpeed = 5f; 
    public float smoothTime = 0.5f; // Чуть увеличил для красивого, вальяжного полета

    [Header("AI Logic")]
    public float decisionCooldown = 1f; // Как часто Няня "принимает решение" о перелете
    private float currentCooldown = 0f;
    private Vector3 currentTargetPosition; // Та самая фиксированная точка, куда мы летим
    private Vector3 currentVelocity; 

    private bool hasTriggeredTutorial = false; 

    [Header("Future Feature: Auto-Gathering")]
    public bool isAutoGatheringActive = false;
    public string gatherSubscriptionEndTime; 

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        // Со старта точкой назначения считаем текущее местоположение
        currentTargetPosition = transform.position;
    }

    private void Update()
    {
        if (player == null) return;

        HandleAIAndMovement();
    }

    private void HandleAIAndMovement()
    {
        // 1. ТАЙМЕР ПРИНЯТИЯ РЕШЕНИЙ (Уменьшаем каждый кадр)
        currentCooldown -= Time.deltaTime;
        
        bool isTutorialWaiting = (TutorialManager.Instance != null && TutorialManager.Instance.currentStep == TutorialManager.TutorialStep.WaitNannyApproach);

        // 2. ЛОГИКА "ПОДУМАТЬ" (Срабатывает только когда прошел КД)
        if (currentCooldown <= 0f)
        {
            Vector3 flatRobotPos = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 flatPlayerPos = new Vector3(player.position.x, 0, player.position.z);
            float distanceToPlayer = Vector3.Distance(flatRobotPos, flatPlayerPos);

            // Игрок ушел слишком далеко? Пора выбрать новую точку!
            if (distanceToPlayer > maxDistance || isTutorialWaiting)
            {
                Vector3 directionToPlayer = (player.position - transform.position).normalized;
                directionToPlayer.y = 0; 

                // Высчитываем новую точку для парковки
                currentTargetPosition = player.position - directionToPlayer * stopDistance;
                
                // Фиксируем высоту (например, на уровне 1.5 метра над ногами игрока)
                currentTargetPosition.y = player.position.y + 1f;

                // Уходим на кулдаун (Няня не будет менять цель следующую секунду)
                currentCooldown = decisionCooldown;
            }
        }

        // 3. ФИЗИЧЕСКИЙ ПОЛЕТ (Работает каждый кадр)
        // Так как currentTargetPosition теперь НЕ меняется каждый кадр, полет будет идеальным!
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            currentTargetPosition, 
            ref currentVelocity, 
            smoothTime
        );

        // 4. ПОВОРОТ
        // Пусть Няня всегда плавно поворачивается лицом к игроку - это добавляет ей "жизни"
        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // --- ТРИГГЕР ТУТОРИАЛА ---
        // Срабатывает, когда мы почти долетели до своей цели парковки
        float currentDistToTarget = Vector3.Distance(transform.position, currentTargetPosition);
        if (!hasTriggeredTutorial && isTutorialWaiting && currentDistToTarget < 1f)
        {
            hasTriggeredTutorial = true; 
            TutorialManager.Instance.AdvanceStep(); 
        }
    }

    // ==========================================
    // 🏗️ АРХИТЕКТУРА ПОД БУДУЩИЕ ФИЧИ (Оставлена без изменений)
    // ==========================================

    [ContextMenu("TEST: Activate Offline Gathering")]
    public void ActivateGathering(int daysAmount)
    {
        isAutoGatheringActive = true;
        DateTime endTime = DateTime.Now.AddDays(daysAmount);
        gatherSubscriptionEndTime = endTime.ToString();
        Debug.Log($"[RobotNanny] Режим оффлайн-сбора активирован до: {gatherSubscriptionEndTime}");
    }

    public void ProcessOfflineLoot() { }

    public void ShowSpeechBubble() { Debug.Log("[RobotNanny] *Показывает облачко мыслей*"); }

    public void Interact() { Debug.Log("[RobotNanny] Игрок кликнул! Запускаю систему диалогов."); }
}