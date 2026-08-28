using UnityEngine;
using Oculus.Interaction;

public class PhysicalContinuousOscillationLever : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform leverPivot;
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private SpringSimulation springSimulation;

    [Header("Angles")]
    [SerializeField] private float normalAngle = -40f;
    [SerializeField] private float continuousAngle = 40f;

    [Header("Snap")]
    [SerializeField] private float snapSpeed = 120f;

    private bool isGrabbed;
    private bool isSnapping;
    private bool isContinuous;

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

        isContinuous =
            springSimulation.ContinuousOscillation;

        SetImmediateRotation(isContinuous);
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

        // Reset continuous mode'u kapatırsa
        // fiziksel kol da NORMAL konumuna dönsün.
        if (!isGrabbed)
        {
            bool simulationContinuous =
                springSimulation.ContinuousOscillation;

            if (simulationContinuous != isContinuous)
            {
                isContinuous = simulationContinuous;
                StartSnap(isContinuous);
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

        float continuousDistance =
            Mathf.Abs(
                Mathf.DeltaAngle(currentX, continuousAngle)
            );

        bool continuous =
            continuousDistance < normalDistance;

        isContinuous = continuous;

        springSimulation.SetContinuousOscillation(
            continuous
        );

        StartSnap(continuous);
    }

    private void StartSnap(bool continuous)
    {
        float angle =
            continuous
                ? continuousAngle
                : normalAngle;

        targetRotation =
            Quaternion.Euler(
                angle,
                lockedY,
                lockedZ
            );

        isSnapping = true;
    }

    private void SetImmediateRotation(bool continuous)
    {
        float angle =
            continuous
                ? continuousAngle
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