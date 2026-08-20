using UnityEngine;
using Oculus.Interaction;

public class SpringSimulation : MonoBehaviour
{
    [Header("Spring")]
    [SerializeField] private Transform springSocketTransform;

    [Header("Meta Snap")]
    [SerializeField] private SnapInteractable springSnapZone;

    [SerializeField]
    private SnapInteractor[] weightSnapInteractors;

    [Header("Spring Physics")]
    [SerializeField] private float springConstant = 4f; // N/m
    [SerializeField] private float damping = 0.35f;
    [SerializeField] private bool gravityEnabled = true;
    [SerializeField] private float selectedGravity = 9.81f;

    [Header("Simulation Time")]
    [SerializeField] private bool isPaused = false;

    [SerializeField]
    private float simulationSpeed = 1f;

    private Vector3 restLocalPosition;

    private float displacement;
    private float velocity;
    private float acceleration;

    private float gravityForce;
    private float springForce;
    private float dampingForce;
    private float netForce;

    private Rigidbody currentWeight;

    // ------------------------------------------------
    // Public physics values
    // ------------------------------------------------

    public Vector3 SpringSocketWorldPosition =>
    springSocketTransform != null
        ? springSocketTransform.position
        : transform.position;

    public float DisplacementMeters =>
        displacement;

    public float DisplacementCentimeters =>
        displacement * 100f;

    public float SpringConstant =>
        springConstant;

    public float VelocityMetersPerSecond =>
        velocity;

    public float AccelerationMetersPerSecondSquared =>
        acceleration;

    public bool HasWeight =>
        currentWeight != null;

    public Rigidbody CurrentWeight =>
        currentWeight;

    public float GravityForceNewtons =>
        gravityForce;

    public float SpringForceNewtons =>
        springForce;

    public float DampingForceNewtons =>
        dampingForce;

    public float NetForceNewtons =>
        netForce;

    public bool GravityEnabled =>
        gravityEnabled;

    public float SelectedGravity =>
        selectedGravity;

    public float EffectiveGravity =>
        gravityEnabled ? selectedGravity : 0f;

    // ------------------------------------------------
    // Simulation time
    // ------------------------------------------------

    public bool IsPaused =>
        isPaused;

    public float SimulationSpeed =>
        simulationSpeed;

    public float SimulationDeltaTime =>
        isPaused
            ? 0f
            : Time.fixedDeltaTime * simulationSpeed;

    // ------------------------------------------------
    // Spring settings
    // ------------------------------------------------

    public void SetSpringConstant(float value)
    {
        springConstant =
            Mathf.Max(0.01f, value);
    }

    public void SetGravityEnabled(bool enabled)
    {
        gravityEnabled = enabled;

        if (!gravityEnabled)
            gravityForce = 0f;
    }

    public void SetGravityPreset(int index)
    {
        switch (index)
        {
            case 1:
                selectedGravity = 1.62f;
                break;

            case 2:
                selectedGravity = 3.71f;
                break;

            default:
                selectedGravity = 9.81f;
                break;
        }
    }

    // ------------------------------------------------
    // Pause / Resume
    // ------------------------------------------------

    public void SetPaused(bool paused)
    {
        isPaused = paused;
    }

    // ------------------------------------------------
    // Slow motion
    // ------------------------------------------------

    public void SetSlowMotion(bool slow)
    {
        simulationSpeed =
            slow ? 0.25f : 1f;
    }

    // ------------------------------------------------
    // Equilibrium
    // ------------------------------------------------

    public float EquilibriumDisplacementMeters
    {
        get
        {
            if (currentWeight == null)
                return 0f;

            return
                (currentWeight.mass *
                 EffectiveGravity)
                / springConstant;
        }
    }

    private void Awake()
    {
        restLocalPosition =
            springSocketTransform.localPosition;
    }

    private void FixedUpdate()
    {
        FindSnappedWeight();

        if (currentWeight == null)
        {
            ResetSpring();
            return;
        }

        // Ağırlık bağlı kalır fakat yay fiziği durur.
        if (isPaused)
            return;

        SimulateSpring();
    }

    // ------------------------------------------------
    // Hangi ağırlığın Meta SnapZone'a bağlı olduğunu bul
    // ------------------------------------------------

    private void FindSnappedWeight()
    {
        currentWeight = null;

        foreach (SnapInteractor snapInteractor
                 in weightSnapInteractors)
        {
            if (snapInteractor == null)
                continue;

            if (!snapInteractor.HasSelectedInteractable)
                continue;

            if (snapInteractor.SelectedInteractable
                != springSnapZone)
                continue;

            Rigidbody rb =
                snapInteractor.GetComponent<Rigidbody>();

            if (rb == null)
                continue;

            currentWeight = rb;
            return;
        }
    }

    // ------------------------------------------------
    // Yay fiziği
    // ------------------------------------------------

    private void SimulateSpring()
    {
        float mass =
            currentWeight.mass;

        gravityForce =
            mass * EffectiveGravity;

        springForce =
            springConstant * displacement;

        dampingForce =
            damping * velocity;

        netForce =
            gravityForce
            - springForce
            - dampingForce;

        acceleration =
            netForce / mass;

        // Normal modda Time.fixedDeltaTime,
        // yavaş modda bunun %25'i kullanılır.
        float dt =
            SimulationDeltaTime;

        velocity +=
            acceleration * dt;

        displacement +=
            velocity * dt;

        springSocketTransform.localPosition =
            restLocalPosition
            + Vector3.down * displacement;
    }

    // ------------------------------------------------
    // Başlangıç durumu
    // ------------------------------------------------

    private void ResetSpring()
    {
        displacement = 0f;
        velocity = 0f;
        acceleration = 0f;

        gravityForce = 0f;
        springForce = 0f;
        dampingForce = 0f;
        netForce = 0f;

        springSocketTransform.localPosition =
            restLocalPosition;
    }

    public void ResetSimulation()
    {
        springConstant = 4f;

        isPaused = false;
        simulationSpeed = 1f;
        gravityEnabled = true;
        selectedGravity = 9.81f;

        ResetSpring();
    }
}
