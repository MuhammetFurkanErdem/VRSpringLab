using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[RequireComponent(typeof(LineRenderer))]
public class PeriodTrace : MonoBehaviour
{
    [SerializeField]
    private SpringSimulation springSimulation;

    [Header("Trace Position")]
    [SerializeField]
    private float horizontalOffset = -0.30f;

    [Tooltip("LEFT/CENTER ve CENTER/RIGHT lane'leri arasındaki uzaklık.")]
    [FormerlySerializedAs("laneHalfWidth")]
    [SerializeField]
    private float laneWidth = 0.04f;

    [SerializeField]
    private float depthOffset = 0f;

    [Header("Trace Detection")]
    [Tooltip("Bu hızın altında yeni bir güvenilir hareket yönü üretilmez.")]
    [SerializeField]
    private float velocityThreshold = 0.02f;

    [Tooltip("Equilibrium çevresindeki sign-change jitter dead zone'u.")]
    [SerializeField]
    private float centerThreshold = 0.002f;

    [Header("Trace Lifecycle")]
    [SerializeField]
    private float completedHoldDuration = 0.4f;

    [SerializeField]
    private float fadeDuration = 0.7f;

    [SerializeField]
    private float gapDuration = 0.5f;

    [Header("Trace Appearance")]
    [SerializeField]
    private float lineWidth = 0.015f;

    [SerializeField]
    private Color traceColor = Color.white;

    private const int AboveEquilibrium = -1;
    private const int BelowEquilibrium = 1;
    private const int MovingUp = -1;
    private const int MovingDown = 1;
    private const int FinalTraceSegmentCount = 5;
    private const string FinalTraceSegmentNamePrefix =
        "FinalTraceSegment_";

    private enum TracePhase
    {
        WaitingForStart,
        DrawingLeftOutbound,
        DrawingCenterDescent,
        DrawingRightReturn,
        Holding,
        Fading,
        Gap
    }

    private readonly Gradient traceGradient = new Gradient();
    private readonly GradientColorKey[] colorKeys =
        new GradientColorKey[2];
    private readonly GradientAlphaKey[] alphaKeys =
        new GradientAlphaKey[2];
    private readonly LineRenderer[] finalTraceSegmentRenderers =
        new LineRenderer[FinalTraceSegmentCount];

    private LineRenderer upperTraceRenderer;
    private Rigidbody trackedWeight;
    private TracePhase phase = TracePhase.WaitingForStart;

    private float phaseTime;
    private float traceBaseX;
    private float traceZ;
    private float cycleEquilibriumWorldY;
    private float upperTurningWorldY;
    private float lowerTurningWorldY;

    private float previousRelativePosition;
    private bool hasPreviousRelativePosition;
    private int stableEquilibriumSide;
    private int lastReliableMovementDirection;

    private void Awake()
    {
        InitializeRenderers();
        ResetTraceState();
    }

    private void LateUpdate()
    {
        if (springSimulation == null ||
            !springSimulation.HasWeight ||
            springSimulation.CurrentWeight == null)
        {
            if (trackedWeight != null ||
                phase != TracePhase.WaitingForStart)
            {
                ResetTraceState();
            }

            return;
        }

        Rigidbody weight = springSimulation.CurrentWeight;

        if (trackedWeight != weight)
            BeginTracking(weight);

        // Pause geometry'yi, aktif uçları ve lifecycle timer'larını dondurur.
        if (springSimulation.IsPaused)
            return;

        float simulationDeltaTime = GetSimulationFrameDeltaTime();

        if (simulationDeltaTime <= 0f)
            return;

        float relativePosition =
            springSimulation.DisplacementMeters -
            springSimulation.EquilibriumDisplacementMeters;

        int crossingDirection = DetectEquilibriumCrossing(
            previousRelativePosition,
            relativePosition);

        int newReliableDirection =
            UpdateReliableMovementDirection();

        float equilibriumWorldY =
            GetEquilibriumWorldY(weight, relativePosition);

        switch (phase)
        {
            case TracePhase.WaitingForStart:
                if (crossingDirection == MovingUp)
                {
                    StartContinuousTrace(
                        weight.position.y,
                        equilibriumWorldY);
                }

                break;

            case TracePhase.DrawingLeftOutbound:
                upperTurningWorldY = Mathf.Max(
                    upperTurningWorldY,
                    weight.position.y);

                DrawLeftOutbound(weight.position.y);

                if (newReliableDirection == MovingDown)
                    StartCenterDescent(weight.position.y);

                break;

            case TracePhase.DrawingCenterDescent:
                lowerTurningWorldY = Mathf.Min(
                    lowerTurningWorldY,
                    weight.position.y);

                DrawCenterDescent(weight.position.y);

                if (newReliableDirection == MovingUp)
                    StartRightReturn(weight.position.y);

                break;

            case TracePhase.DrawingRightReturn:
                DrawRightReturn(weight.position.y);

                if (HasReturnedFromLowerHalf(
                    crossingDirection,
                    relativePosition))
                {
                    FinishContinuousTrace(equilibriumWorldY);
                }

                break;

            case TracePhase.Holding:
            case TracePhase.Fading:
            case TracePhase.Gap:
                UpdateCompletedTrace(simulationDeltaTime);
                break;
        }

        previousRelativePosition = relativePosition;
        hasPreviousRelativePosition = true;
    }

