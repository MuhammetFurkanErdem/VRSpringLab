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

    private Vector3 restLocalPosition;

    private float displacement;
    private float velocity;
    private float acceleration;
    private float gravityForce;
    private float springForce;
    private float dampingForce;
    private float netForce;

    private Rigidbody currentWeight;

    // -------------------------
    // Public physics values
    // -------------------------

    public float DisplacementMeters => displacement;

    public float DisplacementCentimeters => displacement * 100f;

    public float SpringConstant => springConstant;

    public float VelocityMetersPerSecond => velocity;

    public float AccelerationMetersPerSecondSquared => acceleration;

    public bool HasWeight => currentWeight != null;

    public Rigidbody CurrentWeight => currentWeight;

    public float GravityForceNewtons => gravityForce;
    public float SpringForceNewtons => springForce;
    public float DampingForceNewtons => dampingForce;
    public float NetForceNewtons => netForce;

    public void SetSpringConstant(float value)
    {
        springConstant = Mathf.Max(0.01f, value);
    }

    public float EquilibriumDisplacementMeters
    {
        get
        {
            if (currentWeight == null)
                return 0f;

            return
                (currentWeight.mass * Physics.gravity.magnitude)
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

    // -------------------------
    // Yay fiziği
    // -------------------------

    private void SimulateSpring()
    {
        float mass =
            currentWeight.mass;

        gravityForce =
            mass * Physics.gravity.magnitude;

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

        velocity +=
            acceleration * Time.fixedDeltaTime;

        displacement +=
            velocity * Time.fixedDeltaTime;

        springSocketTransform.localPosition =
            restLocalPosition
            + Vector3.down * displacement;
    }

    // -------------------------
    // Başlangıç durumu
    // -------------------------

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
}