using UnityEngine;
using System.Collections.Generic;

public class PatrolPath : MonoBehaviour
{
    // Цвет линий в редакторе
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform current = transform.GetChild(i);
            Transform next = transform.GetChild((i + 1) % transform.childCount); // Замыкаем круг
            
            Gizmos.DrawSphere(current.position, 0.3f);
            Gizmos.DrawLine(current.position, next.position);
        }
    }

    public Transform GetPoint(int index)
    {
        if (transform.childCount == 0) return null;
        return transform.GetChild(index % transform.childCount);
    }
    
    public int Length => transform.childCount;
}