    private void BeginTracking(Rigidbody weight)
    {
        ResetTraceState();

        trackedWeight = weight;
        traceBaseX = weight.position.x + horizontalOffset;
        traceZ = weight.position.z + depthOffset;

        previousRelativePosition =
            springSimulation.DisplacementMeters -
            springSimulation.EquilibriumDisplacementMeters;
        hasPreviousRelativePosition = true;
        stableEquilibriumSide =
            GetEquilibriumSide(previousRelativePosition);
        lastReliableMovementDirection =
            GetReliableMovementDirection();
    }

    private void StartContinuousTrace(
        float currentWeightWorldY,
        float equilibriumWorldY)
    {
        ClearRenderers();
        SetTraceAlpha(1f);

        phase = TracePhase.DrawingLeftOutbound;
        phaseTime = 0f;
        cycleEquilibriumWorldY = equilibriumWorldY;
        upperTurningWorldY = currentWeightWorldY;
        lowerTurningWorldY = equilibriumWorldY;
        lastReliableMovementDirection = MovingUp;

        DrawLeftOutbound(currentWeightWorldY);
    }

    private void StartCenterDescent(float currentWeightWorldY)
    {
        phase = TracePhase.DrawingCenterDescent;
        lowerTurningWorldY = currentWeightWorldY;
        DrawCenterDescent(currentWeightWorldY);
    }

    private void StartRightReturn(float currentWeightWorldY)
    {
        phase = TracePhase.DrawingRightReturn;
        DrawRightReturn(currentWeightWorldY);
    }

    private void FinishContinuousTrace(float equilibriumWorldY)
    {
        cycleEquilibriumWorldY = equilibriumWorldY;
        DrawRightReturn(equilibriumWorldY);
        ShowCompletedTraceSegments(equilibriumWorldY);

        if (upperTraceRenderer != null)
            upperTraceRenderer.enabled = false;

        phase = TracePhase.Holding;
        phaseTime = 0f;
    }

    private void ShowCompletedTraceSegments(float equilibriumWorldY)
    {
        Vector3[] finalPoints =
        {
            GetLanePoint(-1, equilibriumWorldY),
            GetLanePoint(-1, upperTurningWorldY),
            GetLanePoint(0, upperTurningWorldY),
            GetLanePoint(0, lowerTurningWorldY),
            GetLanePoint(1, lowerTurningWorldY),
            GetLanePoint(1, equilibriumWorldY)
        };

        for (int i = 0; i < FinalTraceSegmentCount; i++)
        {
            LineRenderer renderer =
                finalTraceSegmentRenderers[i];

            if (renderer == null)
                continue;

            renderer.positionCount = 2;
            renderer.SetPosition(0, finalPoints[i]);
            renderer.SetPosition(1, finalPoints[i + 1]);
            renderer.enabled = true;
        }
    }

    private void DrawLeftOutbound(float activeWorldY)
    {
        if (upperTraceRenderer == null)
            return;

        upperTraceRenderer.positionCount = 2;
        upperTraceRenderer.SetPosition(
            0,
            GetLanePoint(-1, cycleEquilibriumWorldY));
        upperTraceRenderer.SetPosition(
            1,
            GetLanePoint(-1, activeWorldY));
        upperTraceRenderer.enabled = true;
    }

    private void DrawCenterDescent(float activeWorldY)
    {
        if (upperTraceRenderer == null)
            return;

        upperTraceRenderer.positionCount = 4;
        upperTraceRenderer.SetPosition(
            0,
            GetLanePoint(-1, cycleEquilibriumWorldY));
        upperTraceRenderer.SetPosition(
            1,
            GetLanePoint(-1, upperTurningWorldY));
        upperTraceRenderer.SetPosition(
            2,
            GetLanePoint(0, upperTurningWorldY));
        upperTraceRenderer.SetPosition(
            3,
            GetLanePoint(0, activeWorldY));
        upperTraceRenderer.enabled = true;
    }

