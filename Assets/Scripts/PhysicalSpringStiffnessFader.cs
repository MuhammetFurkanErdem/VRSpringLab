using UnityEngine;

public class PhysicalSpringStiffnessFader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform faderHandle;
    [SerializeField] private SpringSimulation springSimulation;

    [Header("Fader Y Range")]
    [SerializeField] private float minY = -0.0807f;
    [SerializeField] private float maxY = 0.0735f;

    [Header("Spring Constant Range")]
    [SerializeField] private float minSpringConstant = 2f;
    [SerializeField] private float maxSpringConstant = 10f;

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

        // Fader sadece Y ekseninde hareket edebilsin.
        position.x = lockedX;
        position.z = lockedZ;

        // Fiziksel hareket sınırları.
        position.y = Mathf.Clamp(
            position.y,
            minY,
            maxY
        );

        faderHandle.localPosition = position;
        faderHandle.localRotation = lockedRotation;

        // Y konumunu 0-1 aralığına dönüştür.
        float t = Mathf.InverseLerp(
            minY,
            maxY,
            position.y
        );

        // 2-10 N/m arasında yay sertliğine dönüştür.
        float springConstant = Mathf.Lerp(
            minSpringConstant,
            maxSpringConstant,
            t
        );

        springSimulation.SetSpringConstant(
            springConstant
        );
    }
}