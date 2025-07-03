using UnityEngine;
using UnityEngine.SceneManagement;

public class HallucinationHandler : MonoBehaviour
{
    [Header("Light to Monitor")]
    public Light flashlight;

    [Header("Player")]
    public Transform player;
    public float moveSpeed = 2f;

    private Camera mainCam;
    private bool isActive = false;

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        mainCam = Camera.main;
        gameObject.SetActive(false); // Start hidden
    }

    void Update()
    {
        if (flashlight == null || player == null) return;

        float intensity = flashlight.intensity;

        if (intensity <= 1f)
        {
            if (!isActive)
            {
                ActivateHallucination();
            }

            FollowPlayer();
        }
        else
        {
            if (isActive)
            {
                DeactivateHallucination();
            }
        }
    }

    void ActivateHallucination()
    {
        isActive = true;
        gameObject.SetActive(true);

        // Move hallucination far behind camera
        Vector3 spawnPos = mainCam.transform.position - mainCam.transform.forward * 100f;
        spawnPos.y = player.position.y;
        transform.position = spawnPos;
    }

    void DeactivateHallucination()
    {
        isActive = false;
        gameObject.SetActive(false);

        // Reset position far behind camera again
        Vector3 awayPos = mainCam.transform.position - mainCam.transform.forward * 100f;
        awayPos.y = player.position.y;
        transform.position = awayPos;
    }

    void FollowPlayer()
    {
        Vector3 targetPos = player.position;
        targetPos.y = transform.position.y; // Lock Y position
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene("HallucinationGameOver");
        }
    }
}