    private void DrawRightReturn(float activeWorldY)
    {
        if (upperTraceRenderer == null)
            return;

        upperTraceRenderer.positionCount = 6;
        upperTraceRenderer.SetPosition(
            0,
            GetLanePoint(-1, cycleEquilibriumWorldY));
        upperTraceRenderer.SetPosition(
            1,
            GetLanePoint(-1, upperTurningWorldY));
        upperTraceRenderer.SetPosition(
            2,
            GetLanePoint(0, upperTurningWorldY));
        upperTraceRenderer.SetPosition(
            3,
            GetLanePoint(0, lowerTurningWorldY));
        upperTraceRenderer.SetPosition(
            4,
            GetLanePoint(1, lowerTurningWorldY));
        upperTraceRenderer.SetPosition(
            5,
            GetLanePoint(1, activeWorldY));
        upperTraceRenderer.enabled = true;
    }

    private void UpdateCompletedTrace(float simulationDeltaTime)
    {
        phaseTime += simulationDeltaTime;

        if (phase == TracePhase.Holding)
        {
            float hold = Mathf.Max(completedHoldDuration, 0f);

            if (phaseTime < hold)
                return;

            phaseTime -= hold;
            phase = TracePhase.Fading;
        }

        if (phase == TracePhase.Fading)
        {
            float duration = Mathf.Max(fadeDuration, 0.0001f);
            float progress = Mathf.Clamp01(phaseTime / duration);

            SetTraceAlpha(
                1f - Mathf.SmoothStep(0f, 1f, progress));

            if (phaseTime < duration)
                return;

            ClearRenderers();
            phaseTime -= duration;
            phase = TracePhase.Gap;
        }

        if (phase == TracePhase.Gap)
        {
            float gap = Mathf.Max(gapDuration, 0f);

            if (phaseTime < gap)
                return;

            phase = TracePhase.WaitingForStart;
            phaseTime = 0f;
            upperTurningWorldY = 0f;
            lowerTurningWorldY = 0f;
            SetTraceAlpha(1f);
        }
    }

    private int DetectEquilibriumCrossing(
        float previousRelative,
        float currentRelative)
    {
        if (!hasPreviousRelativePosition)
            return 0;

        if (stableEquilibriumSide == 0)
        {
            stableEquilibriumSide =
                GetEquilibriumSide(previousRelative);
        }

        int currentSide =
            GetEquilibriumSide(currentRelative);

        if (currentSide == 0)
            return 0;

        int previousSide = stableEquilibriumSide;
        stableEquilibriumSide = currentSide;

        if (previousSide == BelowEquilibrium &&
            currentSide == AboveEquilibrium)
        {
            return MovingUp;
        }

        if (previousSide == AboveEquilibrium &&
            currentSide == BelowEquilibrium)
        {
            return MovingDown;
        }

        return 0;
    }

    private int GetEquilibriumSide(float relativePosition)
    {
        float threshold = Mathf.Max(centerThreshold, 0f);

        if (relativePosition < -threshold)
            return AboveEquilibrium;

        if (relativePosition > threshold)
            return BelowEquilibrium;

        return 0;
    }

    private bool HasReturnedFromLowerHalf(
        int crossingDirection,
        float relativePosition)
    {
        return crossingDirection == MovingUp ||
               GetEquilibriumSide(relativePosition) ==
               AboveEquilibrium;
    }

    private int UpdateReliableMovementDirection()
    {
        int currentDirection =
            GetReliableMovementDirection();

        if (currentDirection == 0)
            return 0;

        if (lastReliableMovementDirection == 0)
        {
            lastReliableMovementDirection = currentDirection;
            return 0;
        }

        if (currentDirection == lastReliableMovementDirection)
            return 0;

        lastReliableMovementDirection = currentDirection;
        return currentDirection;
    }

