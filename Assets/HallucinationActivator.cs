using UnityEngine;

public class HallucinationActivator : MonoBehaviour
{
    [Header("Light to Monitor")]
    public Light flashlight;

    [Header("Hallucination to Activate")]
    public GameObject hallucinationObject;

    private bool isActive = false;

    void Update()
    {
        if (flashlight == null || hallucinationObject == null)
            return;

        float intensity = flashlight.intensity;

        if (intensity <= 1f && !isActive)
        {
            hallucinationObject.SetActive(true);
            isActive = true;
        }
        else if (intensity > 1f && isActive)
        {
            hallucinationObject.SetActive(false);
            isActive = false;
        }
    }
}
