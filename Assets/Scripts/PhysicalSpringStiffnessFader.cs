using UnityEngine;
using TMPro;

public class PhysicalSpringStiffnessFader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform faderHandle;
    [SerializeField] private SpringSimulation springSimulation;

    [Header("Visual Bar")]
    [SerializeField] private Transform barFill;
    [SerializeField] private Transform barMarker;
    [SerializeField] private TMP_Text valueText;

    [Header("Fader Y Range")]
    [SerializeField] private float minY = -0.0807f;
    [SerializeField] private float maxY = 0.0735f;

    [Header("Spring Constant Range")]
    [SerializeField] private float minSpringConstant = 2f;
    [SerializeField] private float maxSpringConstant = 10f;

    [Header("Bar Fill Settings")]
    [SerializeField] private float fillMinHeight = 0.01f;
    [SerializeField] private float fillMaxHeight = 0.22f;
    [SerializeField] private float fillBottomLocalY = 0f;

    private float lockedX;
    private float lockedZ;
    private Quaternion lockedRotation;

    private void Awake()
    {
        if (faderHandle == null)
            return;

        lockedX = faderHandle.localPosition.x;
        lockedZ = faderHandle.localPosition.z;
        lockedRotation = faderHandle.localRotation;
    }

    private void LateUpdate()
    {
        if (faderHandle == null || springSimulation == null)
            return;

        Vector3 position = faderHandle.localPosition;

        // Sadece Y ekseninde hareket etsin
        position.x = lockedX;
        position.z = lockedZ;
        position.y = Mathf.Clamp(position.y, minY, maxY);

        faderHandle.localPosition = position;
        faderHandle.localRotation = lockedRotation;

        // 0-1 arası normalize
        float t = Mathf.InverseLerp(minY, maxY, position.y);

        // Yay sabiti
        float springConstant = Mathf.Lerp(
            minSpringConstant,
            maxSpringConstant,
            t
        );

        springSimulation.SetSpringConstant(springConstant);

        UpdateVisuals(t, springConstant);
    }

    private void UpdateVisuals(float t, float springConstant)
    {
        if (barFill != null)
        {
            float height = Mathf.Lerp(fillMinHeight, fillMaxHeight, t);

            Vector3 scale = barFill.localScale;
            scale.y = height;
            barFill.localScale = scale;

            Vector3 pos = barFill.localPosition;
            pos.y = fillBottomLocalY + height * 0.5f;
            barFill.localPosition = pos;
        }

        if (barMarker != null)
        {
            float markerY = Mathf.Lerp(
                fillBottomLocalY,
                fillBottomLocalY + fillMaxHeight,
                t
            );

            Vector3 pos = barMarker.localPosition;
            pos.y = markerY;
            barMarker.localPosition = pos;
        }

        if (valueText != null)
        {
            valueText.text = springConstant.ToString("0.0") + " N/m";
        }
    }
}