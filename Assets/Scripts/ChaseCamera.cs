using UnityEngine;

// Keeps the camera's Inspector position and field of view as the base look, then adds a little
// lateral follow, a rise when the player climbs a train, and a field-of-view push with speed.
public class ChaseCamera : MonoBehaviour
{
    public GameManager game;
    public PlayerController player;
    public float fovBoost = 12f;
    public float lateralFollow = 0.5f;
    public float heightFollow = 0.8f;

    Camera cam;
    float baseFov;
    float baseX;
    float baseY;

    void Awake()
    {
        cam = GetComponent<Camera>();
        baseFov = cam.fieldOfView;
        baseX = transform.position.x;
        baseY = transform.position.y;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;

        if (game != null)
        {
            float t = Mathf.InverseLerp(game.startSpeed, game.maxSpeed, game.Speed);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, baseFov + fovBoost * t, 4f * dt);
        }

        if (player != null)
        {
            Vector3 p = transform.position;
            p.x = Mathf.Lerp(p.x, baseX + player.transform.position.x * lateralFollow, 8f * dt);
            p.y = Mathf.Lerp(p.y, baseY + player.GroundY * Cfg.Scale * heightFollow, 5f * dt);
            transform.position = p;
        }
    }
}
