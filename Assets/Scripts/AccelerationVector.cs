using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AccelerationVector : MonoBehaviour
{
    [SerializeField] private SpringSimulation springSimulation;
    [SerializeField] private Transform springEndPoint;

    [Header("Vector")]
    [SerializeField] private float visualScale = 0.04f;
    [SerializeField] private float maxLength = 0.5f;
    [SerializeField] private float width = 0.015f;

    [Header("Arrow Head")]
    [SerializeField] private float headLength = 0.06f;
    [SerializeField] private float headWidth = 0.035f;

    private LineRenderer shaftRenderer;
    private LineRenderer headRenderer;

    private void Awake()
    {
        shaftRenderer = GetComponent<LineRenderer>();

        shaftRenderer.positionCount = 2;
        shaftRenderer.startWidth = width;
        shaftRenderer.endWidth = width;
        shaftRenderer.useWorldSpace = true;

        GameObject headObject = new GameObject("ArrowHead");
        headObject.transform.SetParent(transform);

        headRenderer = headObject.AddComponent<LineRenderer>();

        headRenderer.positionCount = 3;
        headRenderer.startWidth = width;
        headRenderer.endWidth = width;
        headRenderer.useWorldSpace = true;

        headRenderer.material = shaftRenderer.material;
    }

    private void Update()
    {
        float acceleration =
            springSimulation.AccelerationMetersPerSecondSquared;

        if (Mathf.Abs(acceleration) < 0.05f)
        {
            shaftRenderer.enabled = false;
            headRenderer.enabled = false;
            return;
        }

        shaftRenderer.enabled = true;
        headRenderer.enabled = true;

        Vector3 start =
            springEndPoint.position +
            Vector3.left * 0.18f;

        float length =
            Mathf.Min(
                Mathf.Abs(acceleration) * visualScale,
                maxLength
            );

        Vector3 direction =
            acceleration > 0f
                ? Vector3.down
                : Vector3.up;

        Vector3 tip =
            start + direction * length;

        shaftRenderer.SetPosition(0, start);
        shaftRenderer.SetPosition(1, tip);

        Vector3 headBase =
            tip - direction * headLength;

        Vector3 left =
            headBase + Vector3.left * headWidth;

        Vector3 right =
            headBase + Vector3.right * headWidth;

        headRenderer.SetPosition(0, left);
        headRenderer.SetPosition(1, tip);
        headRenderer.SetPosition(2, right);
    }
}