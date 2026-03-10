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
    
    // === ТЕПЕРЬ ЭТА ПЕРЕМЕННАЯ ПУБЛИЧНАЯ ===
    public bool isSearching { get; private set; } = false;

    void Awake()
    {
        Instance = this;
        if (mainCamera == null) mainCamera = Camera.main;
    }

    public bool CanBeFound(string resourceName)
    {
        if (WorldInteractable.AllInteractables.Any(obj => obj != null && (obj.objectName == resourceName || obj.drops.Any(r => r.item != null && r.item.ItemName == resourceName)))) return true;
        if (BoarAI.AllEnemies.Any(enemy => enemy != null && enemy.lootTable.Any(loot => loot.item != null && loot.item.ItemName == resourceName))) return true;
        return false;
    }

    public void FindResourceNodes(string resourceName)
    {
        if (isSearching) return;

        Transform nearestTarget = null;
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
                if (d < minDist) { minDist = d; nearestTarget = obj.transform; }
            }
        }

        foreach (var enemy in BoarAI.AllEnemies)
        {
            if (enemy == null) continue;
            bool dropsResource = enemy.lootTable.Any(loot => loot.item != null && loot.item.ItemName == resourceName);
            
            if (dropsResource)
            {
                float d = Vector3.Distance(playerPos, enemy.transform.position);
                if (d < minDist) { minDist = d; nearestTarget = enemy.transform; }
            }
        }

        if (nearestTarget != null)
        {
            StartCoroutine(ShowObjectRoutine(nearestTarget));
        }
        else
        {
            Debug.Log($"<color=orange>Поиск: На этой локации предмет '{resourceName}' не найти!</color>");
        }
    }

    IEnumerator ShowObjectRoutine(Transform target)
    {
        isSearching = true;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameStateManager.GameState.Cutscene);

        // УБРАЛИ Time.timeScale = 0f; Мир снова живет!

        if (cameraFollowScript != null) cameraFollowScript.enabled = false;
        if (playerController != null) playerController.enabled = false;

        if (CraftingPanel.Instance != null) 
            CraftingPanel.Instance.gameObject.SetActive(false); 

        Vector3 originalPos = mainCamera.transform.position;
        Quaternion originalRot = mainCamera.transform.rotation;

        // Полет ТУДА (Вернули обычный Time.deltaTime)
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * cameraTransitionSpeed;
            
            Vector3 targetPos = target.position + cameraOffset;
            Quaternion targetRot = Quaternion.LookRotation(target.position - mainCamera.transform.position);
            
            mainCamera.transform.position = Vector3.Lerp(originalPos, targetPos, t);
            mainCamera.transform.rotation = Quaternion.Lerp(originalRot, targetRot, t);
            yield return null;
        }

        if (pointerPrefab != null)
        {
            Vector3 pointerPos = target.position;
            Collider col = target.GetComponent<Collider>();
            if (col != null) pointerPos = new Vector3(target.position.x, col.bounds.max.y + 1.5f, target.position.z);
            else pointerPos += Vector3.up * 1.5f;
            
            currentPointer = Instantiate(pointerPrefab, pointerPos, Quaternion.identity);
            currentPointer.transform.SetParent(target); 
        }

        // Вернули обычный WaitForSeconds
        yield return new WaitForSeconds(showDuration);

        if (currentPointer != null) Destroy(currentPointer);

        // Полет ОБРАТНО
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * cameraTransitionSpeed;
            mainCamera.transform.position = Vector3.Lerp(target.position + cameraOffset, originalPos, t);
            mainCamera.transform.rotation = Quaternion.Lerp(Quaternion.LookRotation(target.position - (target.position + cameraOffset)), originalRot, t);
            yield return null;
        }

        isSearching = false;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameStateManager.GameState.Playing);
        else
        {
            Cursor.visible = true; 
            Cursor.lockState = CursorLockMode.None; 
        }

        // УБРАЛИ Time.timeScale = 1f;

        if (cameraFollowScript != null) cameraFollowScript.enabled = true;
        if (playerController != null) playerController.enabled = true;

        Debug.Log("Поиск завершен. Управление возвращено.");
    }
}