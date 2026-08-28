using UnityEngine;
using Oculus.Interaction;

public class PhysicalPlanetSelector : MonoBehaviour, ITransformer
{
    [Header("References")]
    [SerializeField] private Transform leverPivot;
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private SpringSimulation springSimulation;

    [Header("Target Local Rotations")]
    [SerializeField] private Vector3 earthRotation;
    [SerializeField] private Vector3 marsRotation;
    [SerializeField] private Vector3 moonRotation;

    [Header("Snap")]
    [SerializeField] private float snapSpeed = 180f;

    private Vector3 lockedLocalPosition;
    private Vector3 lockedLocalScale;

    private IGrabbable activeGrabbable;
    private Vector3 grabStartDirectionInParentSpace;
    private Vector3 leverStartDirectionInParentSpace;
    private bool hasValidGrabDirection;

    private bool isSnapping;
    private bool snapWhenReleased;

    private Quaternion targetRotation;
    private int selectedPresetIndex;

    private const int EarthPresetIndex = 0;
    private const int MoonPresetIndex = 1;
    private const int MarsPresetIndex = 2;

    private void Awake()
    {
        ResolveSpringSimulation();

        if (leverPivot != null)
        {
            lockedLocalPosition = leverPivot.localPosition;
            lockedLocalScale = leverPivot.localScale;
        }

        if (grabbable != null)
        {
            // Bu mekanik kontrol yalnızca tek elle sürülür. İkinci bir seçim
            // noktası, TwoGrabTransformer olmadığı için hareketi durdurmamalı.
            grabbable.MaxGrabPoints = 1;
            grabbable.InjectOptionalOneGrabTransformer(this);
        }
    }

    private void Start()
    {
        if (leverPivot == null)
            return;

        selectedPresetIndex = GetSimulationPresetIndex();
        SetImmediateRotation(GetRotationForPreset(selectedPresetIndex));
    }

