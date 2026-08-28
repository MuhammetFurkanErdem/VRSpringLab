using UnityEngine;
using Oculus.Interaction;

public class PhysicalFeatureLever : MonoBehaviour
{
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    [Header("References")]
    [SerializeField] private Transform leverPivot;
    [SerializeField] private Grabbable grabbable;

    [Header("Controlled Objects")]
    [SerializeField] private GameObject[] targetObjects;

    [Header("Lever Rotation")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.X;

    [SerializeField] private float offAngle = -40f;
    [SerializeField] private float onAngle = 40f;

    [Header("Snap")]
    [SerializeField] private float snapSpeed = 120f;

    private bool isGrabbed;
    private bool isSnapping;
    private bool isOn;

    private Quaternion targetRotation;

    private void Start()
    {
        if (leverPivot == null)
            return;

        // Sahnedeki mevcut durumu başlangıç state'i olarak kullan.
        if (targetObjects != null &&
            targetObjects.Length > 0 &&
            targetObjects[0] != null)
        {
            isOn = targetObjects[0].activeSelf;
        }

        SetImmediateState(isOn);
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
        if (!isSnapping || isGrabbed || leverPivot == null)
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

            DetermineState();
        }
    }

    private void DetermineState()
    {
        float currentAngle = GetCurrentSignedAngle();

        float distanceToOff =
            Mathf.Abs(Mathf.DeltaAngle(currentAngle, offAngle));

        float distanceToOn =
            Mathf.Abs(Mathf.DeltaAngle(currentAngle, onAngle));

        SetState(distanceToOn < distanceToOff);
    }

    private void SetState(bool newState)
    {
        isOn = newState;

        if (targetObjects != null)
        {
            foreach (GameObject target in targetObjects)
            {
                if (target != null)
                    target.SetActive(isOn);
            }
        }

        targetRotation =
            CreateRotation(isOn ? onAngle : offAngle);

        isSnapping = true;
    }

    private void SetImmediateState(bool state)
    {
        isOn = state;

        targetRotation =
            CreateRotation(isOn ? onAngle : offAngle);

        leverPivot.localRotation = targetRotation;
        isSnapping = false;
    }

    private float GetCurrentSignedAngle()
    {
        Vector3 euler = leverPivot.localEulerAngles;

        return rotationAxis switch
        {
            RotationAxis.X => Mathf.DeltaAngle(0f, euler.x),
            RotationAxis.Y => Mathf.DeltaAngle(0f, euler.y),
            RotationAxis.Z => Mathf.DeltaAngle(0f, euler.z),
            _ => 0f
        };
    }

    private Quaternion CreateRotation(float angle)
    {
        return rotationAxis switch
        {
            RotationAxis.X => Quaternion.Euler(angle, 0f, 0f),
            RotationAxis.Y => Quaternion.Euler(0f, angle, 0f),
            RotationAxis.Z => Quaternion.Euler(0f, 0f, angle),
            _ => Quaternion.identity
        };
    }
}