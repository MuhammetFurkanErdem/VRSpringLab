using UnityEngine;

public class PeriodMeasurement : MonoBehaviour
{
    [SerializeField] private SpringSimulation springSimulation;

    private float previousRelativePosition;
    private float lastCrossingTime = -1f;
    [SerializeField] private float measuredPeriod;

    public float MeasuredPeriod => measuredPeriod;

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

            return 2f * Mathf.PI *
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

        float relativePosition =
            springSimulation.DisplacementMeters -
            springSimulation.EquilibriumDisplacementMeters;

        // Denge noktasını aşağı yönde geçiş.
        // Aynı yöndeki iki geçiş arasında tam 1 periyot vardır.
        bool crossedDownward =
            previousRelativePosition < 0f &&
            relativePosition >= 0f &&
            springSimulation.VelocityMetersPerSecond > 0f;

        if (crossedDownward)
        {
            float currentTime = Time.time;

            if (lastCrossingTime >= 0f)
            {
                measuredPeriod =
                    currentTime - lastCrossingTime;
            }

            lastCrossingTime = currentTime;
        }

        previousRelativePosition =
            relativePosition;
    }

    private void ResetMeasurement()
    {
        previousRelativePosition = 0f;
        lastCrossingTime = -1f;
        measuredPeriod = 0f;
    }
}