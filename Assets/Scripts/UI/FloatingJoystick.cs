using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public static FloatingJoystick Instance;

    [Header("UI References")]
    public RectTransform background; 
    public RectTransform handle;     
    
    [Header("Settings")]
    public float handleRange = 1f;   
    public bool hideOnRelease = true; 

    public Vector2 Direction { get; private set; } = Vector2.zero;

    private Canvas canvas;
    private Camera cam;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            cam = canvas.worldCamera;

        if (hideOnRelease) background.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // --- БЛОКИРОВКА ---
        // Если открыт инвентарь или крафт - запрещаем появление джойстика
        if (GameStateManager.Instance.CurrentState != GameStateManager.GameState.Playing) 
        {
            return; 
        }
        // ------------------

        if (hideOnRelease)
        {
            background.gameObject.SetActive(true);
            background.position = eventData.position;
        }
        
        handle.anchoredPosition = Vector2.zero;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Тоже добавляем проверку, чтобы нельзя было "подхватить" джойстик случайно
        if (GameStateManager.Instance.CurrentState != GameStateManager.GameState.Playing) 
        {
            // Если вдруг открыли инвентарь во время бега - сбрасываем джойстик
            OnPointerUp(eventData); 
            return; 
        }

        Vector2 position = Vector2.zero;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, cam, out position))
        {
            position.x = (position.x / background.sizeDelta.x);
            position.y = (position.y / background.sizeDelta.y);

            Direction = new Vector2(position.x * 2, position.y * 2); 
            if (Direction.magnitude > 1.0f) Direction = Direction.normalized;

            handle.anchoredPosition = new Vector2(Direction.x * (background.sizeDelta.x / 2) * handleRange, Direction.y * (background.sizeDelta.y / 2) * handleRange);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Direction = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
        
        if (hideOnRelease)
        {
            background.gameObject.SetActive(false);
        }
    }
}