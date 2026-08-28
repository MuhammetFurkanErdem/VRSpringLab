using TMPro;
using UnityEngine;

public class SpringInfoDisplay : MonoBehaviour
{
    [SerializeField] private SpringSimulation springSimulation;
    [SerializeField] private TMP_Text displacementText;

    private void Update()
    {
        float cm = springSimulation.DisplacementCentimeters;

        displacementText.text = $"Uzama: {cm:F1} cm";
    }
}