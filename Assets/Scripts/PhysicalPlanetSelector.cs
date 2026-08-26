using UnityEngine;
using TMPro;
using Oculus.Interaction;

public class PhysicalPlanetSelector : MonoBehaviour, ITransformer
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

    [Header("Grab Rotation")]
    [Tooltip("Controller'in yatay world hareketini local Y acisina cevirir.")]
    [SerializeField] private float horizontalDegreesPerMeter = 300f;

    [Tooltip("Controller'in dikey world hareketini local X acisina cevirir.")]
    [SerializeField] private float verticalDegreesPerMeter = 500f;

    private Vector3 lockedLocalPosition;
    private Vector3 lockedLocalScale;

    private IGrabbable activeGrabbable;
    private Vector3 grabStartWorldPosition;
    private float grabStartLocalX;
    private float grabStartLocalY;

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

        if (grabbable != null)
            grabbable.InjectOptionalOneGrabTransformer(this);
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

        // Grab sırasında joystick mafsalı yerinden kopmasın.
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

        grabStartWorldPosition =
            activeGrabbable.GrabPoints[0].position;

        Vector3 currentEuler =
            leverPivot.localRotation.eulerAngles;

        grabStartLocalX =
            Mathf.DeltaAngle(0f, currentEuler.x);
        grabStartLocalY =
            Mathf.DeltaAngle(0f, currentEuler.y);
    }

    public void UpdateTransform()
    {
        if (leverPivot == null ||
            activeGrabbable == null ||
            activeGrabbable.GrabPoints.Count == 0)
        {
            return;
        }

        Vector3 worldDelta =
            activeGrabbable.GrabPoints[0].position -
            grabStartWorldPosition;

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

        float localX = Mathf.Clamp(
            grabStartLocalX +
            -worldDelta.y * verticalDegreesPerMeter,
            minimumLocalX,
            maximumLocalX);

        float localY = Mathf.Clamp(
            grabStartLocalY +
            -worldDelta.x * horizontalDegreesPerMeter,
            minimumLocalY,
            maximumLocalY);

        leverPivot.localPosition = lockedLocalPosition;
        leverPivot.localScale = lockedLocalScale;
        leverPivot.localRotation =
            Quaternion.Euler(localX, localY, 0f);
    }

    public void EndTransform()
    {
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
