using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class EquilibriumLine : MonoBehaviour
{
    [SerializeField] private SpringSimulation springSimulation;
    [SerializeField] private Transform springRestPoint;
    [SerializeField] private float lineWidth = 0.5f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
    }

    private void Update()
    {
        float displacement =
            springSimulation.EquilibriumDisplacementMeters;

        bool hasWeight =
            springSimulation != null &&
            springSimulation.HasWeight;

        lineRenderer.enabled = hasWeight;

        if (!hasWeight)
            return;

        Vector3 equilibriumPoint =
            springRestPoint.position +
            Vector3.down * displacement;

        Vector3 start =
            equilibriumPoint + Vector3.left * lineWidth;

        Vector3 end =
            equilibriumPoint + Vector3.right * lineWidth;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
}
