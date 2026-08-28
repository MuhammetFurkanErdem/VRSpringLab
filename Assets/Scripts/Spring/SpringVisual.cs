using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SpringVisual : MonoBehaviour
{
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    [SerializeField] private int turns = 12;
    [SerializeField] private int segments = 120;
    [SerializeField] private float radius = 0.08f;
    [SerializeField] private float width = 0.02f;

    [Header("End Taper")]
    [SerializeField, Range(0.5f, 0.99f)]
    private float endTaperStart = 0.88f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = segments + 1;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
    }

    private void Update()
    {
        DrawSpring();
    }

    private void DrawSpring()
    {
        if (startPoint == null || endPoint == null)
            return;

        Vector3 start = startPoint.position;
        Vector3 end = endPoint.position;

        Vector3 direction = end - start;
        float length = direction.magnitude;

        if (length <= 0.001f)
            return;

        Vector3 axis = direction.normalized;

        Vector3 reference =
            Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.99f
                ? Vector3.right
                : Vector3.up;

        Vector3 right =
            Vector3.Cross(axis, reference).normalized;

        Vector3 forward =
            Vector3.Cross(axis, right).normalized;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;

            float angle =
                t * turns * Mathf.PI * 2f;

            Vector3 center =
                Vector3.Lerp(start, end, t);

            // Son kısımda yayın yarıçapını sıfıra doğru küçült.
            float radiusMultiplier = 1f;

            if (t > endTaperStart)
            {
                radiusMultiplier =
                    Mathf.Clamp01(
                        (1f - t) /
                        (1f - endTaperStart)
                    );
            }

            float currentRadius =
                radius * radiusMultiplier;

            Vector3 offset =
                right * Mathf.Cos(angle) * currentRadius +
                forward * Mathf.Sin(angle) * currentRadius;

            lineRenderer.SetPosition(
                i,
                center + offset
            );
        }
    }
}