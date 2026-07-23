using UnityEngine;

public class SpriteAnimation : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    [Header("Animations")]
    public Sprite[] idleFront;
    public Sprite[] walkFront;

    public Sprite[] idleBack;
    public Sprite[] walkBack;

    public Sprite[] action;

    public float fps = 10f;

    Sprite[] currentAnim;
    int frame;
    float timer;

    public enum Facing
    {
        Front,
        Back
    }

    Facing facing = Facing.Front;

    void Start()
    {
        PlayIdle();
    }

    void Update()
    {
        if (currentAnim == null || currentAnim.Length == 0)
            return;

        timer += Time.deltaTime;

        if (timer >= 1f / fps)
        {
            timer = 0f;
            frame = (frame + 1) % currentAnim.Length;
            spriteRenderer.sprite = currentAnim[frame];
        }
    }

    void SetAnimation(Sprite[] anim)
    {
        if (currentAnim == anim)
            return;

        currentAnim = anim;
        frame = 0;
        timer = 0;

        if (currentAnim.Length > 0)
            spriteRenderer.sprite = currentAnim[0];
    }

    public void WalkForward()
    {
        facing = Facing.Back;
        SetAnimation(walkBack);
    }

    public void WalkBackward()
    {
        facing = Facing.Front;
        SetAnimation(walkFront);
    }

    public void PlayIdle()
    {
        if (facing == Facing.Front)
            SetAnimation(idleFront);
        else
            SetAnimation(idleBack);
    }

    public void PlayAction()
    {
        SetAnimation(action);
    }
}