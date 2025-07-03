using UnityEngine;
using UnityEngine.UI;

public class JumpscareTrigger : MonoBehaviour
{
    [Header("Images")]
    public RawImage rawImage1;
    public RawImage rawImage2;

    [Header("Audio")]
    public AudioSource jumpscareAudio;

    private bool hasJumpscareStarted = false;

    void Start()
    {
        if (rawImage1 != null) rawImage1.enabled = true;
        if (rawImage2 != null) rawImage2.enabled = false;

        StartCoroutine(TriggerJumpscare());
    }

    System.Collections.IEnumerator TriggerJumpscare()
    {
        yield return new WaitForSeconds(3f);

        if (rawImage1 != null) rawImage1.enabled = false;
        if (rawImage2 != null) rawImage2.enabled = true;

        if (jumpscareAudio != null)
        {
            jumpscareAudio.Play();
            hasJumpscareStarted = true;
        }
    }

    void Update()
    {
        if (hasJumpscareStarted && !jumpscareAudio.isPlaying)
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
