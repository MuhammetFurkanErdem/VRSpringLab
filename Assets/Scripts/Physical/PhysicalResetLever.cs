using UnityEngine;
using Oculus.Interaction;

public class PhysicalResetLever : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform leverPivot;
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private SpringSimulation springSimulation;

    [Header("Reset")]
    [Tooltip("Kol bu açıdan daha fazla çekildiyse reset tetiklenir.")]
    [SerializeField] private float triggerAngle = -70f;

    [Header("Return")]
    [SerializeField] private float restAngle = 0f;
    [SerializeField] private float returnSpeed = 180f;
    [SerializeField] private PhysicalSpringStiffnessFader stiffnessFader;

    private bool isGrabbed;
    private bool returning;

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
        if (!returning || isGrabbed || leverPivot == null)
            return;

        Vector3 euler = leverPivot.localEulerAngles;

        float currentX =
            Mathf.DeltaAngle(0f, euler.x);

        float newX =
            Mathf.MoveTowardsAngle(
                currentX,
                restAngle,
                returnSpeed * Time.deltaTime
            );

        leverPivot.localRotation =
            Quaternion.Euler(
                newX,
                0f,
                0f
            );

        if (Mathf.Abs(
                Mathf.DeltaAngle(newX, restAngle)) < 0.2f)
        {
            leverPivot.localRotation =
                Quaternion.Euler(restAngle, 0f, 0f);

            returning = false;
        }
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            isGrabbed = true;
            returning = false;
        }

        else if (evt.Type == PointerEventType.Unselect)
        {
            isGrabbed = false;

            CheckReset();
            returning = true;
        }
    }

    private void CheckReset()
    {
        if (leverPivot == null)
            return;

        float currentAngle =
            Mathf.DeltaAngle(
                0f,
                leverPivot.localEulerAngles.x
            );

        if (currentAngle <= triggerAngle)
        {
            if (springSimulation != null)
                springSimulation.ResetSimulation();

            if (stiffnessFader != null)
                stiffnessFader.ResetToDefault();

            Debug.Log("Physical Reset Lever: Reset triggered.");
        }
    }
}