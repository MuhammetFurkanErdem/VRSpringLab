using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PeriodTrace : MonoBehaviour
{
    [SerializeField]
    private SpringSimulation springSimulation;

    [Header("Trace Position")]
    [SerializeField]
    private float horizontalOffset = -0.30f;

    [SerializeField]
    private float depthOffset = 0f;

    [Tooltip("Ardışık izlerin hafif yana kayması.")]
    [SerializeField]
    private float laneSeparation = 0.025f;

    [Header("Trace Appearance")]
    [SerializeField]
    private float lineWidth = 0.015f;

    [SerializeField]
    private Color traceColor = Color.white;

    [Tooltip("Tamamlanan izin kaç simülasyon saniyesinde kaybolacağı.")]
    [SerializeField]
    private float fadeDuration = 0.8f;

    [Header("Detection")]
    [SerializeField]
    private float centerThreshold = 0.002f;

    [Header("Pool")]
    [SerializeField]
    private int strokeCount = 6;

    private class TraceStroke
    {
        public LineRenderer renderer;

        public bool active;
        public bool fading;

        public float fadeTime;

        public float centerY;
        public float extremeY;

        // -1 = merkezin üstü
        //  1 = merkezin altı
        public int side;

        public float x;
        public float z;
    }

    private TraceStroke[] strokes;

    private int currentStrokeIndex = -1;
    private int nextStrokeIndex;

    private int lastSide;

    private bool hasWeight;

    private void Awake()
    {
        CreateStrokePool();
        ClearAllStrokes();
    }

    private void Update()
    {
        UpdateFadingStrokes();

        if (springSimulation == null ||
            !springSimulation.HasWeight ||
            springSimulation.CurrentWeight == null)
        {
            if (hasWeight)
            {
                ClearAllStrokes();
                hasWeight = false;
            }

            return;
        }

        Rigidbody weight =
            springSimulation.CurrentWeight;

        float relativePosition =
            springSimulation.DisplacementMeters -
            springSimulation.EquilibriumDisplacementMeters;

        int currentSide =
            GetSide(relativePosition);

        // Ağırlık yeni bağlandı.
        if (!hasWeight)
        {
            hasWeight = true;

            lastSide = currentSide;
            currentStrokeIndex = -1;

            return;
        }

        // ------------------------------------------------
        // Salınım merkezini geçti mi?
        // ------------------------------------------------

        if (currentSide != 0 &&
            lastSide != 0 &&
            currentSide != lastSide)
        {
            StartNewStroke(
                weight,
                currentSide,
                relativePosition
            );
        }

        if (currentSide != 0)
        {
            lastSide = currentSide;
        }

        UpdateCurrentStroke(weight);
    }

    // ------------------------------------------------
    // Yeni yarım salınım başlat
    // ------------------------------------------------

    private void StartNewStroke(
        Rigidbody weight,
        int newSide,
        float relativePosition)
    {
        // Önceki aktif çizgi artık tamamlandı.
        if (currentStrokeIndex >= 0)
        {
            TraceStroke oldStroke =
                strokes[currentStrokeIndex];

            oldStroke.active = false;
            oldStroke.fading = true;
            oldStroke.fadeTime = 0f;
        }

        currentStrokeIndex =
            nextStrokeIndex;

        nextStrokeIndex =
            (nextStrokeIndex + 1) %
            strokes.Length;

        TraceStroke stroke =
            strokes[currentStrokeIndex];

        ResetStroke(stroke);

        // ------------------------------------------------
        // Denge noktasının world-space Y konumu
        //
        // displacement arttıkça obje aşağı indiği için
        // relativePosition'ı mevcut Y'ye ekleyerek
        // denge merkezini buluyoruz.
        // ------------------------------------------------

        float equilibriumY =
            weight.position.y +
            relativePosition;

        float laneOffset =
            (currentStrokeIndex % 2 == 0)
                ? -laneSeparation * 0.5f
                : laneSeparation * 0.5f;

        stroke.centerY =
            equilibriumY;

        stroke.extremeY =
            equilibriumY;

        stroke.side =
            newSide;

        stroke.x =
            weight.position.x +
            horizontalOffset +
            laneOffset;

        stroke.z =
            weight.position.z +
            depthOffset;

        stroke.active = true;

        stroke.renderer.enabled = true;

        SetStrokeAlpha(
            stroke,
            1f
        );

        DrawStroke(stroke);
    }

    // ------------------------------------------------
    // Aktif iz ağırlıkla birlikte büyür
    // ------------------------------------------------

    private void UpdateCurrentStroke(
        Rigidbody weight)
    {
        if (currentStrokeIndex < 0)
            return;

        TraceStroke stroke =
            strokes[currentStrokeIndex];

        if (!stroke.active)
            return;

        float currentY =
            weight.position.y;

        // Merkezin altında.
        // Unity Y değeri aşağı indikçe küçülüyor.
        if (stroke.side > 0)
        {
            stroke.extremeY =
                Mathf.Min(
                    stroke.extremeY,
                    currentY
                );
        }

        // Merkezin üstünde.
        else if (stroke.side < 0)
        {
            stroke.extremeY =
                Mathf.Max(
                    stroke.extremeY,
                    currentY
                );
        }

        DrawStroke(stroke);
    }

    // ------------------------------------------------
    // Çizgiyi çiz
    // ------------------------------------------------

    private void DrawStroke(
        TraceStroke stroke)
    {
        Vector3 centerPoint =
            new Vector3(
                stroke.x,
                stroke.centerY,
                stroke.z
            );

        Vector3 extremePoint =
            new Vector3(
                stroke.x,
                stroke.extremeY,
                stroke.z
            );

        stroke.renderer.SetPosition(
            0,
            centerPoint
        );

        stroke.renderer.SetPosition(
            1,
            extremePoint
        );
    }

    // ------------------------------------------------
    // Eski çizgileri yavaşça söndür
    // ------------------------------------------------

    private void UpdateFadingStrokes()
    {
        if (strokes == null ||
            springSimulation == null)
        {
            return;
        }

        // Pause durumunda iz de donsun.
        float simulationDeltaTime =
            springSimulation.IsPaused
                ? 0f
                : Time.deltaTime *
                  springSimulation.SimulationSpeed;

        foreach (TraceStroke stroke
                 in strokes)
        {
            if (!stroke.fading)
                continue;

            stroke.fadeTime +=
                simulationDeltaTime;

            float alpha =
                1f -
                (stroke.fadeTime /
                 fadeDuration);

            alpha =
                Mathf.Clamp01(alpha);

            SetStrokeAlpha(
                stroke,
                alpha
            );

            if (alpha <= 0f)
            {
                stroke.fading = false;
                stroke.renderer.enabled = false;
            }
        }
    }

    // ------------------------------------------------
    // Relative position hangi tarafta?
    // ------------------------------------------------

    private int GetSide(
        float relativePosition)
    {
        if (relativePosition >
            centerThreshold)
        {
            return 1;
        }

        if (relativePosition <
            -centerThreshold)
        {
            return -1;
        }

        return 0;
    }

    // ------------------------------------------------
    // Renderer pool
    // ------------------------------------------------

    private void CreateStrokePool()
    {
        strokeCount =
            Mathf.Max(2, strokeCount);

        strokes =
            new TraceStroke[strokeCount];

        LineRenderer template =
            GetComponent<LineRenderer>();

        ConfigureRenderer(template);

        strokes[0] =
            new TraceStroke
            {
                renderer = template
            };

        for (int i = 1;
             i < strokeCount;
             i++)
        {
            GameObject strokeObject =
                new GameObject(
                    $"TraceStroke_{i}"
                );

            strokeObject.transform.SetParent(
                transform,
                false
            );

            LineRenderer renderer =
                strokeObject.AddComponent<
                    LineRenderer>();

            renderer.sharedMaterial =
                template.sharedMaterial;

            ConfigureRenderer(renderer);

            strokes[i] =
                new TraceStroke
                {
                    renderer = renderer
                };
        }
    }

    private void ConfigureRenderer(
        LineRenderer renderer)
    {
        renderer.positionCount = 2;

        renderer.useWorldSpace = true;

        renderer.startWidth =
            lineWidth;

        renderer.endWidth =
            lineWidth;

        renderer.numCapVertices = 2;

        renderer.enabled = false;
    }

    // ------------------------------------------------
    // Alpha
    // ------------------------------------------------

    private void SetStrokeAlpha(
        TraceStroke stroke,
        float alpha)
    {
        Color color =
            traceColor;

        color.a =
            alpha;

        stroke.renderer.startColor =
            color;

        stroke.renderer.endColor =
            color;
    }

    // ------------------------------------------------
    // Reset
    // ------------------------------------------------

    private void ResetStroke(
        TraceStroke stroke)
    {
        stroke.active = false;
        stroke.fading = false;

        stroke.fadeTime = 0f;

        stroke.renderer.enabled = false;
    }

    private void ClearAllStrokes()
    {
        if (strokes == null)
            return;

        foreach (TraceStroke stroke
                 in strokes)
        {
            ResetStroke(stroke);
        }

        currentStrokeIndex = -1;
        nextStrokeIndex = 0;
        lastSide = 0;
    }

    private void OnDisable()
    {
        ClearAllStrokes();
        hasWeight = false;
    }
}