    private void OnEnable()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
        }
    }

    private void OnDisable()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        }
    }

    private void LateUpdate()
    {
        if (leverPivot == null)
            return;

        // Grab sırasında joystick mafsalı yerinden kopmasın.
        leverPivot.localPosition = lockedLocalPosition;
        leverPivot.localScale = lockedLocalScale;

        bool grabActive =
            grabbable != null &&
            grabbable.GrabPoints.Count > 0;

        if (grabActive)
        {
            isSnapping = false;
            return;
        }

        if (snapWhenReleased)
        {
            snapWhenReleased = false;
            SnapToNearestPlanet();
        }

        // ResetSimulation veya başka bir fiziksel sistem gravity preset'ini
        // değiştirdiğinde joystick SpringSimulation state'ini takip eder.
        int simulationPresetIndex = GetSimulationPresetIndex();

        if (simulationPresetIndex != selectedPresetIndex)
        {
            selectedPresetIndex = simulationPresetIndex;
            StartSnap(GetRotationForPreset(selectedPresetIndex));
        }

        if (!isSnapping)
            return;

        leverPivot.localRotation =
            Quaternion.RotateTowards(
                leverPivot.localRotation,
                targetRotation,
                snapSpeed * Time.deltaTime
            );

        if (Quaternion.Angle(
                leverPivot.localRotation,
                targetRotation) < 0.2f)
        {
            leverPivot.localRotation = targetRotation;
            isSnapping = false;
        }
    }

    public void Initialize(IGrabbable initializedGrabbable)
    {
        activeGrabbable = initializedGrabbable;
    }

    public void BeginTransform()
    {
        if (leverPivot == null ||
            activeGrabbable == null ||
            activeGrabbable.GrabPoints.Count == 0)
        {
            return;
        }

        Vector3 grabOffset =
            activeGrabbable.GrabPoints[0].position -
            leverPivot.position;

        hasValidGrabDirection =
            grabOffset.sqrMagnitude > 0.000001f;

        if (!hasValidGrabDirection)
            return;

        grabStartDirectionInParentSpace =
            WorldDirectionToParentSpace(grabOffset).normalized;

        // Joystick mesh'i LeverPivot'in local +Z ekseni boyunca uzanıyor.
        // Başlangıç yönünü saklayıp controller'ın pivot çevresindeki gerçek
        // açısal hareketini buna uygularız. Böylece eğik panelde world X/Y
        // eksenlerine bağlı yapay dead-zone ve ters geçişler oluşmaz.
        leverStartDirectionInParentSpace =
            leverPivot.localRotation * Vector3.forward;
    }

    public void UpdateTransform()
    {
        if (leverPivot == null ||
            activeGrabbable == null ||
            activeGrabbable.GrabPoints.Count == 0)
        {
            return;
        }

        if (!hasValidGrabDirection)
            return;

        Vector3 currentGrabOffset =
            activeGrabbable.GrabPoints[0].position -
            leverPivot.position;

        if (currentGrabOffset.sqrMagnitude <= 0.000001f)
            return;

        Vector3 currentGrabDirectionInParentSpace =
            WorldDirectionToParentSpace(currentGrabOffset).normalized;

        Quaternion grabSwing = Quaternion.FromToRotation(
            grabStartDirectionInParentSpace,
            currentGrabDirectionInParentSpace
        );

        Vector3 desiredLeverDirection =
            (grabSwing * leverStartDirectionInParentSpace).normalized;

        float minimumLocalX = Mathf.Min(
            earthRotation.x,
            Mathf.Min(marsRotation.x, moonRotation.x));
        float maximumLocalX = Mathf.Max(
            earthRotation.x,
            Mathf.Max(marsRotation.x, moonRotation.x));
        float minimumLocalY = Mathf.Min(
            earthRotation.y,
            Mathf.Min(marsRotation.y, moonRotation.y));
        float maximumLocalY = Mathf.Max(
            earthRotation.y,
            Mathf.Max(marsRotation.y, moonRotation.y));

        float horizontalLength = Mathf.Sqrt(
            desiredLeverDirection.x * desiredLeverDirection.x +
            desiredLeverDirection.z * desiredLeverDirection.z
        );

        float localX = Mathf.Clamp(
            Mathf.Atan2(
                -desiredLeverDirection.y,
                horizontalLength
            ) * Mathf.Rad2Deg,
            minimumLocalX,
            maximumLocalX);

        float localY = Mathf.Clamp(
            Mathf.Atan2(
                desiredLeverDirection.x,
                desiredLeverDirection.z
            ) * Mathf.Rad2Deg,
            minimumLocalY,
            maximumLocalY);

        leverPivot.localPosition = lockedLocalPosition;
        leverPivot.localScale = lockedLocalScale;
        leverPivot.localRotation =
            Quaternion.Euler(localX, localY, 0f);
    }

    public void EndTransform()
    {
        hasValidGrabDirection = false;
    }

    private Vector3 WorldDirectionToParentSpace(
        Vector3 worldDirection)
    {
        return leverPivot.parent != null
            ? leverPivot.parent.InverseTransformDirection(
                worldDirection
            )
            : worldDirection;
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            isSnapping = false;
            snapWhenReleased = false;
        }
        else if (evt.Type == PointerEventType.Unselect ||
                 evt.Type == PointerEventType.Cancel)
        {
            // Birden fazla interactable aynı Grabbable'ı izleyebilir. En son
            // grab point bırakılmadan snap başlatma.
            snapWhenReleased = true;
        }
    }

    private void SnapToNearestPlanet()
    {
        Quaternion current = leverPivot.localRotation;

        Quaternion earth =
            Quaternion.Euler(earthRotation);

        Quaternion mars =
            Quaternion.Euler(marsRotation);

        Quaternion moon =
            Quaternion.Euler(moonRotation);

        float earthDistance =
            Quaternion.Angle(current, earth);

        float marsDistance =
            Quaternion.Angle(current, mars);

        float moonDistance =
            Quaternion.Angle(current, moon);

        if (earthDistance <= marsDistance &&
            earthDistance <= moonDistance)
        {
            SelectPlanet(
                earthRotation,
                EarthPresetIndex
            );
        }
        else if (marsDistance <= moonDistance)
        {
            SelectPlanet(
                marsRotation,
                MarsPresetIndex
            );
        }
        else
        {
            SelectPlanet(
                moonRotation,
                MoonPresetIndex
            );
        }
    }

    private void SelectPlanet(
        Vector3 rotation,
        int presetIndex)
    {
        selectedPresetIndex = presetIndex;
        StartSnap(rotation);

        if (springSimulation != null)
            springSimulation.SetGravityPreset(presetIndex);
    }

    private void SetImmediateRotation(Vector3 rotation)
    {
        leverPivot.localRotation =
            Quaternion.Euler(rotation);

        targetRotation =
            leverPivot.localRotation;

        isSnapping = false;
    }

    private void StartSnap(Vector3 rotation)
    {
        targetRotation = Quaternion.Euler(rotation);
        isSnapping = true;
    }

    private Vector3 GetRotationForPreset(int presetIndex)
    {
        switch (presetIndex)
        {
            case MoonPresetIndex:
                return moonRotation;

            case MarsPresetIndex:
                return marsRotation;

            default:
                return earthRotation;
        }
    }

    private int GetSimulationPresetIndex()
    {
        if (springSimulation == null)
            return EarthPresetIndex;

        float gravity = springSimulation.SelectedGravity;
        float earthDistance = Mathf.Abs(gravity - 9.81f);
        float moonDistance = Mathf.Abs(gravity - 1.62f);
        float marsDistance = Mathf.Abs(gravity - 3.71f);

        if (moonDistance <= earthDistance &&
            moonDistance <= marsDistance)
        {
            return MoonPresetIndex;
        }

        if (marsDistance <= earthDistance)
            return MarsPresetIndex;

        return EarthPresetIndex;
    }

    private void ResolveSpringSimulation()
    {
        if (springSimulation != null)
            return;

        SpringSimulation[] simulations =
            FindObjectsByType<SpringSimulation>(
                FindObjectsInactive.Include
            );

        foreach (SpringSimulation simulation in simulations)
        {
            if (simulation.gameObject.scene != gameObject.scene)
                continue;

            springSimulation = simulation;
            return;
        }
    }
}
