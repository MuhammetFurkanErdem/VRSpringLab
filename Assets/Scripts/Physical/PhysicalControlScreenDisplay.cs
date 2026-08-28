using UnityEngine;
using TMPro;

public class PhysicalControlScreenDisplay : MonoBehaviour
{
    public enum DisplayType
    {
        InfoPanel,
        FreeLength,
        Equilibrium,
        PeriodTrace,
        Forces,
        Acceleration,
        Velocity
    }

    [Header("Display Type")]
    [SerializeField] private DisplayType displayType;

    [Header("Text References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text valueText;

    [Header("Simulation References")]
    [SerializeField] private SpringSimulation springSimulation;
    [SerializeField] private PeriodMeasurement periodMeasurement;

    [Header("Controlled Feature Objects")]
    [Tooltip("Assign the GameObject(s) controlled by the physical lever.")]
    [SerializeField] private GameObject[] targetObjects;

    [Header("Refresh")]
    [SerializeField] private float refreshInterval = 0.1f;

    private float refreshTimer;

    private void Start()
    {
        SetTitle();
        RefreshDisplay();
    }

    private void Update()
    {
        refreshTimer += Time.deltaTime;

        if (refreshTimer < refreshInterval)
            return;

        refreshTimer = 0f;

        RefreshDisplay();
    }

    private void SetTitle()
    {
        if (titleText == null)
            return;

        titleText.text = displayType switch
        {
            DisplayType.InfoPanel => "INFO PANEL",
            DisplayType.FreeLength => "FREE LENGTH",
            DisplayType.Equilibrium => "EQUILIBRIUM",
            DisplayType.PeriodTrace => "PERIOD TRACE",
            DisplayType.Forces => "FORCES",
            DisplayType.Acceleration => "ACCELERATION",
            DisplayType.Velocity => "VELOCITY",
            _ => ""
        };
    }

    private void RefreshDisplay()
    {
        bool isActive = AreTargetObjectsActive();

        UpdateStatus(isActive);

        if (springSimulation == null)
        {
            if (valueText != null)
                valueText.text = "--";

            return;
        }

        switch (displayType)
        {
            case DisplayType.InfoPanel:
                UpdateInfoPanel();
                break;

            case DisplayType.FreeLength:
                UpdateFreeLength();
                break;

            case DisplayType.Equilibrium:
                UpdateEquilibrium();
                break;

            case DisplayType.PeriodTrace:
                UpdatePeriodTrace();
                break;

            case DisplayType.Forces:
                UpdateForces();
                break;

            case DisplayType.Acceleration:
                UpdateAcceleration();
                break;

            case DisplayType.Velocity:
                UpdateVelocity();
                break;
        }
    }

    private void UpdateStatus(bool active)
    {
        if (statusText == null)
            return;

        statusText.text = active ? "ON" : "OFF";
    }

    private bool AreTargetObjectsActive()
    {
        if (targetObjects == null || targetObjects.Length == 0)
            return false;

        foreach (GameObject target in targetObjects)
        {
            if (target == null || !target.activeSelf)
                return false;
        }

        return true;
    }

    // ------------------------------------------------
    // INFO PANEL
    // ------------------------------------------------

    private void UpdateInfoPanel()
    {
        if (valueText == null)
            return;

        if (!springSimulation.HasWeight ||
            springSimulation.CurrentWeight == null)
        {
            valueText.text =
                $"k  {springSimulation.SpringConstant:0.0} N/m\n" +
                "Mass  --";

            return;
        }

        float massGrams =
            springSimulation.CurrentWeight.mass * 1000f;

        valueText.text =
            $"k  {springSimulation.SpringConstant:0.0} N/m\n" +
            $"Mass  {massGrams:0} g";
    }

    // ------------------------------------------------
    // FREE LENGTH
    // ------------------------------------------------

    private void UpdateFreeLength()
    {
        if (valueText == null)
            return;

        valueText.text = "REFERENCE\nPOSITION";
    }

    // ------------------------------------------------
    // EQUILIBRIUM
    // ------------------------------------------------

    private void UpdateEquilibrium()
    {
        if (valueText == null)
            return;

        if (!springSimulation.HasWeight)
        {
            valueText.text = "x_eq  --";
            return;
        }

        float centimeters =
            springSimulation.EquilibriumDisplacementMeters * 100f;

        valueText.text =
            $"x_eq  {centimeters:0.0} cm";
    }

    // ------------------------------------------------
    // PERIOD
    // ------------------------------------------------

    private void UpdatePeriodTrace()
    {
        if (valueText == null)
            return;

        if (periodMeasurement == null ||
            !springSimulation.HasWeight)
        {
            valueText.text =
                "T measured  --\n" +
                "T theory    --";

            return;
        }

        float measured =
            periodMeasurement.MeasuredPeriod;

        float theoretical =
            periodMeasurement.TheoreticalPeriod;

        string measuredText =
            measured > 0f
                ? $"{measured:0.00} s"
                : "--";

        string theoreticalText =
            theoretical > 0f
                ? $"{theoretical:0.00} s"
                : "--";

        valueText.text =
            $"T meas  {measuredText}\n" +
            $"T theo  {theoreticalText}";
    }

    // ------------------------------------------------
    // FORCES
    // ------------------------------------------------

    private void UpdateForces()
    {
        if (valueText == null)
            return;

        if (!springSimulation.HasWeight)
        {
            valueText.text =
                "Fg  --\n" +
                "Fs  --";

            return;
        }

        valueText.text =
            $"Fg  {springSimulation.GravityForceNewtons:0.00} N\n" +
            $"Fs  {springSimulation.SpringForceNewtons:0.00} N";
    }

    // ------------------------------------------------
    // ACCELERATION
    // ------------------------------------------------

    private void UpdateAcceleration()
    {
        if (valueText == null)
            return;

        if (!springSimulation.HasWeight)
        {
            valueText.text = "a  --";
            return;
        }

        valueText.text =
            $"a  {springSimulation.AccelerationMetersPerSecondSquared:+0.00;-0.00;0.00} m/s²";
    }

    // ------------------------------------------------
    // VELOCITY
    // ------------------------------------------------

    private void UpdateVelocity()
    {
        if (valueText == null)
            return;

        if (!springSimulation.HasWeight)
        {
            valueText.text = "v  --";
            return;
        }

        valueText.text =
            $"v  {springSimulation.VelocityMetersPerSecond:+0.00;-0.00;0.00} m/s";
    }
}