using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpringLabUIController : MonoBehaviour
{
    [Header("Scene Features")]
    [SerializeField] private GameObject freeLengthLine;
    [SerializeField] private GameObject equilibriumLine;
    [SerializeField] private GameObject displacementInfo;
    [SerializeField] private GameObject springInfo;
    [SerializeField] private GameObject velocityVector;
    [SerializeField] private GameObject accelerationVector;
    [SerializeField] private GameObject gravityForceVector;
    [SerializeField] private GameObject springForceVector;
    [SerializeField] private GameObject periodTrace;

    [Header("Simulation")]
    [SerializeField] private SpringSimulation springSimulation;
    [SerializeField] private PeriodMeasurement periodMeasurement;

    [Header("Reset UI")]
    [SerializeField] private Slider springConstantSlider;
    [SerializeField] private Toggle pauseToggle;
    [SerializeField] private Toggle slowMotionToggle;
    [SerializeField] private Toggle continuousOscillationToggle;
    [SerializeField] private Toggle gravityToggle;
    [SerializeField] private TMP_Dropdown gravityPresetDropdown;

    public void SetFreeLengthVisible(bool visible)
    {
        if (freeLengthLine != null)
            freeLengthLine.SetActive(visible);
    }

    public void SetEquilibriumVisible(bool visible)
    {
        if (equilibriumLine != null)
            equilibriumLine.SetActive(visible);
    }

    public void SetDisplacementVisible(bool visible)
    {
        if (displacementInfo != null)
            displacementInfo.SetActive(visible);
    }

    public void SetVelocityVisible(bool visible)
    {
        if (velocityVector != null)
            velocityVector.SetActive(visible);
    }

    public void SetAccelerationVisible(bool visible)
    {
        if (accelerationVector != null)
            accelerationVector.SetActive(visible);
    }

    public void SetForcesVisible(bool visible)
    {
        if (gravityForceVector != null)
            gravityForceVector.SetActive(visible);

        if (springForceVector != null)
            springForceVector.SetActive(visible);
    }

    public void SetInfoPanelVisible(bool visible)
    {
        if (springInfo != null)
            springInfo.SetActive(visible);
    }

    public void SetPeriodTraceVisible(bool visible)
    {
        if (periodTrace != null)
            periodTrace.SetActive(visible);
    }

    public void SetSimulationPaused(bool paused)
    {
        if (springSimulation != null)
            springSimulation.SetPaused(paused);
    }

    public void SetSlowMotion(bool slow)
    {
        if (springSimulation != null)
            springSimulation.SetSlowMotion(slow);
    }

    public void SetContinuousOscillation(bool enabled)
    {
        if (springSimulation != null)
            springSimulation.SetContinuousOscillation(enabled);
    }

    public void SetGravityEnabled(bool enabled)
    {
        if (springSimulation != null)
            springSimulation.SetGravityEnabled(enabled);
    }

    public void SetGravityPreset(int index)
    {
        if (springSimulation != null)
            springSimulation.SetGravityPreset(index);
    }

    // ------------------------------------------------
    // Reset Experiment
    // ------------------------------------------------

    public void ResetExperiment()
    {
        // Simülasyonu başlangıç durumuna döndür.
        if (springSimulation != null)
            springSimulation.ResetSimulation();

        // Periyot ölçümünü temizle.
        if (periodMeasurement != null)
            periodMeasurement.ResetMeasurement();

        // Slider'ı başlangıç değerine getir.
        // Normal value kullanıyoruz ki Value Text de güncellensin.
        if (springConstantSlider != null)
            springConstantSlider.value = 4f;

        // Pause görselini ve durumunu kapat.
        if (pauseToggle != null)
            pauseToggle.isOn = false;

        // Slow Motion görselini ve durumunu kapat.
        if (slowMotionToggle != null)
            slowMotionToggle.isOn = false;

        // ResetSimulation zaten fizik durumunu kapattığı için event'i tekrar çağırma.
        if (continuousOscillationToggle != null)
            continuousOscillationToggle.SetIsOnWithoutNotify(false);

        // Yer çekimi durumunu ve toggle görselini başlangıca döndür.
        if (gravityToggle != null)
            gravityToggle.isOn = true;

        // Gezegen seçimini Dünya'ya döndür.
        if (gravityPresetDropdown != null)
            gravityPresetDropdown.value = 0;
    }
}
