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

    }
}