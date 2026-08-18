using UnityEngine;
using TMPro;

public class PeriodDisplay : MonoBehaviour
{
    [SerializeField] private PeriodMeasurement periodMeasurement;
    [SerializeField] private TMP_Text measuredText;
    [SerializeField] private TMP_Text theoreticalText;

    private void Update()
    {
        if (periodMeasurement == null)
            return;

        float measured =
            periodMeasurement.MeasuredPeriod;

        float theoretical =
            periodMeasurement.TheoreticalPeriod;

        if (measured > 0f)
        {
            measuredText.text =
                $"Ölçülen: {measured:0.00} s";
        }
        else
        {
            measuredText.text =
                "Ölçülen: -- s";
        }

        if (theoretical > 0f)
        {
            theoreticalText.text =
                $"Teorik: {theoretical:0.00} s";
        }
        else
        {
            theoreticalText.text =
                "Teorik: -- s";
        }
    }
}