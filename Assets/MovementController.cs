using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Transform head;
    public AudioSource audioSource;

    [Header("Movement Settings")]
    public float walkForwardSpeed = 5f;
    public float walkSideBackwardSpeed = 3f;
    public float sprintForwardSpeed = 8f;
    public float sprintSideBackwardSpeed = 5f;
    public float acceleration = 20f;
    public float jumpForce = 5f;
    public LayerMask groundLayer;

    [Header("Footsteps")]
    public List<AudioClip> footstepClips = new List<AudioClip>();
    public float baseStepCooldown = 0.5f;       // time between footsteps when walking
    public float sprintStepCooldownMultiplier = 0.6f; // footsteps faster when sprinting
    public float footstepVolume = 1.0f;

    private Rigidbody rb;
    private Vector3 moveDirection;

    private float footstepCooldownTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (audioSource == null)
            Debug.LogWarning("AudioSource is not assigned!");
    }

    void Update()
    {
        moveDirection = Vector3.zero;

        bool moved = false;

        // Movement input detection (WASD or ZQSD)
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Z)) { moveDirection += head.forward; moved = true; }
        if (Input.GetKey(KeyCode.S)) { moveDirection -= head.forward; moved = true; }
        if (Input.GetKey(KeyCode.D)) { moveDirection += head.right; moved = true; }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q)) { moveDirection -= head.right; moved = true; }

        moveDirection.y = 0f;
        moveDirection = moveDirection.normalized;

        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            Vector3 vel = rb.velocity;
            vel.y = 0f;
            rb.velocity = vel;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        HandleFootsteps(moved);
    }

    void FixedUpdate()
    {
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        float forwardAmount = Vector3.Dot(moveDirection, head.forward);
        float sideAmount = Vector3.Dot(moveDirection, head.right);

        float forwardSpeed = isSprinting ? sprintForwardSpeed : walkForwardSpeed;
        float sideSpeed = isSprinting ? sprintSideBackwardSpeed : walkSideBackwardSpeed;

        float targetSpeed = 0f;

        if (forwardAmount > 0.1f) targetSpeed = forwardSpeed;
        else if (forwardAmount < -0.1f) targetSpeed = sideSpeed;

        if (Mathf.Abs(sideAmount) > 0.1f) targetSpeed = Mathf.Max(targetSpeed, sideSpeed);

        Vector3 desiredVelocity = moveDirection * targetSpeed;

        if (desiredVelocity.magnitude > 0.1f)
        {
            float castDistance = 0.6f;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            RaycastHit hit;

            if (Physics.SphereCast(origin, 0.3f, desiredVelocity.normalized, out hit, castDistance, groundLayer))
            {
                Vector3 hitNormal = hit.normal;
                desiredVelocity = Vector3.ProjectOnPlane(desiredVelocity, hitNormal);

                if (desiredVelocity.magnitude < 0.1f)
                    desiredVelocity = Vector3.zero;
            }
        }

        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        Vector3 velocityChange = desiredVelocity - horizontalVelocity;

        Vector3 accelerationVector = Vector3.ClampMagnitude(velocityChange, acceleration * Time.fixedDeltaTime);
        rb.AddForce(accelerationVector, ForceMode.VelocityChange);
    }

    void HandleFootsteps(bool isMoving)
    {
        if (audioSource == null || footstepClips.Count == 0)
            return;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        // Update cooldown timer
        footstepCooldownTimer -= Time.deltaTime;

        if (isMoving && footstepCooldownTimer <= 0f)
        {
            PlayFootstepSound();
            footstepCooldownTimer = baseStepCooldown * (isSprinting ? sprintStepCooldownMultiplier : 1f);
        }
    }

    void PlayFootstepSound()
    {
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Count)];
        audioSource.PlayOneShot(clip, footstepVolume);
    }

    bool IsGrounded()
    {
        return Physics.CheckBox(transform.position + Vector3.down * 0.6f, new Vector3(0.3f, 0.05f, 0.3f), Quaternion.identity, groundLayer);
    }
}
