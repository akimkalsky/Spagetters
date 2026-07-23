using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 4f;
    public float turnSpeed = 180f;
    public float gravity = -20f;

    private CharacterController controller;
    private Vector3 velocity;
    public SpriteAnimation anim;
    private Goon currentTarget;
    public float sightDistance = 20f;

    //steppies
    private Vector3 lastPosition;
    private float walkedDistance = 0f;

    public float stepDistance = 1f; // 1 Unity unit = 1 step

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        lastPosition = transform.position;
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        // Forward / Back
        float forward = 0f;

        if (Keyboard.current.wKey.isPressed)
            forward = 1f;
        else if (Keyboard.current.sKey.isPressed)
            forward = -1f;

        if (forward > 0)
        {
            anim.WalkForward();
        }
        else if (forward < 0)
        {
            anim.WalkBackward();
        }
        else
        {
            anim.PlayIdle();
        }

        // Rotate
        float turn = 0f;

        if (Keyboard.current.aKey.isPressed)
            turn = -1f;
        else if (Keyboard.current.dKey.isPressed)
            turn = 1f;

        // Rotate player
        transform.Rotate(Vector3.up * turn * turnSpeed * Time.deltaTime);

        // Gravity
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        // Move in the direction the player is facing
        Vector3 motion = transform.forward * forward * moveSpeed;
        motion.y = velocity.y;

        controller.Move(motion * Time.deltaTime);
        // Count distance walked
        walkedDistance += Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        while (walkedDistance >= stepDistance)
        {
            walkedDistance -= stepDistance;

            GameManager.Instance.UseStep();
        }



        // Look for a goon in front of the player
        Vector3 rayOrigin = transform.position + controller.center;
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, transform.forward, out hit, sightDistance))
        {
            Debug.DrawRay(rayOrigin, transform.forward * sightDistance, Color.red);

            Goon goon = hit.collider.GetComponent<Goon>();

            if (goon != null)
            {
                // Hide previous target's UI
                if (currentTarget != null && currentTarget != goon)
                    currentTarget.HideUI();

                currentTarget = goon;

                // Show countdown
                float distance = hit.distance;

                int stepsAway = Mathf.CeilToInt(distance / stepDistance);
                int remaining = stepsAway - goon.duelDistance;

                goon.UpdateCountdown(remaining);

                Debug.Log($"Goon spotted! Duel starts at {goon.duelDistance} steps.");
            }
            else
            {
                if (currentTarget != null)
                {
                    currentTarget.HideUI();
                    currentTarget = null;
                }
            }
        }
        else
        {
            Debug.DrawRay(rayOrigin, transform.forward * sightDistance, Color.green);

            if (currentTarget != null)
            {
                currentTarget.HideUI();
                currentTarget = null;
            }
        }

    }
}