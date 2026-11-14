using UnityEngine;
using UnityEngine.InputSystem;

// Fixed-position runner: three lanes, jump, slide, fast fall.
// The player never moves forward; the world scrolls toward it.
public class PlayerController : MonoBehaviour
{
    [Header("Child object that holds the model")]
    public Transform model;

    [Header("Debug")]
    public bool sneakers = false;

    public int Lane { get; private set; } = 1;
    public bool IsGrounded { get; private set; } = true;
    public bool IsSliding { get; private set; }
    public float Height { get { return y - GroundY; } }

    // The collision code raises this when the player stands on a train or ramp.
    public float GroundY { get; set; } = 0f;

    float x;              // game units
    float y;              // game units
    float vy;
    float slideTimer;
    bool slideOnLand;

    Vector3 modelBaseScale = Vector3.one;
    Vector3 modelBasePos = Vector3.zero;

    Vector2 swipeStart;
    bool swiping;
    bool swipeFired;

    void Awake()
    {
        x = Cfg.LaneX(Lane);
        y = GroundY;

        if (model != null)
        {
            modelBaseScale = model.localScale;
            modelBasePos = model.localPosition;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        ReadKeyboard();
        ReadSwipe();

        StepLane(dt);
        StepVertical(dt);
        StepSlide(dt);

        Apply();
    }

    // Called by GameManager when a new run starts.
    public void ResetState()
    {
        Lane = 1;
        x = Cfg.LaneX(Lane);
        GroundY = 0f;
        y = 0f;
        vy = 0f;
        IsGrounded = true;
        slideOnLand = false;
        EndSlide();
    }

    // ---------- input ----------

    void ReadKeyboard()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) MoveLane(-1);
        if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) MoveLane(1);

        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Jump();
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) Slide();
    }

    void ReadSwipe()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null) return;

        if (pointer.press.wasPressedThisFrame)
        {
            swipeStart = pointer.position.ReadValue();
            swiping = true;
            swipeFired = false;
        }
        else if (pointer.press.wasReleasedThisFrame)
        {
            swiping = false;
        }

        if (!swiping || swipeFired) return;

        Vector2 delta = pointer.position.ReadValue() - swipeStart;
        float threshold = Screen.height * Cfg.SwipeThresholdScreenFraction;
        if (delta.magnitude < threshold) return;

        swipeFired = true;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            MoveLane(delta.x > 0f ? 1 : -1);
        else if (delta.y > 0f)
            Jump();
        else
            Slide();
    }

    // ---------- actions ----------

    public void MoveLane(int dir)
    {
        Lane = Mathf.Clamp(Lane + dir, 0, Cfg.LaneCount - 1);
    }

    public void Jump()
    {
        if (!IsGrounded) return;

        vy = Cfg.JumpVelocity * (sneakers ? Cfg.SneakerBoost : 1f);
        IsGrounded = false;
        EndSlide();
    }

    public void Slide()
    {
        if (!IsGrounded)
        {
            // Swipe down in the air: fast fall, then slide on landing.
            vy = Cfg.FastFallVelocity;
            slideOnLand = true;
            return;
        }

        IsSliding = true;
        slideTimer = Cfg.SlideTime;
    }

    void EndSlide()
    {
        IsSliding = false;
        slideTimer = 0f;
    }

    // ---------- motion ----------

    void StepLane(float dt)
    {
        float target = Cfg.LaneX(Lane);
        x = Mathf.MoveTowards(x, target, Cfg.LaneSwitchSpeed * dt);
    }

    void StepVertical(float dt)
    {
        if (IsGrounded)
        {
            if (y <= GroundY + 0.01f)
            {
                y = GroundY;
                return;
            }

            // The ground dropped away (stepped off a train): start falling.
            IsGrounded = false;
            vy = 0f;
        }

        vy -= Cfg.Gravity * dt;
        y += vy * dt;

        if (y <= GroundY)
        {
            y = GroundY;
            vy = 0f;
            IsGrounded = true;

            if (slideOnLand)
            {
                slideOnLand = false;
                Slide();
            }
        }
    }

    void StepSlide(float dt)
    {
        if (!IsSliding) return;

        slideTimer -= dt;
        if (slideTimer <= 0f) EndSlide();
    }

    void Apply()
    {
        transform.localPosition = new Vector3(x, y, 0f) * Cfg.Scale;

        if (model == null) return;

        // Placeholder slide: squash the capsule. Replaced by a real animation later.
        float squash = IsSliding ? 0.5f : 1f;
        model.localScale = new Vector3(modelBaseScale.x, modelBaseScale.y * squash, modelBaseScale.z);
        model.localPosition = new Vector3(modelBasePos.x, modelBasePos.y * squash, modelBasePos.z);
    }
}
