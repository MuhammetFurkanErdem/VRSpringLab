using UnityEngine;
using TMPro;

public class PhysicalMainControlScreenDisplay : MonoBehaviour
{
    public enum DisplayType
    {
        Pause,
        SpringStiffness,
        Planet,
        Reset,
        Gravity,
        SlowMotion,
        ContinuousOscillation
    }

    [Header("Display Type")]
    [SerializeField] private DisplayType displayType;

    [Header("Text References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text valueText;

    [Header("Simulation")]
    [SerializeField] private SpringSimulation springSimulation;

    [Header("Refresh")]
    [SerializeField] private float refreshInterval = 0.1f;

    private float timer;

    private void Start()
    {
        SetTitle();
        RefreshDisplay();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer < refreshInterval)
            return;

        timer = 0f;
        RefreshDisplay();
    }

    private void SetTitle()
    {
        if (titleText == null)
            return;

        titleText.text = displayType switch
        {
            DisplayType.Pause => "SIMULATION",
            DisplayType.SpringStiffness => "SPRING STIFFNESS",
            DisplayType.Planet => "PLANET",
            DisplayType.Reset => "RESET",
            DisplayType.Gravity => "GRAVITY",
            DisplayType.SlowMotion => "SIMULATION SPEED",
            DisplayType.ContinuousOscillation => "OSCILLATION",
            _ => ""
        };
    }

    private void RefreshDisplay()
    {
        if (springSimulation == null)
        {
            if (statusText != null)
                statusText.text = "--";

            if (valueText != null)
                valueText.text = "--";

            return;
        }

        switch (displayType)
        {
            case DisplayType.Pause:
                UpdatePause();
                break;

            case DisplayType.SpringStiffness:
                UpdateSpringStiffness();
                break;

            case DisplayType.Planet:
                UpdatePlanet();
                break;

            case DisplayType.Reset:
                UpdateReset();
                break;

            case DisplayType.Gravity:
                UpdateGravity();
                break;

            case DisplayType.SlowMotion:
                UpdateSlowMotion();
                break;

            case DisplayType.ContinuousOscillation:
                UpdateContinuousOscillation();
                break;
        }
    }

    // ------------------------------------------------
    // PAUSE
    // ------------------------------------------------

    private void UpdatePause()
    {
        bool paused = springSimulation.IsPaused;

        if (statusText != null)
            statusText.text = paused ? "PAUSED" : "RUNNING";

        if (valueText != null)
            valueText.text = paused ? "PRESS START" : "SYSTEM ACTIVE";
    }

    // ------------------------------------------------
    // SPRING STIFFNESS
    // ------------------------------------------------

    private void UpdateSpringStiffness()
    {
        if (statusText != null)
            statusText.text = "ACTIVE";

        if (valueText != null)
        {
            valueText.text =
                $"k = {springSimulation.SpringConstant:0.0} N/m";
        }
    }

    // ------------------------------------------------
    // PLANET
    // ------------------------------------------------

    private void UpdatePlanet()
    {
        float gravity = springSimulation.SelectedGravity;

        string planet = GetPlanetName(gravity);

        if (statusText != null)
            statusText.text = planet;

        if (valueText != null)
        {
            valueText.text =
                $"g = {gravity:0.00} m/s²";
        }
    }

    // ------------------------------------------------
    // RESET
    // ------------------------------------------------

    private void UpdateReset()
    {
        if (statusText != null)
            statusText.text = "READY";

        if (valueText != null)
            valueText.text = "PULL TO RESET";
    }

    // ------------------------------------------------
    // GRAVITY
    // ------------------------------------------------

    private void UpdateGravity()
    {
        bool enabled = springSimulation.GravityEnabled;

        if (statusText != null)
            statusText.text = enabled ? "ON" : "OFF";

        if (valueText != null)
        {
            valueText.text = enabled
                ? $"g = {springSimulation.EffectiveGravity:0.00} m/s²"
                : "g = 0.00 m/s²";
        }
    }

    // ------------------------------------------------
    // SLOW MOTION
    // ------------------------------------------------

    private void UpdateSlowMotion()
    {
        float speed = springSimulation.SimulationSpeed;

        bool slow = speed < 1f;

        if (statusText != null)
            statusText.text = slow ? "SLOW" : "NORMAL";

        if (valueText != null)
            valueText.text = $"{speed:0.00}x";
    }

    // ------------------------------------------------
    // CONTINUOUS OSCILLATION
    // ------------------------------------------------

    private void UpdateContinuousOscillation()
    {
        bool continuous =
            springSimulation.ContinuousOscillation;

        if (statusText != null)
        {
            statusText.text =
                continuous
                    ? "CONTINUOUS"
                    : "DAMPED";
        }

        if (valueText != null)
        {
            valueText.text =
                continuous
                    ? "DAMPING OFF"
                    : "DAMPING ON";
        }
    }

    // ------------------------------------------------
    // PLANET NAME
    // ------------------------------------------------

    private string GetPlanetName(float gravity)
    {
        if (Mathf.Abs(gravity - 1.62f) < 0.1f)
            return "MOON";

        if (Mathf.Abs(gravity - 3.71f) < 0.1f)
            return "MARS";

        return "EARTH";
    }
}