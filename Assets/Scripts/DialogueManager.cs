using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.Events;

[Serializable]
public struct DialogueNode
{
    public string speakerName;
    [TextArea(2, 5)] public string text;
    public Sprite avatarSprite;
    public bool isLeftAvatar; // true = игрок слева, false = NPC справа

    [Tooltip("Событие, которое сработает В МОМЕНТ появления этой реплики на экране")]
    public UnityEvent onNodeStart;
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public Image leftAvatar;
    public Image rightAvatar;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public GameObject gameplayUI;

    [Header("Settings")]
    public float typeSpeed = 0.03f; // Скорость печатной машинки

    private Queue<DialogueNode> nodesQueue = new Queue<DialogueNode>();
    private bool isTyping = false;
    private string currentFullText = "";
    
    // Событие, которое сработает, когда диалог полностью закончится
    public Action OnDialogueFinished; 

    private void Awake()
    {
        // Убираем проверки if (Instance == null). 
        // Просто всегда перезаписываем ссылку на актуальный свежий объект в текущей сцене!
        Instance = this; 
    }

    // Добавили Action onCompleteCallback
    public void StartDialogue(List<DialogueNode> nodes, Action onCompleteCallback = null)
    {
        if (GameStateManager.Instance != null) 
            GameStateManager.Instance.SetState(GameStateManager.GameState.MenuOpen);

        if (gameplayUI != null) gameplayUI.SetActive(false); 
        
        dialoguePanel.SetActive(true);
        nodesQueue.Clear();

        foreach (var node in nodes)
        {
            nodesQueue.Enqueue(node);
        }

        // Перезаписываем событие завершения
        OnDialogueFinished = null; 
        if (onCompleteCallback != null)
        {
             OnDialogueFinished += onCompleteCallback;
        }

        DisplayNextNode();
    }

    public void DisplayNextNode()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = currentFullText;
            isTyping = false;
            return;
        }

        if (nodesQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueNode node = nodesQueue.Dequeue();
        
        // === ВОТ ЗДЕСЬ СРАБАТЫВАЕТ СОБЫТИЕ КОНКРЕТНОЙ ФРАЗЫ ===
        if (node.onNodeStart != null)
        {
            node.onNodeStart.Invoke();
        }
        // ======================================================

        nameText.text = node.speakerName;
        currentFullText = node.text;

        // Настраиваем аватарки (выделяем говорящего цветом/размером)
        if (node.isLeftAvatar)
        {
            leftAvatar.sprite = node.avatarSprite;
            leftAvatar.color = Color.white;
            rightAvatar.color = Color.gray; // Затемняем того, кто молчит
        }
        else
        {
            rightAvatar.sprite = node.avatarSprite;
            rightAvatar.color = Color.white;
            leftAvatar.color = Color.gray;
        }

        StartCoroutine(TypeSentence(node.text));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        dialogueText.text = "";
        isTyping = true;

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
    }

    private void EndDialogue()
    {
        // Размораживаем мир
        if (GameStateManager.Instance != null) 
            GameStateManager.Instance.SetState(GameStateManager.GameState.Playing);
            
        if (gameplayUI != null) gameplayUI.SetActive(true); // Возвращаем кнопки игры
        
        dialoguePanel.SetActive(false);
        OnDialogueFinished?.Invoke(); // Сообщаем Туториалу, что мы закончили!
    }

    // Эту кнопку нужно повесить на невидимую кнопку-подложку на весь экран внутри DialoguePanel
    public void OnPanelClicked()
    {
        DisplayNextNode();
    }
}