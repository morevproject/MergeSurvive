using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Gradient")]
public class UIGradient : BaseMeshEffect
{
    public Color topColor = new Color(0, 0, 0, 0f);    // Верх - прозрачный
    public Color bottomColor = new Color(0, 0, 0, 1f); // Низ - плотный
    
    [Range(0f, 1f)]
    public float solidPortion = 0.5f; // НОВАЯ ФИЧА: Какая часть снизу будет сплошной (0.5 = половина)

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        UIVertex vertex = new UIVertex();
        vh.PopulateUIVertex(ref vertex, 0);
        float bottomY = vertex.position.y;
        float topY = vertex.position.y;

        for (int i = 1; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            if (vertex.position.y < bottomY) bottomY = vertex.position.y;
            if (vertex.position.y > topY) topY = vertex.position.y;
        }

        float uiElementHeight = topY - bottomY;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            byte alpha = vertex.color.a;
            float normalizedY = (vertex.position.y - bottomY) / uiElementHeight;
            
            // МАГИЯ ЗДЕСЬ: Если мы ниже solidPortion, рисуем сплошной цвет. Выше - растворяем.
            float gradientT = 0f;
            if (normalizedY > solidPortion)
            {
                gradientT = (normalizedY - solidPortion) / (1f - solidPortion);
            }

            vertex.color = Color.Lerp(bottomColor, topColor, gradientT);
            vertex.color.a = (byte)((vertex.color.a * alpha) / 255);
            vh.SetUIVertex(vertex, i);
        }
    }
}