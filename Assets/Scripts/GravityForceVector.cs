using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class GravityForceVector : MonoBehaviour
{
    [SerializeField] private SpringSimulation springSimulation;

    [Header("Vector")]
    [SerializeField] private float visualScale = 0.12f;
    [SerializeField] private float maxLength = 0.5f;
    [SerializeField] private float width = 0.015f;
    [SerializeField] private float horizontalOffset = 0.18f;

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
        if (springSimulation == null ||
            !springSimulation.HasWeight ||
            springSimulation.CurrentWeight == null)
        {
            SetRenderersVisible(false);
            return;
        }

        float force = springSimulation.GravityForceNewtons;

        if (force < 0.01f)
        {
            SetRenderersVisible(false);
            return;
        }

        SetRenderersVisible(true);

        Vector3 start =
            springSimulation.CurrentWeight.transform.position +
            Vector3.right * horizontalOffset;

        float length =
            Mathf.Min(force * visualScale, maxLength);

        Vector3 direction = Vector3.down;

        Vector3 tip =
            start + direction * length;

        shaftRenderer.SetPosition(0, start);
        shaftRenderer.SetPosition(1, tip);

        DrawArrowHead(tip, direction);
    }

    private void DrawArrowHead(Vector3 tip, Vector3 direction)
    {
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

    private void SetRenderersVisible(bool visible)
    {
        shaftRenderer.enabled = visible;
        headRenderer.enabled = visible;
    }
}