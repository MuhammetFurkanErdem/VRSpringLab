using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CurrentPositionLine : MonoBehaviour
{
    [SerializeField]
    private SpringSimulation springSimulation;

    [Header("Line Position")]
    [SerializeField]
    private float lineLength = 0.45f;

    [SerializeField]
    private float verticalOffset = 0f;

    [SerializeField]
    private float depthOffset = 0f;

    [Header("Line Appearance")]
    [SerializeField]
    private float lineWidth = 0.012f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
    }

    private void Update()
    {
        if (springSimulation == null ||
            !springSimulation.HasWeight ||
            springSimulation.CurrentWeight == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;

        Vector3 weightPosition =
            springSimulation.CurrentWeight.position;

        Vector3 center =
            weightPosition
            + Vector3.up * verticalOffset
            + Vector3.forward * depthOffset;

        float halfLength =
            lineLength * 0.5f;

        Vector3 start =
            center + Vector3.left * halfLength;

        Vector3 end =
            center + Vector3.right * halfLength;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
}