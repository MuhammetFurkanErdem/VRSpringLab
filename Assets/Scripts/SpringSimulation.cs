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
    private float acceleration;

    // Public physics values
    public float DisplacementMeters => displacement;
    public float DisplacementCentimeters => displacement * 100f;

    public float SpringConstant => springConstant;

    public float VelocityMetersPerSecond => velocity;

    public float AccelerationMetersPerSecondSquared => acceleration;

    public float EquilibriumDisplacementMeters
    {
        get
        {
            if (!springSocket.hasSelection)
                return 0f;

            var selected = springSocket.interactablesSelected[0];

            Rigidbody rb =
                selected.transform.GetComponent<Rigidbody>();

            if (rb == null)
                return 0f;

            return
                (rb.mass * Physics.gravity.magnitude)
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
        // Yayda ağırlık yoksa sistemi başlangıç konumuna getir
        if (!springSocket.hasSelection)
        {
            displacement = 0f;
            velocity = 0f;
            acceleration = 0f;

            springSocketTransform.localPosition =
                restLocalPosition;

            return;
        }

        // Socket'a takılı nesneyi al
        var selectedInteractable =
            springSocket.interactablesSelected[0];

        Transform selectedTransform =
            selectedInteractable.transform;

        Rigidbody weight =
            selectedTransform.GetComponent<Rigidbody>();

        if (weight == null)
        {
            acceleration = 0f;
            return;
        }

        float mass = weight.mass;

        // Kuvvetler
        float gravityForce =
            mass * Physics.gravity.magnitude;

        float springForce =
            springConstant * displacement;

        float dampingForce =
            damping * velocity;

        // Net kuvvet
        float netForce =
            gravityForce
            - springForce
            - dampingForce;

        // F = ma  →  a = F / m
        acceleration =
            netForce / mass;

        // Hız
        velocity +=
            acceleration * Time.fixedDeltaTime;

        // Konum / uzama
        displacement +=
            velocity * Time.fixedDeltaTime;

        // Socket'ı aşağı doğru hareket ettir
        springSocketTransform.localPosition =
            restLocalPosition
            + Vector3.down * displacement;
    }
}