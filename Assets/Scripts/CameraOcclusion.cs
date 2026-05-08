using System.Collections.Generic;
using UnityEngine;

public class CameraOcclusion : MonoBehaviour
{
    [Header("Настройки")]
    public Transform target;
    public LayerMask obstacleLayer;
    public float castRadius = 1.5f; // Толщина нашего луча-трубы

    private List<Renderer> hiddenRenderers = new List<Renderer>();

    void LateUpdate()
    {
        if (target == null) return;

        Shader.SetGlobalVector("_PlayerPos", target.position);

        foreach (var r in hiddenRenderers)
        {
            if (r != null) r.material.SetFloat("_CutoutActive", 0f); 
        }
        hiddenRenderers.Clear();

        Vector3 direction = target.position - transform.position;
        float distance = direction.magnitude;

        // === ФИКС ЗДЕСЬ ===
        // Укорачиваем луч на 1.5 метра (чтобы сфера не цепляла стену за спиной).
        // Используем Mathf.Max, чтобы дистанция случайно не ушла в минус.
        float checkDistance = Mathf.Max(0f, distance - 1.5f); 

        // Обязательно нормализуем направление (direction.normalized)
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, castRadius, direction.normalized, checkDistance, obstacleLayer);
        // ==================

        foreach (var hit in hits)
        {
            Renderer r = hit.collider.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.SetFloat("_CutoutActive", 1f); 
                hiddenRenderers.Add(r);
            }
        }
    }
}