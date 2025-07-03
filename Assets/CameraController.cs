using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For Image
using TMPro;          // For TMP_Text

public class CameraController : MonoBehaviour
{
    public Transform _camera;
    public Transform hand;

    [Header("Camera Rotation")]
    public float cameraSensitivity = 200f;
    public float cameraAcceleration = 5.0f;

    [Header("Camera Bobbing")]
    public float walkBobFrequency = 8f;
    public float idleBobFrequency = 1.5f;
    public float bobAmplitude = 0.05f;
    public float bobLerpSpeed = 5f;

    [Header("Camera Tilt")]
    public float tiltAngle = 5f;
    public float tiltLerpSpeed = 5f;

    [Header("Flashlight Battery")]
    public Light flashlightSpotlight;
    public List<Image> batteryBars; // Assign 4 images here, right to left order
    public TMP_Text batteryPercentText; // Drag your TMP text here

    public float drainInterval = 20f; // total time for battery to drain from 100% to 0%
    private float batteryPercentage = 100f; // 100% full battery

    private float currentIntensity;

    private float rotation_x_axis;
    private float rotation_y_axis;

    private float bobTimer = 0f;
    private float originalCamY;
    private float targetTilt = 0f;
    private float currentTilt = 0f;

    private float[] intensityLevels = new float[] { 0.4f, 2.5f, 5f, 8f, 8f };

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        originalCamY = _camera.localPosition.y;

        batteryPercentage = 100f;

        if (flashlightSpotlight != null)
        {
            currentIntensity = flashlightSpotlight.intensity = intensityLevels[4];
        }
        else
        {
            Debug.LogWarning("Flashlight Spotlight not assigned!");
        }
    }

    void Update()
    {
        // --- Mouse look ---

        rotation_x_axis += Input.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;
        rotation_y_axis += Input.GetAxis("Mouse X") * cameraSensitivity * Time.deltaTime;

        rotation_x_axis = Mathf.Clamp(rotation_x_axis, -90f, 90f);

        hand.localRotation = Quaternion.Euler(-rotation_x_axis, rotation_y_axis, 0);

        transform.localRotation = Quaternion.Lerp(transform.localRotation,
            Quaternion.Euler(0, rotation_y_axis, 0), cameraAcceleration * Time.deltaTime);

        _camera.localRotation = Quaternion.Lerp(_camera.localRotation,
            Quaternion.Euler(-rotation_x_axis, 0, currentTilt), cameraAcceleration * Time.deltaTime);

        // --- Camera Bobbing and Tilt ---

        ApplyCameraBobbing();
        ApplyCameraTilt();

        // --- Battery Drain ---
        HandleBatteryDrain();

        // --- Update Battery UI ---
        UpdateBatteryUI();

        // --- Update battery percentage text ---
        UpdateBatteryPercentageText();

        // --- Smoothly update flashlight intensity ---
        SmoothUpdateFlashlightIntensity();

        // --- Check for battery pickup ---
        CheckBatteryPickup();
    }

    void ApplyCameraBobbing()
    {
        float horizontalInput = Mathf.Abs(Input.GetAxisRaw("Horizontal"));
        float verticalInput = Mathf.Abs(Input.GetAxisRaw("Vertical"));
        bool isMoving = (horizontalInput > 0.1f || verticalInput > 0.1f);

        float frequency = isMoving ? walkBobFrequency : idleBobFrequency;
        float amplitude = isMoving ? bobAmplitude : bobAmplitude * 0.5f;

        bobTimer += Time.deltaTime * frequency;

        float offsetY = Mathf.Sin(bobTimer) * amplitude;

        Vector3 camPos = _camera.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, originalCamY + offsetY, Time.deltaTime * bobLerpSpeed);
        _camera.localPosition = camPos;
    }

    void ApplyCameraTilt()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        targetTilt = Mathf.Lerp(targetTilt, -horizontalInput * tiltAngle, Time.deltaTime * tiltLerpSpeed);

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltLerpSpeed);
    }

    void HandleBatteryDrain()
    {
        if (batteryPercentage <= 0f)
        {
            batteryPercentage = 0f;
            return;
        }

        batteryPercentage -= (100f / drainInterval) * Time.deltaTime; // percentage per second
        batteryPercentage = Mathf.Clamp(batteryPercentage, 0f, 100f);
    }

    void UpdateBatteryUI()
    {
        if (batteryBars == null || batteryBars.Count != 4)
        {
            Debug.LogWarning("Battery bars list not set properly.");
            return;
        }

        // Calculate how many bars to show (each bar is 25%)
        int barsToShow = Mathf.CeilToInt(batteryPercentage / 25f);

        for (int i = 0; i < 4; i++)
        {
            if (i < barsToShow)
            {
                // Full color bars on (right to left)
                batteryBars[i].color = Color.Lerp(Color.red, Color.yellow, (float)i / 3f);
                batteryBars[i].enabled = true;
            }
            else
            {
                // Dim or hide empty bars
                batteryBars[i].color = Color.gray;
                batteryBars[i].enabled = true;
            }
        }
    }

    void UpdateBatteryPercentageText()
    {
        if (batteryPercentText == null)
            return;

        if (batteryPercentage <= 0f)
        {
            batteryPercentText.text = "NEED BATTERY";
        }
        else
        {
            batteryPercentText.text = Mathf.CeilToInt(batteryPercentage) + "%";
        }
    }

    void SmoothUpdateFlashlightIntensity()
    {
        if (flashlightSpotlight == null)
            return;

        // Intensity interpolates from full (4 bars) to empty (0 bars)
        int bars = Mathf.CeilToInt(batteryPercentage / 25f);
        bars = Mathf.Clamp(bars, 0, 4);

        float targetIntensity = intensityLevels[bars];

        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * 3f);
        flashlightSpotlight.intensity = currentIntensity;
    }

    void CheckBatteryPickup()
    {
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 20f))
            {
                if (hit.collider.CompareTag("Battery"))
                {
                    Destroy(hit.collider.gameObject);
                    batteryPercentage = 100f;
                }
            }
        }
    }
}
