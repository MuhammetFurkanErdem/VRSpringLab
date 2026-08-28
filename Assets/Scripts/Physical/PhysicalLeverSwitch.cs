using UnityEngine;
using Oculus.Interaction;

public class PhysicalLeverSwitch : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpringSimulation springSimulation;
    [SerializeField] private Transform leverPivot;
    [SerializeField] private Grabbable grabbable;

    [Header("Lever Angles")]
    [SerializeField] private float onAngle = -40f;
    [SerializeField] private float offAngle = 40f;
    [SerializeField] private bool invertState = false;

    [Header("Snap")]
    [SerializeField] private float snapSpeed = 180f;

    private bool isOn;
    private bool isGrabbed;
    private bool isSnapping;

    private float targetAngle;

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
        if (!isSnapping || isGrabbed || leverPivot == null)
            return;

        float currentAngle =
            Mathf.DeltaAngle(0f, leverPivot.localEulerAngles.x);

        float newAngle =
            Mathf.MoveTowardsAngle(
                currentAngle,
                targetAngle,
                snapSpeed * Time.deltaTime
            );

        Vector3 euler = leverPivot.localEulerAngles;
        euler.x = newAngle;
        leverPivot.localEulerAngles = euler;

        if (Mathf.Abs(Mathf.DeltaAngle(newAngle, targetAngle)) < 0.2f)
        {
            SetExactAngle(targetAngle);
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

        if (evt.Type == PointerEventType.Unselect)
        {
            isGrabbed = false;
            SnapToNearestState();
        }
    }

    private void SnapToNearestState()
    {
        float currentAngle =
            Mathf.DeltaAngle(0f, leverPivot.localEulerAngles.x);

        float distanceToOn =
            Mathf.Abs(Mathf.DeltaAngle(currentAngle, onAngle));

        float distanceToOff =
            Mathf.Abs(Mathf.DeltaAngle(currentAngle, offAngle));

        bool newState = distanceToOn <= distanceToOff;

        targetAngle = newState ? onAngle : offAngle;

        SetState(newState);

        isSnapping = true;
    }

    private void SetState(bool value)
    {
        if (invertState)
            value = !value;

        isOn = value;

        if (springSimulation != null)
            springSimulation.SetGravityEnabled(isOn);

        Debug.Log($"Physical Lever: Gravity {(isOn ? "ON" : "OFF")}");
    }

    private void SetExactAngle(float angle)
    {
        Vector3 euler = leverPivot.localEulerAngles;
        euler.x = angle;
        leverPivot.localEulerAngles = euler;
    }
}