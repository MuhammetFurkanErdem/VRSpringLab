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

    [Tooltip("Aktif ve sönmekte olan izlerin birbirinden hafifçe ayrılması.")]
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

    private LineRenderer activeRenderer;
    private LineRenderer fadingRenderer;

    private Rigidbody trackedWeight;

    private bool hasActiveTrace;
    private bool isFading;

    private int lastSide;

    private float activeStartY;
    private float activePeakY;
    private float activeBaseX;
    private float activeZ;

    private float fadeTime;

    private void Awake()
    {
        activeRenderer = GetComponent<LineRenderer>();
        ConfigureRenderer(activeRenderer);

        GameObject fadingObject = new GameObject("FadingTrace");
        fadingObject.transform.SetParent(transform, false);

        fadingRenderer = fadingObject.AddComponent<LineRenderer>();
        fadingRenderer.sharedMaterial = activeRenderer.sharedMaterial;
        fadingRenderer.sortingLayerID = activeRenderer.sortingLayerID;
        fadingRenderer.sortingOrder = activeRenderer.sortingOrder;
        fadingRenderer.alignment = activeRenderer.alignment;
        fadingRenderer.textureMode = activeRenderer.textureMode;

        ConfigureRenderer(fadingRenderer);
        ClearTraces();
    }

    private void Update()
    {
        UpdateFadingTrace();

        if (springSimulation == null ||
            !springSimulation.HasWeight ||
            springSimulation.CurrentWeight == null)
        {
            if (trackedWeight != null)
            {
                trackedWeight = null;
                ClearTraces();
            }

            return;
        }

        Rigidbody weight = springSimulation.CurrentWeight;

        float relativePosition =
            springSimulation.DisplacementMeters -
            springSimulation.EquilibriumDisplacementMeters;

        int currentSide = GetSide(relativePosition);

        if (trackedWeight != weight)
        {
            trackedWeight = weight;
            ClearTraces();
            BeginActiveTrace(weight, weight.position.y);
            lastSide = currentSide;
        }

        if (currentSide != 0 &&
            lastSide != 0 &&
            currentSide != lastSide)
        {
            CompleteActiveTrace();

            // Displacement aşağı yönde pozitifken world Y azalır.
            // Bu yüzden mevcut kütle Y'sine göreli displacement eklenerek
            // sabit equilibrium world Y bulunur.
            float equilibriumY =
                weight.position.y + relativePosition;

            BeginActiveTrace(weight, equilibriumY);
        }

        if (currentSide != 0)
        {
            lastSide = currentSide;
        }

        UpdateActiveTrace(weight.position.y);
    }

    private void BeginActiveTrace(
        Rigidbody weight,
        float startY)
    {
        activeStartY = startY;
        activePeakY = startY;

        activeBaseX =
            weight.position.x + horizontalOffset;

        activeZ =
            weight.position.z + depthOffset;

        hasActiveTrace = true;
        activeRenderer.enabled = true;
        SetRendererAlpha(activeRenderer, 1f);
        DrawActiveTrace(weight.position.y);
    }

    private void UpdateActiveTrace(float currentY)
    {
        if (!hasActiveTrace)
            return;

        if (Mathf.Abs(currentY - activeStartY) >
            Mathf.Abs(activePeakY - activeStartY))
        {
            activePeakY = currentY;
        }

        // Aktif ucun extrema'da takılmaması için her frame doğrudan
        // simülasyonun hareket ettirdiği Rigidbody world Y'si kullanılır.
        DrawActiveTrace(currentY);
    }

    private void DrawActiveTrace(float currentY)
    {
        float x = activeBaseX + laneSeparation * 0.5f;

        activeRenderer.SetPosition(
            0,
            new Vector3(x, activeStartY, activeZ));

        activeRenderer.SetPosition(
            1,
            new Vector3(x, currentY, activeZ));
    }

    private void CompleteActiveTrace()
    {
        if (!hasActiveTrace)
            return;

        float x = activeBaseX - laneSeparation * 0.5f;

        fadingRenderer.SetPosition(
            0,
            new Vector3(x, activeStartY, activeZ));

        fadingRenderer.SetPosition(
            1,
            new Vector3(x, activePeakY, activeZ));

        fadeTime = 0f;
        isFading = true;
        fadingRenderer.enabled = true;
        SetRendererAlpha(fadingRenderer, 1f);

        hasActiveTrace = false;
        activeRenderer.enabled = false;
    }

    private void UpdateFadingTrace()
    {
        if (!isFading || springSimulation == null)
            return;

        if (springSimulation.IsPaused)
            return;

        fadeTime +=
            Time.deltaTime * springSimulation.SimulationSpeed;

        float alpha = fadeDuration > 0f
            ? 1f - fadeTime / fadeDuration
            : 0f;

        alpha = Mathf.Clamp01(alpha);
        SetRendererAlpha(fadingRenderer, alpha);

        if (alpha <= 0f)
        {
            isFading = false;
            fadingRenderer.enabled = false;
        }
    }

    private int GetSide(float relativePosition)
    {
        if (relativePosition > centerThreshold)
            return 1;

        if (relativePosition < -centerThreshold)
            return -1;

        return 0;
    }

    private void ConfigureRenderer(LineRenderer renderer)
    {
        renderer.positionCount = 2;
        renderer.useWorldSpace = true;
        renderer.startWidth = lineWidth;
        renderer.endWidth = lineWidth;
        renderer.numCapVertices = 2;
        renderer.enabled = false;
    }

    private void SetRendererAlpha(
        LineRenderer renderer,
        float alpha)
    {
        Color color = traceColor;
        color.a *= alpha;

        renderer.startColor = color;
        renderer.endColor = color;
    }

    private void ClearTraces()
    {
        hasActiveTrace = false;
        isFading = false;
        lastSide = 0;
        fadeTime = 0f;

        if (activeRenderer != null)
            activeRenderer.enabled = false;

        if (fadingRenderer != null)
            fadingRenderer.enabled = false;
    }

    private void OnDisable()
    {
        trackedWeight = null;
        ClearTraces();
    }
}
