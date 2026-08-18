using UnityEngine;

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

    public void SetFreeLengthVisible(bool visible)
    {
        freeLengthLine.SetActive(visible);
    }

    public void SetEquilibriumVisible(bool visible)
    {
        equilibriumLine.SetActive(visible);
    }

    public void SetDisplacementVisible(bool visible)
    {
        displacementInfo.SetActive(visible);
    }

    public void SetVelocityVisible(bool visible)
    {
        velocityVector.SetActive(visible);
    }

    public void SetAccelerationVisible(bool visible)
    {
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
}