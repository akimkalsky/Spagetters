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
    private string shownPrompt;
    public float sightDistance = 20f;

    private Vector3 lastPosition;
    private float walkedDistance = 0f;
    private float stuckTimer;
    private Vector3 lastHitNormal;

    public float stepDistance = 1f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        lastPosition = transform.position;
    }

    void Update()
    {
        if (GameFlow.Instance != null && GameFlow.Instance.State != GameState.Explore)
            return;

        if (Keyboard.current == null)
            return;

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

        float turn = 0f;

        if (Keyboard.current.aKey.isPressed)
            turn = -1f;
        else if (Keyboard.current.dKey.isPressed)
            turn = 1f;

        transform.Rotate(Vector3.up * turn * turnSpeed * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        Vector3 motion = transform.forward * forward * moveSpeed;
        motion.y = velocity.y;

        Vector3 preMove = transform.position;
        controller.Move(motion * Time.deltaTime);

        Vector3 disp = transform.position - preMove;
        disp.y = 0f;
        if (Mathf.Abs(forward) > 0.01f && disp.magnitude < 0.01f && controller.isGrounded)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 0.3f)
            {
                Vector3 escape = lastHitNormal;
                escape.y = 0f;
                if (escape.sqrMagnitude > 0.001f)
                {
                    controller.Move(escape.normalized * moveSpeed * Time.deltaTime);
                }
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        walkedDistance += Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        while (walkedDistance >= stepDistance)
        {
            walkedDistance -= stepDistance;

            GameManager.Instance.UseStep();
        }

        Vector3 rayOrigin = transform.position + controller.center;
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, transform.forward, out hit, sightDistance))
        {
            Debug.DrawRay(rayOrigin, transform.forward * sightDistance, Color.red);

            Goon goon = hit.collider.GetComponent<Goon>();
            JobStation job = goon == null ? hit.collider.GetComponentInParent<JobStation>() : null;
            int stepsAway = Mathf.CeilToInt(hit.distance / stepDistance);

            if (goon != null)
            {
                if (currentTarget != null && currentTarget != goon)
                    currentTarget.HideUI();
                currentTarget = goon;

                int remaining = stepsAway - goon.duelDistance;
                goon.UpdateCountdown(remaining);
                SetPrompt(remaining <= 0 ? "◄  CLICK / E  TO DUEL  ►" : null);

                if (remaining <= 0 && Clicked())
                {
                    StartShowdown(goon);
                    return;
                }
            }
            else if (job != null)
            {
                ClearTarget();
                bool inRange = stepsAway <= job.interactDistance;
                SetPrompt(inRange ? $"◄  CLICK / E:  {job.title}  ►" : null);
                if (inRange && Clicked())
                {
                    SetPrompt(null);
                    GameFlow.Instance.StartMinigame(job.minigameKey, job);
                    return;
                }
            }
            else
            {
                SetPrompt(null);
                ClearTarget();
            }
        }
        else
        {
            Debug.DrawRay(rayOrigin, transform.forward * sightDistance, Color.green);
            SetPrompt(null);
            ClearTarget();
        }

    }

    void OnControllerColliderHit(ControllerColliderHit hit) => lastHitNormal = hit.normal;

    bool Clicked()
    {
        return (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);
    }

    void ClearTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.HideUI();
            currentTarget = null;
        }
    }

    void SetPrompt(string p)
    {
        if (p == shownPrompt)
        {
            return;
        }
        shownPrompt = p;
        GameEvents.RaisePrompt(p);
    }

    void StartShowdown(Goon goon)
    {
        SetPrompt(null);

        var rival = goon.GetRival();
        RivalRoster.Select(rival);

        var boss = RivalRoster.TopUndefeated();
        bool isBoss = boss != null && rival != null && boss.Index == rival.Index;

        var duel = DuelController.EnsureInstance();
        duel.playerActor = transform;
        duel.rivalActor = goon.transform;
        duel.paceStep = 0.3f;
        duel.playerAnim = anim;
        duel.rivalAnim = goon.GetComponentInChildren<SpriteAnimation>();
        duel.feintChance = isBoss ? 0.85f : 0.5f;
        duel.timeout = isBoss ? 1.0f : 1.2f;

        goon.HideUI();

        if (GameSettings.StoryMode && DuelIntroUI.Instance != null)
        {
            // Let DuelIntroUI freeze the state and manage dialogue advancement
            DuelIntroUI.Instance.Show(goon, duel);
        }
        else
        {
            // Arcade Mode: Start immediately
            if (GameFlow.Instance != null && GameFlow.Instance.BeginEncounter(goon))
            {
                duel.BeginDuel();
            }
        }
    }
}