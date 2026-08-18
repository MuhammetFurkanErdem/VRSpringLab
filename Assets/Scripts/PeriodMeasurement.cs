using UnityEngine;

public class PeriodMeasurement : MonoBehaviour
{
    [SerializeField]
    private SpringSimulation springSimulation;

    private float previousRelativePosition;
    private float lastCrossingTime = -1f;

    [SerializeField]
    private float measuredPeriod;

    private float simulationTime;


    // ------------------------------------------------
    // Public values
    // ------------------------------------------------

    public float MeasuredPeriod =>
        measuredPeriod;

    public float TheoreticalPeriod
    {
        get
        {
            if (springSimulation == null ||
                !springSimulation.HasWeight ||
                springSimulation.CurrentWeight == null)
            {
                return 0f;
            }

            float mass =
                springSimulation.CurrentWeight.mass;

            float k =
                springSimulation.SpringConstant;

            return
                2f *
                Mathf.PI *
                Mathf.Sqrt(mass / k);
        }
    }

    private void FixedUpdate()
    {
        if (springSimulation == null ||
            !springSimulation.HasWeight)
        {
            ResetMeasurement();
            return;
        }

        // Pause sırasında 0,
        // normal modda fixedDeltaTime,
        // slow motion'da fixedDeltaTime * 0.25.
        simulationTime +=
            springSimulation.SimulationDeltaTime;

        // Pause durumunda ölçüm yapma.
        if (springSimulation.IsPaused)
            return;

        float relativePosition =
            springSimulation.DisplacementMeters -
            springSimulation.EquilibriumDisplacementMeters;

        // ------------------------------------------------
        // Denge noktasını aşağı yönde geçiş.
        //
        // Aynı yöndeki iki denge geçişi arasındaki süre
        // tam bir periyottur.
        // ------------------------------------------------

        bool crossedDownward =
            previousRelativePosition < 0f &&
            relativePosition >= 0f &&
            springSimulation.VelocityMetersPerSecond > 0f;

        if (crossedDownward)
        {
            float currentTime =
                simulationTime;

            if (lastCrossingTime >= 0f)
            {
                measuredPeriod =
                    currentTime -
                    lastCrossingTime;
            }

            lastCrossingTime =
                currentTime;
        }

        previousRelativePosition =
            relativePosition;
    }

    public void ResetMeasurement()
    {
        previousRelativePosition = 0f;

        lastCrossingTime = -1f;

        measuredPeriod = 0f;

        simulationTime = 0f;
    }
}