using UnityEngine;

public class PhysicalStartStopPanel : MonoBehaviour
{
    // Indicador_Pantalla.002 uses submesh 0 for the bezel and submesh 1
    // for the circular indicator face. Only the face may change state.
    private const int IndicatorCenterMaterialIndex = 1;

    [Header("Simulation")]
    [SerializeField] private SpringSimulation springSimulation;

    [Header("Buttons")]
    [SerializeField] private Transform startButton;
    [SerializeField] private Transform stopButton;

    [Header("Button Press Visual")]
    [SerializeField]
    private Vector3 startPressOffset =
        new Vector3(0f, -0.01f, 0f);

    [SerializeField]
    private Vector3 stopPressOffset =
        new Vector3(0f, -0.01f, 0f);

    [SerializeField] private float buttonMoveSpeed = 0.15f;

    [Header("Indicator Lights")]
    [SerializeField] private Renderer greenLightRenderer;
    [SerializeField] private Renderer redLightRenderer;

    [Header("Indicator Materials")]
    [SerializeField] private Material inactiveMaterial;
    [SerializeField] private Material startActiveMaterial;
    [SerializeField] private Material stopActiveMaterial;

    private Vector3 startRestPosition;
    private Vector3 stopRestPosition;

    private bool startPressed;
    private bool stopPressed;

    private bool lastPausedState;

    private void Awake()
    {
        if (startButton != null)
            startRestPosition = startButton.localPosition;

        if (stopButton != null)
            stopRestPosition = stopButton.localPosition;
    }

    private void Start()
    {
        if (springSimulation != null)
        {
            lastPausedState = springSimulation.IsPaused;
            UpdateIndicators(lastPausedState);
        }
    }

    private void Update()
    {
        AnimateButtons();

        // Pause başka bir sistem tarafından değişirse
        // panel ışıkları yine doğru state'i göstersin.
        if (springSimulation != null &&
            springSimulation.IsPaused != lastPausedState)
        {
            lastPausedState = springSimulation.IsPaused;
            UpdateIndicators(lastPausedState);
        }
    }

    // ------------------------------------------------
    // START
    // ------------------------------------------------

    public void PressStart()
    {
        Debug.Log("START BUTTON PRESSED");

        startPressed = true;

        if (springSimulation != null)
            springSimulation.SetPaused(false);

        SetPausedState(false);
    }

    public void ReleaseStart()
    {
        startPressed = false;
    }

    // ------------------------------------------------
    // STOP
    // ------------------------------------------------

    public void PressStop()
    {
        Debug.Log("STOP BUTTON PRESSED");

        stopPressed = true;

        if (springSimulation != null)
            springSimulation.SetPaused(true);

        SetPausedState(true);
    }

    public void ReleaseStop()
    {
        stopPressed = false;
    }

    // ------------------------------------------------
    // State
    // ------------------------------------------------

    private void SetPausedState(bool paused)
    {
        lastPausedState = paused;
        UpdateIndicators(paused);
    }

    private void UpdateIndicators(bool paused)
    {
        if (greenLightRenderer == null || redLightRenderer == null)
            return;

        if (paused)
        {
            // STOP aktif
            SetMaterialAtIndex(
                greenLightRenderer,
                IndicatorCenterMaterialIndex,
                inactiveMaterial
            );

            SetMaterialAtIndex(
                redLightRenderer,
                IndicatorCenterMaterialIndex,
                stopActiveMaterial
            );
        }
        else
        {
            // START aktif
            SetMaterialAtIndex(
                greenLightRenderer,
                IndicatorCenterMaterialIndex,
                startActiveMaterial
            );

            SetMaterialAtIndex(
                redLightRenderer,
                IndicatorCenterMaterialIndex,
                inactiveMaterial
            );
        }
    }

    private void SetMaterialAtIndex(
        Renderer targetRenderer,
        int materialIndex,
        Material material)
    {
        if (targetRenderer == null || material == null)
            return;

        Material[] materials = targetRenderer.sharedMaterials;

        if (materialIndex < 0 ||
            materialIndex >= materials.Length)
        {
            Debug.LogWarning(
                $"{targetRenderer.name}: Material index {materialIndex} geçersiz."
            );

            return;
        }

        materials[materialIndex] = material;
        targetRenderer.sharedMaterials = materials;
    }

    // ------------------------------------------------
    // Button visuals
    // ------------------------------------------------

    private void AnimateButtons()
    {
        if (startButton != null)
        {
            Vector3 target =
                startPressed
                    ? startRestPosition + startPressOffset
                    : startRestPosition;

            startButton.localPosition =
                Vector3.MoveTowards(
                    startButton.localPosition,
                    target,
                    buttonMoveSpeed * Time.deltaTime
                );
        }

        if (stopButton != null)
        {
            Vector3 target =
                stopPressed
                    ? stopRestPosition + stopPressOffset
                    : stopRestPosition;

            stopButton.localPosition =
                Vector3.MoveTowards(
                    stopButton.localPosition,
                    target,
                    buttonMoveSpeed * Time.deltaTime
                );
        }
    }
}
