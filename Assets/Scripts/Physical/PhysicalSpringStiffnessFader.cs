using UnityEngine;
using TMPro;
using Oculus.Interaction;

[DefaultExecutionOrder(10000)]
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
    [SerializeField] private float fillMinHeight = 0.002f;
    [SerializeField] private float fillMaxHeight = 0.203f;
    [SerializeField] private float fillBottomLocalY = -0.0204f;

    [Header("Reset")]
    [SerializeField] private float defaultSpringConstant = 4f;

    private float lockedX;
    private float lockedZ;
    private Quaternion lockedRotation;

    private Grabbable faderGrabbable;
    private ITransformer faderTransformer;
    private Rigidbody faderRigidbody;
    private bool resetPoseSyncPending;

    private void Awake()
    {
        if (faderHandle == null)
            return;

        lockedX = faderHandle.localPosition.x;
        lockedZ = faderHandle.localPosition.z;
        lockedRotation = faderHandle.localRotation;

        faderGrabbable = faderHandle.GetComponent<Grabbable>();
        faderRigidbody = faderHandle.GetComponent<Rigidbody>();

        MonoBehaviour[] behaviours =
            faderHandle.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is ITransformer transformer)
            {
                faderTransformer = transformer;
                break;
            }
        }
    }

    private void LateUpdate()
    {
        if (faderHandle == null || springSimulation == null)
            return;

        // Reset pointer event'i sırasında Meta interaction sistemi aynı frame'de
        // cached pose'u tekrar yazabilir. Reset pose'unu frame sonunda yalnızca
        // bir kez yeniden uygula ve o frame eski fiziksel değeri okuma.
        if (resetPoseSyncPending)
        {
            ApplyResetPose();
            ApplyDefaultValueAndVisuals();
            resetPoseSyncPending = false;
            return;
        }

        Vector3 position = faderHandle.localPosition;

        // X/Z ve rotation her zaman kilitli.
        position.x = lockedX;
        position.z = lockedZ;

        position.y = Mathf.Clamp(
            position.y,
            minY,
            maxY
        );

        faderHandle.localPosition = position;
        faderHandle.localRotation = lockedRotation;

        ApplyValueFromPosition(position.y);
    }

    private void ApplyValueFromPosition(float y)
    {
        float t = Mathf.InverseLerp(
            minY,
            maxY,
            y
        );

        float springConstant = Mathf.Lerp(
            minSpringConstant,
            maxSpringConstant,
            t
        );

        springSimulation.SetSpringConstant(springConstant);

        UpdateVisuals(t, springConstant);
    }

    private float GetYForSpringConstant(float springConstant)
    {
        float t = Mathf.InverseLerp(
            minSpringConstant,
            maxSpringConstant,
            springConstant
        );

        return Mathf.Lerp(
            minY,
            maxY,
            t
        );
    }

    public void ResetToDefault()
    {
        ApplyResetPose();
        ApplyDefaultValueAndVisuals();

        // Reset çağrısı interaction event sırasının ortasında gelebilir.
        // LateUpdate'taki tek seferlik senkron, daha sonra çalışan transformer
        // Move çağrısının eski pose'u geri getirmesini engeller.
        resetPoseSyncPending = true;
    }

    private void ApplyResetPose()
    {
        if (faderHandle == null)
            return;

        float targetY =
            GetYForSpringConstant(defaultSpringConstant);

        bool isActivelyGrabbed =
            faderGrabbable != null &&
            faderGrabbable.SelectingPointsCount > 0;

        if (isActivelyGrabbed && faderTransformer != null)
            faderTransformer.EndTransform();

        Vector3 position = faderHandle.localPosition;
        position.x = lockedX;
        position.y = targetY;
        position.z = lockedZ;

        faderHandle.localPosition = position;
        faderHandle.localRotation = lockedRotation;

        // Transform ve kinematic Rigidbody pose'larını aynı anda güncelle.
        // Böylece physics sync eski world pose'u geri taşıyamaz.
        if (faderRigidbody != null)
        {
            faderRigidbody.position = faderHandle.position;
            faderRigidbody.rotation = faderHandle.rotation;

            if (!faderRigidbody.isKinematic)
            {
                faderRigidbody.linearVelocity = Vector3.zero;
                faderRigidbody.angularVelocity = Vector3.zero;
            }
        }

        // Aktif grab devam ediyorsa yeni fiziksel pose'u transformer'in
        // başlangıç state'i yap; sonraki Move eski pose'u geri yazmasın.
        if (isActivelyGrabbed && faderTransformer != null)
            faderTransformer.BeginTransform();
    }

    private void ApplyDefaultValueAndVisuals()
    {
        if (springSimulation != null)
            springSimulation.SetSpringConstant(defaultSpringConstant);

        float t = Mathf.InverseLerp(
            minSpringConstant,
            maxSpringConstant,
            defaultSpringConstant
        );

        UpdateVisuals(t, defaultSpringConstant);
    }

    private void UpdateVisuals(float t, float springConstant)
    {
        if (barFill != null)
        {
            float height = Mathf.Lerp(
                fillMinHeight,
                fillMaxHeight,
                t
            );

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
            valueText.text =
                springConstant.ToString("0.0") + " N/m";
        }
    }
}
