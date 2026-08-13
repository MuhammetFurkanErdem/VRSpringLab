using TMPro;
using UnityEngine;

public class RulerTicks : MonoBehaviour
{
    [SerializeField] private float lengthMeters = 1f;
    [SerializeField] private int stepCm = 10;

    [SerializeField] private float tickWidth = 0.12f;
    [SerializeField] private float tickHeight = 0.008f;
    [SerializeField] private float labelScale = 0.003f;

    private void Start()
    {
        CreateTicks();
    }

    private void CreateTicks()
    {
        int maxCm = Mathf.RoundToInt(lengthMeters * 100f);

        for (int cm = 0; cm <= maxCm; cm += stepCm)
        {
            float y = -(cm / 100f);

            CreateTick(cm, y);
        }
    }

    private void CreateTick(int cm, float y)
    {
        GameObject tick = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tick.name = $"Tick_{cm}cm";
        tick.transform.SetParent(transform);

        tick.transform.localPosition =
            new Vector3(-tickWidth * 0.5f, y, 0f);

        tick.transform.localScale =
            new Vector3(tickWidth, tickHeight, 0.01f);

        Destroy(tick.GetComponent<Collider>());

        GameObject labelObject = new GameObject($"Label_{cm}cm");
        labelObject.transform.SetParent(transform);

        labelObject.transform.localPosition =
            new Vector3(-0.16f, y, 0f);

        labelObject.transform.localRotation =
            Quaternion.Euler(0f, 0f, 0f);

        labelObject.transform.localScale =
            Vector3.one * labelScale;

        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();

        label.text = $"{cm}";
        label.fontSize = 36;
        label.alignment = TextAlignmentOptions.Center;
    }
}