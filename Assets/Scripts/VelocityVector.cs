using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class VelocityVector : MonoBehaviour
{
    [SerializeField] private SpringSimulation springSimulation;
    [SerializeField] private Transform springEndPoint;

    [SerializeField] private float visualScale = 0.4f;
    [SerializeField] private float maxLength = 0.5f;
    [SerializeField] private float width = 0.015f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.useWorldSpace = true;
    }

    private void Update()
    {
        float velocity = springSimulation.VelocityMetersPerSecond;

        if (Mathf.Abs(velocity) < 0.01f)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;

        Vector3 start =
            springEndPoint.position + Vector3.right * 0.18f;

        float length =
            Mathf.Min(Mathf.Abs(velocity) * visualScale, maxLength);

        Vector3 direction =
            velocity > 0f ? Vector3.down : Vector3.up;

        Vector3 end =
            start + direction * length;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
}