using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class FreeLengthLine : MonoBehaviour
{
    [SerializeField] private Transform springAnchor;
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
        Vector3 start = springRestPoint.position + Vector3.left * lineWidth;
        Vector3 end = springRestPoint.position + Vector3.right * lineWidth;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
}