using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SpringSimulation : MonoBehaviour
{
    [SerializeField] private Transform springSocketTransform;
    [SerializeField] private XRSocketInteractor springSocket;

    [Header("Spring Physics")]
    [SerializeField] private float springConstant = 4f; // N/m
    [SerializeField] private float damping = 0.35f;

    private Vector3 restLocalPosition;
    private float displacement;
    private float velocity;

    public float DisplacementMeters => displacement;
    public float DisplacementCentimeters => displacement * 100f;
    public float SpringConstant => springConstant;
    public float VelocityMetersPerSecond => velocity;

    public float EquilibriumDisplacementMeters
    {
        get
        {
            if (!springSocket.hasSelection)
                return 0f;

            var selected = springSocket.interactablesSelected[0];
            Rigidbody rb = selected.transform.GetComponent<Rigidbody>();

            if (rb == null)
                return 0f;

            return (rb.mass * Physics.gravity.magnitude) / springConstant;
        }
    }

    private void Awake()
    {
        restLocalPosition = springSocketTransform.localPosition;
    }

    private void FixedUpdate()
    {
        if (!springSocket.hasSelection)
        {
            displacement = 0f;
            velocity = 0f;
            springSocketTransform.localPosition = restLocalPosition;
            return;
        }

        var selectedInteractable = springSocket.interactablesSelected[0];
        var selectedTransform = selectedInteractable.transform;

        Rigidbody weight = selectedTransform.GetComponent<Rigidbody>();

        if (weight == null)
            return;

        float mass = weight.mass;

        float gravityForce = mass * Physics.gravity.magnitude;
        float springForce = springConstant * displacement;
        float dampingForce = damping * velocity;

        float netForce =
            gravityForce - springForce - dampingForce;

        float acceleration = netForce / mass;

        velocity += acceleration * Time.fixedDeltaTime;
        displacement += velocity * Time.fixedDeltaTime;

        springSocketTransform.localPosition =
            restLocalPosition + Vector3.down * displacement;
    }


}