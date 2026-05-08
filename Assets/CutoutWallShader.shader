Shader "Custom/CutoutWallShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        _HoleRadius ("Hole Radius", Float) = 2.5
        _CutoutActive ("Cutout Active", Float) = 0.0
        
        // Технический параметр для режима Cutout
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5 
    }
    SubShader
    {
        // 1. МЕНЯЕМ ТЕГИ НА CUTOUT (Теперь дом твердый, без глюков сортировки)
        Tags { "Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout" }
        LOD 200
        
        // 2. ВЫКЛЮЧАЕМ ОТСЕЧЕНИЕ НЕВИДИМЫХ ГРАНЕЙ (Cull Off)
        // Чтобы, прорезав переднюю стену, мы видели внутреннюю часть задней стены, а не пустоту!
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard alphatest:_Cutoff addshadow

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos; 
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        float _HoleRadius;
        float _CutoutActive;
        float3 _PlayerPos; 

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            
            if (_CutoutActive > 0.5)
            {
                // 1. Вектор от камеры до игрока (наш главный "лазер")
                // _WorldSpaceCameraPos - это встроенная переменная Unity
                float3 camToPlayer = _PlayerPos - _WorldSpaceCameraPos;
                float distToPlayer = length(camToPlayer);
                float3 dirToPlayer = camToPlayer / distToPlayer; // Направление лазера

                // 2. Вектор от камеры до текущего пикселя стены
                float3 camToPixel = IN.worldPos - _WorldSpaceCameraPos;
                
                // 3. Проекция (насколько пиксель удален от камеры вдоль нашего лазера)
                float projection = dot(camToPixel, dirToPlayer);
                
                // 4. Если пиксель находится строго МЕЖДУ камерой и игроком (не сзади героя и не сзади камеры)
                if (projection > 0 && projection < distToPlayer)
                {
                    // 5. Высчитываем кратчайшее расстояние от пикселя до лазера
                    float tubeDist = length(cross(camToPixel, dirToPlayer));
                    
                    // Вырезаем цилиндр (трубу) нужного радиуса!
                    clip(tubeDist - _HoleRadius);
                }
            }

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Transparent/Cutout/VertexLit"
}