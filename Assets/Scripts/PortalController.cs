using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class PortalController : MonoBehaviour
{
    // Описываем все возможные состояния нашего портала
    public enum PortalState 
    { 
        Locked,     // Неизвестен/Заблокирован (персонаж не знает что это)
        Ready,      // Разблокирован по сюжету (можно включить)
        Active      // Включен и работает
    }

    [Header("Текущее состояние")]
    public PortalState currentState = PortalState.Locked;

    [Header("Настройки визуала")]
    public ParticleSystem portalVFX;

    [Header("Сюжетные заглушки")]
    [Tooltip("Что скажет герой, если ткнуть в заблокированный портал?")]
    public List<DialogueNode> lockedDialogue; 

    private void Start()
    {
        // Применяем визуал согласно стартовому стейту
        UpdateVisuals();
    }

    private void OnMouseDown()
    {
        Interact();
    }

    // Главный метод взаимодействия (можно вызывать и не через клик, а, например, кнопкой UI)
    public void Interact()
    {
        switch (currentState)
        {
            case PortalState.Locked:
                // Герой не знает, что делать. Вызываем диалог-заглушку!
                if (DialogueManager.Instance != null && lockedDialogue != null && lockedDialogue.Count > 0)
                {
                    DialogueManager.Instance.StartDialogue(lockedDialogue);
                }
                else
                {
                    Debug.Log("[Portal] Герой: И что мне с этой штукой делать?");
                }
                break;

            case PortalState.Ready:
                // Игрок уже знает как включать -> Включаем!
                SetState(PortalState.Active);
                break;

            case PortalState.Active:
                // Выключаем
                SetState(PortalState.Ready);
                break;
        }
    }

    // Универсальный метод для внешних скриптов (квестов, инвентаря)
    public void SetState(PortalState newState)
    {
        currentState = newState;
        UpdateVisuals();
        Debug.Log($"[Portal] Состояние изменено на: {currentState}");
    }

    // Метод, который дергает частицы, анимации и звуки
    private void UpdateVisuals()
    {
        if (portalVFX != null)
        {
            if (currentState == PortalState.Active)
                portalVFX.Play();
            else
                portalVFX.Stop();
        }
    }
}