using UnityEngine;
using Oculus.Interaction;

public class PhysicalSlowMotionLever : MonoBehaviour, ITransformer
{
    [Header("References")]
    [SerializeField] private Transform leverPivot;
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private SpringSimulation springSimulation;

    [Header("Angles")]
    [SerializeField] private float normalAngle = -40f;
    [SerializeField] private float slowMotionAngle = 40f;

    [Header("Snap")]
    [SerializeField] private float snapSpeed = 120f;

    private bool isGrabbed;
    private bool isSnapping;
    private bool snapWhenReleased;
    private bool isSlowMotion;

    private Quaternion targetRotation;

    private float lockedY;
    private float lockedZ;
    private Vector3 lockedLocalPosition;
    private Vector3 lockedLocalScale;

    private IGrabbable activeGrabbable;
    private Vector3 grabStartVector;
    private Vector3 grabRotationAxis;
    private float grabStartAngle;
    private bool hasValidGrabVector;

    private void Awake()
    {
        if (grabbable != null)
        {
            grabbable.MaxGrabPoints = 1;
            grabbable.InjectOptionalOneGrabTransformer(this);
        }

        if (leverPivot == null)
            return;

        lockedLocalPosition = leverPivot.localPosition;
        lockedLocalScale = leverPivot.localScale;

        Vector3 euler = leverPivot.localEulerAngles;

        lockedY = euler.y;
        lockedZ = euler.z;
    }

    private void Start()
    {
        if (leverPivot == null || springSimulation == null)
            return;

        isSlowMotion = springSimulation.SimulationSpeed < 1f;

        SetImmediateRotation(isSlowMotion);
    }

    private void OnEnable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
    }

    private void OnDisable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
    }

    private void Update()
    {
        if (leverPivot == null || springSimulation == null)
            return;

        isGrabbed =
            grabbable != null &&
            grabbable.GrabPoints.Count > 0;

        if (isGrabbed)
        {
            isSnapping = false;
            return;
        }

        if (snapWhenReleased)
        {
            snapWhenReleased = false;
            SelectNearestState();
        }

        // Reset veya başka bir sistem slow motion state'ini
        // değiştirdiyse fiziksel kol da onu takip etsin.
        if (!isGrabbed)
        {
            bool simulationSlow =
                springSimulation.SimulationSpeed < 1f;

            if (simulationSlow != isSlowMotion)
            {
                isSlowMotion = simulationSlow;
                StartSnap(isSlowMotion);
            }
        }

        if (!isSnapping || isGrabbed)
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

    private void HandlePointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            isGrabbed = true;
            isSnapping = false;
            snapWhenReleased = false;
        }
        else if (evt.Type == PointerEventType.Unselect ||
                 evt.Type == PointerEventType.Cancel)
        {
            snapWhenReleased = true;
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

        grabRotationAxis =
            leverPivot.TransformDirection(Vector3.right).normalized;

        Vector3 grabOffset =
            activeGrabbable.GrabPoints[0].position -
            leverPivot.position;

        grabStartVector = Vector3.ProjectOnPlane(
            grabOffset,
            grabRotationAxis
        );

        hasValidGrabVector =
            grabStartVector.sqrMagnitude > 0.000001f;

        grabStartAngle = GetCurrentLocalX();
    }

    public void UpdateTransform()
    {
        if (!hasValidGrabVector ||
            leverPivot == null ||
            activeGrabbable == null ||
            activeGrabbable.GrabPoints.Count == 0)
        {
            return;
        }

        Vector3 currentOffset =
            activeGrabbable.GrabPoints[0].position -
            leverPivot.position;

        Vector3 currentVector = Vector3.ProjectOnPlane(
            currentOffset,
            grabRotationAxis
        );

        if (currentVector.sqrMagnitude <= 0.000001f)
            return;

        float angleDelta = Vector3.SignedAngle(
            grabStartVector,
            currentVector,
            grabRotationAxis
        );

        float minimumAngle = Mathf.Min(
            normalAngle,
            slowMotionAngle
        );
        float maximumAngle = Mathf.Max(
            normalAngle,
            slowMotionAngle
        );

        float localX = Mathf.Clamp(
            grabStartAngle + angleDelta,
            minimumAngle,
            maximumAngle
        );

        leverPivot.localPosition = lockedLocalPosition;
        leverPivot.localScale = lockedLocalScale;
        leverPivot.localRotation = Quaternion.Euler(
            localX,
            lockedY,
            lockedZ
        );
    }

    public void EndTransform()
    {
        hasValidGrabVector = false;
    }

    private float GetCurrentLocalX()
    {
        return Mathf.DeltaAngle(
            0f,
            leverPivot.localEulerAngles.x
        );
    }

    private void SelectNearestState()
    {
        float currentX = GetCurrentLocalX();

        float normalDistance =
            Mathf.Abs(
                Mathf.DeltaAngle(currentX, normalAngle)
            );

        float slowDistance =
            Mathf.Abs(
                Mathf.DeltaAngle(currentX, slowMotionAngle)
            );

        bool slow =
            slowDistance < normalDistance;

        isSlowMotion = slow;

        springSimulation.SetSlowMotion(slow);

        StartSnap(slow);
    }

    private void StartSnap(bool slow)
    {
        float angle =
            slow
                ? slowMotionAngle
                : normalAngle;

        targetRotation =
            Quaternion.Euler(
                angle,
                lockedY,
                lockedZ
            );

        isSnapping = true;
    }

    private void SetImmediateRotation(bool slow)
    {
        float angle =
            slow
                ? slowMotionAngle
                : normalAngle;

        targetRotation =
            Quaternion.Euler(
                angle,
                lockedY,
                lockedZ
            );

        leverPivot.localRotation = targetRotation;
        isSnapping = false;
    }
}
