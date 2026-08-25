using UnityEngine;
using TMPro;
using Oculus.Interaction;

public class PhysicalPlanetSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform leverPivot;
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private TMP_Dropdown planetDropdown;

    [Header("Target Local Rotations")]
    [SerializeField] private Vector3 earthRotation;
    [SerializeField] private Vector3 marsRotation;
    [SerializeField] private Vector3 moonRotation;

    [Header("Dropdown Indices")]
    [SerializeField] private int earthDropdownIndex = 0;
    [SerializeField] private int marsDropdownIndex = 1;
    [SerializeField] private int moonDropdownIndex = 2;

    [Header("Snap")]
    [SerializeField] private float snapSpeed = 180f;

    private Vector3 lockedLocalPosition;
    private Vector3 lockedLocalScale;

    private bool isGrabbed;
    private bool isSnapping;

    private Quaternion targetRotation;

    private void Awake()
    {
        if (leverPivot != null)
        {
            lockedLocalPosition = leverPivot.localPosition;
            lockedLocalScale = leverPivot.localScale;
        }
    }

    private void Start()
    {
        // Mevcut dropdown hangi gezegendeyse
        // joystick de o konumdan başlasın.
        if (planetDropdown == null || leverPivot == null)
            return;

        if (planetDropdown.value == marsDropdownIndex)
        {
            SetImmediateRotation(marsRotation);
        }
        else if (planetDropdown.value == moonDropdownIndex)
        {
            SetImmediateRotation(moonRotation);
        }
        else
        {
            SetImmediateRotation(earthRotation);
        }
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

        // OneGrabFreeTransformer objeyi taşımaya çalışsa bile
        // joystick mafsalı yerinden kopmasın.
        leverPivot.localPosition = lockedLocalPosition;
        leverPivot.localScale = lockedLocalScale;

        if (isGrabbed || !isSnapping)
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

            SnapToNearestPlanet();
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
                earthDropdownIndex
            );
        }
        else if (marsDistance <= moonDistance)
        {
            SelectPlanet(
                marsRotation,
                marsDropdownIndex
            );
        }
        else
        {
            SelectPlanet(
                moonRotation,
                moonDropdownIndex
            );
        }
    }

    private void SelectPlanet(
        Vector3 rotation,
        int dropdownIndex)
    {
        targetRotation =
            Quaternion.Euler(rotation);

        isSnapping = true;

        if (planetDropdown != null)
        {
            planetDropdown.value = dropdownIndex;
            planetDropdown.RefreshShownValue();
        }
    }

    private void SetImmediateRotation(Vector3 rotation)
    {
        leverPivot.localRotation =
            Quaternion.Euler(rotation);

        targetRotation =
            leverPivot.localRotation;
    }
}