    private int GetReliableMovementDirection()
    {
        float velocity =
            springSimulation.VelocityMetersPerSecond;
        float threshold =
            Mathf.Max(velocityThreshold, 0.0001f);

        // SpringSimulation displacement/velocity değerleri aşağı pozitif.
        if (velocity > threshold)
            return MovingDown;

        if (velocity < -threshold)
            return MovingUp;

        return 0;
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

    private float GetEquilibriumWorldY(
        Rigidbody weight,
        float relativePosition)
    {
        // worldY = restY - displacement olduğundan:
        // equilibriumY = currentWeightY + displacement - equilibrium.
        return weight.position.y + relativePosition;
    }

    private Vector3 GetLanePoint(int lane, float worldY)
    {
        float laneOffset = Mathf.Abs(laneWidth);
        float laneX = traceBaseX + lane * laneOffset;

        return new Vector3(laneX, worldY, traceZ);
    }

    private void InitializeRenderers()
    {
        upperTraceRenderer = GetComponent<LineRenderer>();

        Material traceMaterial =
            upperTraceRenderer.sharedMaterial;

        ConfigureRenderer(upperTraceRenderer, traceMaterial);

        for (int i = 0; i < FinalTraceSegmentCount; i++)
        {
            LineRenderer renderer =
                GetOrCreateFinalTraceSegmentRenderer(i);

            finalTraceSegmentRenderers[i] = renderer;
            ConfigureRenderer(renderer, traceMaterial);
            renderer.numCornerVertices = 0;
            renderer.sortingLayerID =
                upperTraceRenderer.sortingLayerID;
            renderer.sortingOrder =
                upperTraceRenderer.sortingOrder;
        }
    }

    private LineRenderer GetOrCreateFinalTraceSegmentRenderer(
        int segmentIndex)
    {
        string segmentName =
            FinalTraceSegmentNamePrefix + segmentIndex;
        Transform segmentTransform =
            transform.Find(segmentName);

        if (segmentTransform == null)
        {
            GameObject segmentObject =
                new GameObject(segmentName);
            segmentObject.hideFlags = HideFlags.DontSave;
            segmentTransform = segmentObject.transform;
            segmentTransform.SetParent(transform, false);
        }

        LineRenderer renderer =
            segmentTransform.GetComponent<LineRenderer>();

        return renderer != null
            ? renderer
            : segmentTransform.gameObject.AddComponent<LineRenderer>();
    }

    private void ConfigureRenderer(
        LineRenderer renderer,
        Material traceMaterial)
    {
        if (renderer == null)
            return;

        renderer.positionCount = 0;
        renderer.useWorldSpace = true;
        renderer.alignment = LineAlignment.View;
        renderer.startWidth = lineWidth;
        renderer.endWidth = lineWidth;
        renderer.numCornerVertices = 4;
        renderer.numCapVertices = 2;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = traceMaterial;
        renderer.enabled = false;
    }

    private void SetTraceAlpha(float normalizedAlpha)
    {
        Color color = traceColor;
        float alpha =
            traceColor.a * Mathf.Clamp01(normalizedAlpha);

        colorKeys[0] = new GradientColorKey(color, 0f);
        colorKeys[1] = new GradientColorKey(color, 1f);
        alphaKeys[0] = new GradientAlphaKey(alpha, 0f);
        alphaKeys[1] = new GradientAlphaKey(alpha, 1f);

        traceGradient.SetKeys(colorKeys, alphaKeys);

        if (upperTraceRenderer != null)
            upperTraceRenderer.colorGradient = traceGradient;

        for (int i = 0; i < finalTraceSegmentRenderers.Length; i++)
        {
            if (finalTraceSegmentRenderers[i] != null)
            {
                finalTraceSegmentRenderers[i].colorGradient =
                    traceGradient;
            }
        }
    }

    private void ClearRenderers()
    {
        ClearRenderer(upperTraceRenderer);

        for (int i = 0; i < finalTraceSegmentRenderers.Length; i++)
            ClearRenderer(finalTraceSegmentRenderers[i]);
    }

    private void ClearRenderer(LineRenderer renderer)
    {
        if (renderer == null)
            return;

        renderer.positionCount = 0;
        renderer.enabled = false;
    }

    private void ResetTraceState()
    {
        trackedWeight = null;
        phase = TracePhase.WaitingForStart;
        phaseTime = 0f;
        traceBaseX = 0f;
        traceZ = 0f;
        cycleEquilibriumWorldY = 0f;
        upperTurningWorldY = 0f;
        lowerTurningWorldY = 0f;
        previousRelativePosition = 0f;
        hasPreviousRelativePosition = false;
        stableEquilibriumSide = 0;
        lastReliableMovementDirection = 0;

        ClearRenderers();
        SetTraceAlpha(1f);
    }

    private void OnDisable()
    {
        ResetTraceState();
    }
}
