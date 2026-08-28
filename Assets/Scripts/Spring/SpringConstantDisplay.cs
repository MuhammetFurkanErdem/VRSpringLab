using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpringConstantDisplay : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text valueText;

    private void OnEnable()
    {
        if (slider == null)
            return;

        slider.onValueChanged.AddListener(UpdateText);
        UpdateText(slider.value);
    }

    private void OnDisable()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(UpdateText);
    }

    private void UpdateText(float value)
    {
        if (valueText != null)
            valueText.text = value.ToString("0.0");
    }
}