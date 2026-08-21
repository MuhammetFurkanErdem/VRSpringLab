using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PeriodTrace : MonoBehaviour
{
    [SerializeField]
    private SpringSimulation springSimulation;

    [Header("Trace Position")]
    [SerializeField]
    private float horizontalOffset = -0.30f;

    [Tooltip("Sol ve sağ dikey lane'in merkez çizgisine uzaklığı.")]
    [SerializeField]
    private float laneHalfWidth = 0.04f;

    [SerializeField]
    private float depthOffset = 0f;

    [Header("Trace Sampling")]
    [Tooltip("İki kalıcı örnek arasındaki simülasyon süresi.")]
    [SerializeField]
    private float sampleInterval = 0.025f;

    [Tooltip("Aynı lane üzerinde yeni history noktası için gereken Y hareketi.")]
    [SerializeField]
    private float minVerticalDistance = 0.004f;

    [Tooltip("Bir noktanın kaç simülasyon saniyesi boyunca izde kalacağı.")]
    [SerializeField]
    private float traceLifetime = 3f;

    [Header("Trace Appearance")]
    [SerializeField]
    private float lineWidth = 0.015f;

    [SerializeField]
    private Color traceColor = Color.white;

    private const int FadeKeyCount = 8;
    private const int MovingUp = -1;
    private const int MovingDown = 1;

    [Header("Turning Point Detection")]
    [Tooltip("Bu hızın altındaki değerler yön değiştirme olarak kabul edilmez.")]
    [SerializeField]
    private float velocityThreshold = 0.02f;

    private readonly List<TraceSample> samples =
        new List<TraceSample>();

    private readonly Gradient traceGradient =
        new Gradient();

    private readonly GradientColorKey[] colorKeys =
        new GradientColorKey[2];

    private readonly GradientAlphaKey[] alphaKeys =
        new GradientAlphaKey[FadeKeyCount];

    private LineRenderer traceRenderer;
    private Rigidbody trackedWeight;

    private float traceTime;
    private float lastSampleTime;
    private Vector3 lastSamplePosition;

    private float traceBaseX;
    private float traceZ;
    private int currentDirection;
    private float directionExtremeY;

    private struct TraceSample
    {
        public Vector3 Position;
        public float Time;

        public TraceSample(Vector3 position, float time)
        {
            Position = position;
            Time = time;
        }
    }

    private void Awake()
    {
        traceRenderer = GetComponent<LineRenderer>();
        ConfigureRenderer();
        ClearTrace();
    }

    private void LateUpdate()
    {
        if (springSimulation == null ||
            !springSimulation.HasWeight ||
            springSimulation.CurrentWeight == null)
        {
            if (trackedWeight != null)
            {
                trackedWeight = null;
                ClearTrace();
            }

            return;
        }

        Rigidbody weight = springSimulation.CurrentWeight;

        if (trackedWeight != weight)
            BeginTracking(weight);

        // Pause sırasında sampling, aktif uç ve lifetime tamamen donar.
        if (springSimulation.IsPaused)
            return;

        float simulationDeltaTime =
            GetSimulationFrameDeltaTime();

        if (simulationDeltaTime <= 0f)
            return;

        traceTime += simulationDeltaTime;

        RemoveExpiredSamples();
        UpdateDirectionExtreme(weight.position.y);

        int detectedDirection = GetMotionDirection();
        bool directionChanged =
            detectedDirection != 0 &&
            detectedDirection != currentDirection;

        if (directionChanged)
        {
            AddTurningPointBridge(
                directionExtremeY,
                currentDirection,
                detectedDirection);

            currentDirection = detectedDirection;
            directionExtremeY = weight.position.y;
        }

        Vector3 currentPoint = GetLanePoint(
            currentDirection,
            weight.position.y);

        if (!directionChanged && ShouldAddSample(currentPoint))
            AddSample(currentPoint);

        DrawTrace(currentPoint);
    }

    private void BeginTracking(Rigidbody weight)
    {
        ClearTrace();

        trackedWeight = weight;
        traceBaseX = weight.position.x + horizontalOffset;
        traceZ = weight.position.z + depthOffset;

        int detectedDirection = GetMotionDirection();
        currentDirection = detectedDirection != 0
            ? detectedDirection
            : MovingDown;
        directionExtremeY = weight.position.y;

        Vector3 firstPoint = GetLanePoint(
            currentDirection,
            weight.position.y);

        AddSample(firstPoint);
        DrawTrace(firstPoint);
    }

    private float GetSimulationFrameDeltaTime()
    {
        float fixedDeltaTime =
            Mathf.Max(Time.fixedDeltaTime, 0.0001f);

        float simulationScale =
            springSimulation.SimulationDeltaTime /
            fixedDeltaTime;

        return Time.deltaTime * simulationScale;
    }

    private Vector3 GetLanePoint(int direction, float worldY)
    {
        float laneOffset = Mathf.Abs(laneHalfWidth);

        float laneX = direction == MovingUp
            ? traceBaseX - laneOffset
            : traceBaseX + laneOffset;

        return new Vector3(
            laneX,
            worldY,
            traceZ);
    }

    private void AddTurningPointBridge(
        float worldY,
        int previousDirection,
        int nextDirection)
    {
        // Aynı gerçek Y'de iki vertex: eski lane'in sonu ve yeni lane'in
        // başlangıcı. LineRenderer bunların arasını kısa yatay çizgi yapar.
        AddSample(GetLanePoint(previousDirection, worldY));
        AddSample(GetLanePoint(nextDirection, worldY));
    }

    private void UpdateDirectionExtreme(float worldY)
    {
        if (currentDirection == MovingDown)
            directionExtremeY = Mathf.Min(directionExtremeY, worldY);
        else if (currentDirection == MovingUp)
            directionExtremeY = Mathf.Max(directionExtremeY, worldY);
    }

    private bool ShouldAddSample(Vector3 currentPoint)
    {
        if (samples.Count == 0)
            return true;

        float interval = Mathf.Max(sampleInterval, 0.005f);
        float elapsed = traceTime - lastSampleTime;

        if (elapsed < interval)
            return false;

        float verticalDistance =
            Mathf.Abs(currentPoint.y - lastSamplePosition.y);

        return verticalDistance >=
               Mathf.Max(minVerticalDistance, 0f);
    }

    private void AddSample(Vector3 position)
    {
        samples.Add(new TraceSample(position, traceTime));

        lastSamplePosition = position;
        lastSampleTime = traceTime;
    }

    private void RemoveExpiredSamples()
    {
        float lifetime = Mathf.Max(traceLifetime, 0.05f);
        int expiredCount = 0;

        while (expiredCount < samples.Count &&
               traceTime - samples[expiredCount].Time >= lifetime)
        {
            expiredCount++;
        }

        if (expiredCount > 0)
            samples.RemoveRange(0, expiredCount);
    }

    private void DrawTrace(Vector3 activeTip)
    {
        if (traceRenderer == null)
            return;

        int pointCount = samples.Count + 1;
        traceRenderer.positionCount = pointCount;

        for (int i = 0; i < samples.Count; i++)
            traceRenderer.SetPosition(i, samples[i].Position);

        // Kalıcı sample aralığından bağımsız olarak aktif uç her frame
        // gerçek Rigidbody world Y konumuna gider.
        traceRenderer.SetPosition(pointCount - 1, activeTip);

        UpdateFadeGradient(pointCount);
        traceRenderer.enabled = pointCount >= 2;
    }

    private void UpdateFadeGradient(int pointCount)
    {
        Color gradientColor = traceColor;
        gradientColor.a = 1f;

        colorKeys[0] =
            new GradientColorKey(gradientColor, 0f);

        colorKeys[1] =
            new GradientColorKey(gradientColor, 1f);

        float lifetime = Mathf.Max(traceLifetime, 0.05f);

        for (int i = 0; i < FadeKeyCount; i++)
        {
            float normalizedPosition =
                i / (FadeKeyCount - 1f);

            float pointTime = GetTimeAtPosition(
                normalizedPosition,
                pointCount);

            float remainingLifetime = Mathf.Clamp01(
                1f - (traceTime - pointTime) / lifetime);

            float alpha =
                Mathf.SmoothStep(0f, 1f, remainingLifetime) *
                traceColor.a;

            // Aktif uç her zaman tam görünürdür.
            if (i == FadeKeyCount - 1)
                alpha = traceColor.a;

            alphaKeys[i] =
                new GradientAlphaKey(alpha, normalizedPosition);
        }

        traceGradient.SetKeys(colorKeys, alphaKeys);
        traceRenderer.colorGradient = traceGradient;
    }

    private float GetTimeAtPosition(
        float normalizedPosition,
        int pointCount)
    {
        if (pointCount <= 1)
            return traceTime;

        float exactIndex =
            normalizedPosition * (pointCount - 1);

        int lowerIndex = Mathf.FloorToInt(exactIndex);
        int upperIndex = Mathf.Min(lowerIndex + 1, pointCount - 1);
        float interpolation = exactIndex - lowerIndex;

        return Mathf.Lerp(
            GetPointTime(lowerIndex),
            GetPointTime(upperIndex),
            interpolation);
    }

    private float GetPointTime(int index)
    {
        return index >= samples.Count
            ? traceTime
            : samples[index].Time;
    }

    private int GetMotionDirection()
    {
        float velocity =
            springSimulation.VelocityMetersPerSecond;

        float threshold =
            Mathf.Max(velocityThreshold, 0.0001f);

        // SpringSimulation displacement'i aşağı yönde pozitif tutuyor.
        if (velocity > threshold)
            return MovingDown;

        if (velocity < -threshold)
            return MovingUp;

        return 0;
    }

    private void ConfigureRenderer()
    {
        traceRenderer.positionCount = 0;
        traceRenderer.useWorldSpace = true;
        traceRenderer.startWidth = lineWidth;
        traceRenderer.endWidth = lineWidth;
        traceRenderer.numCornerVertices = 0;
        traceRenderer.numCapVertices = 2;
        traceRenderer.enabled = false;
    }

    private void ClearTrace()
    {
        samples.Clear();

        traceTime = 0f;
        lastSampleTime = 0f;
        lastSamplePosition = Vector3.zero;
        currentDirection = 0;
        directionExtremeY = 0f;

        if (traceRenderer != null)
        {
            traceRenderer.positionCount = 0;
            traceRenderer.enabled = false;
        }
    }

    private void OnDisable()
    {
        trackedWeight = null;
        ClearTrace();
    }
}
