using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitTrigger : MonoBehaviour
{
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        // Assuming your player has tag "Player"
        if (other.CompareTag("Player"))
        {
            triggered = true;
            Debug.Log("You Win!");

            // TODO: Replace with your winning scene name
            SceneManager.LoadScene("WinScene");
        }
    }
}
