using UnityEngine;
using System.Collections;
using System.Linq;

public class ResourceFinder : MonoBehaviour
{
    public static ResourceFinder Instance;

    [Header("Components to Control")]
    public Camera mainCamera;
    public MonoBehaviour cameraFollowScript; 
    public MonoBehaviour playerController;

    [Header("Settings")]
    public GameObject pointerPrefab;
    public float cameraTransitionSpeed = 2.0f;
    public float showDuration = 3.0f;
    public Vector3 cameraOffset = new Vector3(0, 10, -10);

    private GameObject currentPointer;
    private bool isSearching = false;

    void Awake()
    {
        Instance = this;
        if (mainCamera == null) mainCamera = Camera.main;
    }

    public void FindResourceNodes(string resourceName)
    {
        if (isSearching) return;

        WorldInteractable nearestObj = null;
        float minDist = float.MaxValue;
        Vector3 playerPos = mainCamera.transform.position;

        foreach (var obj in WorldInteractable.AllInteractables)
        {
            if (obj == null) continue;

            bool dropsResource = obj.drops.Any(rule => rule.item != null && rule.item.ItemName == resourceName);
            if (!dropsResource && obj.objectName == resourceName) dropsResource = true;

            if (dropsResource)
            {
                float d = Vector3.Distance(playerPos, obj.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearestObj = obj;
                }
            }
        }

        if (nearestObj != null)
        {
            StartCoroutine(ShowObjectRoutine(nearestObj.transform));
        }
        else
        {
            Debug.Log($"<color=orange>Поиск: На этой локации предмет '{resourceName}' не найти!</color>");
        }
    }

    IEnumerator ShowObjectRoutine(Transform target)
    {
        isSearching = true;
        
        // 1. ПЕРЕХОДИМ В РЕЖИМ КАТСЦЕНЫ
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameStateManager.GameState.Cutscene);

        if (cameraFollowScript != null) cameraFollowScript.enabled = false;
        
        if (playerController != null) 
        {
            playerController.enabled = false;
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic) rb.velocity = Vector3.zero;
        }

        if (CraftingPanel.Instance != null) 
            CraftingPanel.Instance.gameObject.SetActive(false); 

        Vector3 originalPos = mainCamera.transform.position;
        Quaternion originalRot = mainCamera.transform.rotation;
        Vector3 targetPos = target.position + cameraOffset;
        Quaternion targetRot = Quaternion.LookRotation(target.position - targetPos);

        // Полет ТУДА
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * cameraTransitionSpeed;
            mainCamera.transform.position = Vector3.Lerp(originalPos, targetPos, t);
            mainCamera.transform.rotation = Quaternion.Lerp(originalRot, targetRot, t);
            yield return null;
        }

        // Поинтер
        if (pointerPrefab != null)
        {
            Vector3 pointerPos = target.position;
            Collider col = target.GetComponent<Collider>();
            if (col != null)
            {
                pointerPos = new Vector3(target.position.x, col.bounds.max.y + 2.0f, target.position.z);
            }
            else
            {
                pointerPos += Vector3.up * 2.0f;
            }
            currentPointer = Instantiate(pointerPrefab, pointerPos, Quaternion.identity);
        }

        yield return new WaitForSeconds(showDuration);

        if (currentPointer != null) Destroy(currentPointer);

        // Полет ОБРАТНО
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * cameraTransitionSpeed;
            mainCamera.transform.position = Vector3.Lerp(targetPos, originalPos, t);
            mainCamera.transform.rotation = Quaternion.Lerp(targetRot, originalRot, t);
            yield return null;
        }

        // 2. ВОЗВРАЩЕНИЕ В ИГРУ
        isSearching = false;

        if (cameraFollowScript != null) cameraFollowScript.enabled = true;
        
        if (playerController != null) 
        {
            playerController.enabled = true;
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null) rb.WakeUp();
        }

        // --- ИСПРАВЛЕНИЕ: Используем Playing вместо Gameplay ---
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameStateManager.GameState.Playing);
        }
        else
        {
            Cursor.visible = true; 
            Cursor.lockState = CursorLockMode.None; 
        }

        Debug.Log("Поиск завершен. Управление возвращено.");
    }
}