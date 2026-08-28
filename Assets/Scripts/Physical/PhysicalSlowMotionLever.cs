using UnityEngine;
using Oculus.Interaction;

public class PhysicalSlowMotionLever : MonoBehaviour
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
    private bool isSlowMotion;

    private Quaternion targetRotation;

    private float lockedY;
    private float lockedZ;

    private void Awake()
    {
        if (leverPivot == null)
            return;

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
        }
        else if (evt.Type == PointerEventType.Unselect)
        {
            isGrabbed = false;
            SelectNearestState();
        }
    }

    private void SelectNearestState()
    {
        float currentX =
            Mathf.DeltaAngle(
                0f,
                leverPivot.localEulerAngles.x
            );

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