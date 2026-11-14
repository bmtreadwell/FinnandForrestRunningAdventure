using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Scrolls the world toward the fixed player, spawns rows of trains, ramps, barriers and coins,
// checks collisions, and keeps score. All distances are in game units (see Cfg.Scale).
// REBUILT after the Nov 14 machine wipe: written fresh from the handoff rules, not restored verbatim.
public class GameManager : MonoBehaviour
{
    public enum Kind { Tie, Train, Ramp, Barrier, Overhead, Coin }

    class Item
    {
        public GameObject go;
        public Kind kind;
        public int lane;
        public float z;          // center; the player sits at z = 0
        public float len;
        public float halfWidth;
        public float y;          // center height
        public bool rampFed;     // a long train you can drive up onto
    }

    [Header("Scene links")]
    public PlayerController player;

    [Header("Speed (game units per second)")]
    public float startSpeed = 150f;
    public float maxSpeed = 330f;
    public float acceleration = 2.5f;

    public float Speed { get; private set; }
    public float Distance { get; private set; }
    public int Coins { get; private set; }
    public bool Dead { get; private set; }
    public int Score { get { return (int)(Distance * 0.1f) + Coins * 10; } }

    const float SpawnHorizon = 900f;
    const float RowSpacing = 160f;
    const float TieSpacing = 30f;
    const float CullBehind = -150f;
    const float PlayerHalfDepth = 8f;
    const float PlayerHalfWidth = 10f;
    const float RampLength = 120f;
    const float TopTrainHit = 59.5f;     // a normal jump (apex 49, sneakers 59) never clears a train
    const float StandHeight = 30f;

    readonly List<Item> items = new List<Item>();
    readonly Dictionary<Kind, Stack<GameObject>> pool = new Dictionary<Kind, Stack<GameObject>>();
    readonly Dictionary<Kind, Material> materials = new Dictionary<Kind, Material>();
    readonly float[] trainEnd = new float[Cfg.LaneCount];

    float nextRowZ;
    float nextTieZ;
    int safeLane = 1;
    float deadTimer;

    // Editor-only test driver: plays the game by itself.
    bool autoPilot;
    float autoCooldown;

    GUIStyle hudLeft, hudRight, centerBig, centerSmall;

    void Awake()
    {
        Application.targetFrameRate = 60;
        if (player == null) player = FindFirstObjectByType<PlayerController>();
#if UNITY_EDITOR
        autoPilot = UnityEditor.EditorPrefs.GetBool("FF_AutoPilot", false);
#endif
        ResetRun();
    }

    void ResetRun()
    {
        for (int i = 0; i < items.Count; i++) Release(items[i]);
        items.Clear();

        Speed = startSpeed;
        Distance = 0f;
        Coins = 0;
        Dead = false;
        deadTimer = 0f;
        nextRowZ = 500f;
        nextTieZ = -100f;
        for (int l = 0; l < trainEnd.Length; l++) trainEnd[l] = -9999f;
        safeLane = 1;
        if (player != null) player.ResetState();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (Dead)
        {
            deadTimer += dt;
            if (deadTimer > 0.4f && RestartPressed()) ResetRun();
            return;
        }

        Speed = Mathf.Min(maxSpeed, Speed + acceleration * dt);
        float step = Speed * dt;
        Distance += step;

        nextRowZ -= step;
        nextTieZ -= step;
        for (int l = 0; l < trainEnd.Length; l++) trainEnd[l] -= step;

        Spawn();
        Scroll(step);
        Cull();

        if (autoPilot) AutoPilot(dt);

        Collide();
    }

    static bool RestartPressed()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;
        Pointer p = Pointer.current;
        return p != null && p.press.wasPressedThisFrame;
    }

    // ---------- spawning ----------

    void Spawn()
    {
        while (nextTieZ < SpawnHorizon)
        {
            AddItem(Kind.Tie, 1, nextTieZ, 6f, Cfg.LaneWidth * 3.2f, 1f, 0.5f);
            nextTieZ += TieSpacing;
        }

        while (nextRowZ < SpawnHorizon)
        {
            SpawnRow(nextRowZ);
            nextRowZ += RowSpacing;
        }
    }

    void SpawnRow(float z)
    {
        // Drift the safe lane by at most one lane per row.
        safeLane = Mathf.Clamp(safeLane + Random.Range(-1, 2), 0, Cfg.LaneCount - 1);

        // Lanes that can take an obstacle: not the safe lane, and no train still running through.
        List<int> others = new List<int>();
        for (int l = 0; l < Cfg.LaneCount; l++)
            if (l != safeLane && trainEnd[l] < z - 20f) others.Add(l);

        float r = Random.value;
        int pattern = r < 0.20f ? 0 : r < 0.50f ? 1 : r < 0.68f ? 2 : r < 0.88f ? 3 : 4;

        if (pattern == 1 && others.Count > 0)
        {
            AddTrain(others[Random.Range(0, others.Count)], z);
        }
        else if (pattern == 2)
        {
            for (int i = 0; i < others.Count; i++) AddTrain(others[i], z);
        }
        else if (pattern == 3)
        {
            for (int i = 0; i < others.Count; i++)
                if (others.Count == 1 || Random.value < 0.7f)
                    AddItem(Kind.Barrier, others[i], z, 8f, 44f, 18f, 9f);
        }
        else if (pattern == 4 && others.Count > 0)
        {
            AddItem(Kind.Overhead, others[Random.Range(0, others.Count)], z, 8f, 48f, 46f, 47f);
        }

        for (int i = 0; i < 6; i++)
            AddItem(Kind.Coin, safeLane, z - 50f + i * 20f, 8f, 8f, 8f, 14f);
    }

    void AddTrain(int lane, float z)
    {
        bool lng = Random.value < 0.4f;
        float len = lng ? 380f : 220f;

        // A ramp only goes in if the stretch in front of the train is empty in that lane.
        float rampZ = z - len * 0.5f - RampLength * 0.5f;
        bool rampFed = lng && LaneClear(lane, rampZ - RampLength * 0.5f - 20f, z + len * 0.5f + 20f);

        Item t = AddItem(Kind.Train, lane, z, len, 40f, Cfg.TrainHeight, Cfg.TrainHeight * 0.5f);
        t.rampFed = rampFed;
        trainEnd[lane] = z + len * 0.5f;

        if (rampFed)
        {
            AddItem(Kind.Ramp, lane, rampZ, RampLength, 44f, Cfg.TrainHeight, Cfg.TrainHeight * 0.5f);

            // Coins along the roof.
            for (float cz = z - len * 0.5f + 30f; cz < z + len * 0.5f - 20f; cz += 30f)
                AddItem(Kind.Coin, lane, cz, 8f, 8f, 8f, Cfg.TrainHeight + 14f);
        }
    }

    bool LaneClear(int lane, float z0, float z1)
    {
        for (int i = 0; i < items.Count; i++)
        {
            Item it = items[i];
            if (it.lane != lane) continue;
            if (it.kind == Kind.Tie || it.kind == Kind.Coin) continue;
            if (it.z + it.len * 0.5f < z0 || it.z - it.len * 0.5f > z1) continue;
            return false;
        }
        return true;
    }

    Item AddItem(Kind kind, int lane, float z, float len, float width, float height, float yCenter)
    {
        Item it = new Item();
        it.kind = kind;
        it.lane = lane;
        it.z = z;
        it.len = len;
        it.halfWidth = width * 0.5f;
        it.y = yCenter;

        Stack<GameObject> stack;
        if (!pool.TryGetValue(kind, out stack)) { stack = new Stack<GameObject>(); pool[kind] = stack; }
        it.go = stack.Count > 0 ? stack.Pop() : Make(kind);

        if (kind == Kind.Ramp)
        {
            // A thin slab tilted up toward the train it leads onto.
            float slope = Mathf.Sqrt(len * len + height * height);
            float angle = Mathf.Atan2(height, len) * Mathf.Rad2Deg;
            it.go.transform.localScale = new Vector3(width, 4f, slope) * Cfg.Scale;
            it.go.transform.rotation = Quaternion.Euler(-angle, 0f, 0f);
        }
        else
        {
            it.go.transform.localScale = new Vector3(width, height, len) * Cfg.Scale;
            it.go.transform.rotation = Quaternion.identity;
        }

        Place(it);
        it.go.SetActive(true);
        items.Add(it);
        return it;
    }

    void Place(Item it)
    {
        float x = it.kind == Kind.Tie ? 0f : Cfg.LaneX(it.lane);
        it.go.transform.position = new Vector3(x, it.y, it.z) * Cfg.Scale;
    }

    void Release(Item it)
    {
        it.go.SetActive(false);
        pool[it.kind].Push(it.go);
    }

    GameObject Make(Kind kind)
    {
        PrimitiveType type = kind == Kind.Coin ? PrimitiveType.Sphere : PrimitiveType.Cube;
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = kind.ToString();
        go.transform.SetParent(transform, false);

        Collider col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        go.GetComponent<Renderer>().sharedMaterial = MaterialFor(kind);
        return go;
    }

    Material MaterialFor(Kind kind)
    {
        Material m;
        if (materials.TryGetValue(kind, out m)) return m;

        Color c;
        switch (kind)
        {
            case Kind.Tie: c = new Color(0.30f, 0.26f, 0.22f); break;
            case Kind.Train: c = new Color(0.45f, 0.52f, 0.60f); break;
            case Kind.Ramp: c = new Color(0.55f, 0.55f, 0.55f); break;
            case Kind.Barrier: c = new Color(0.95f, 0.55f, 0.15f); break;
            case Kind.Overhead: c = new Color(0.80f, 0.20f, 0.20f); break;
            default: c = new Color(1.00f, 0.85f, 0.15f); break;
        }

        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        materials[kind] = m;
        return m;
    }

    // ---------- world motion ----------

    void Scroll(float step)
    {
        for (int i = 0; i < items.Count; i++)
        {
            Item it = items[i];
            it.z -= step;
            Place(it);
        }
    }

    void Cull()
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            Item it = items[i];
            if (it.z + it.len * 0.5f < CullBehind)
            {
                Release(it);
                items.RemoveAt(i);
            }
        }
    }

    // ---------- collisions ----------

    float RampHeightAt(Item ramp)
    {
        float t = Mathf.Clamp01((0f - (ramp.z - ramp.len * 0.5f)) / ramp.len);
        return t * Cfg.TrainHeight;
    }

    bool OnRamp(int lane, float px)
    {
        for (int i = 0; i < items.Count; i++)
        {
            Item it = items[i];
            if (it.kind != Kind.Ramp || it.lane != lane) continue;
            if (Mathf.Abs(it.z) < it.len * 0.5f + PlayerHalfDepth) return true;
        }
        return false;
    }

    void Collide()
    {
        if (player == null) return;

        Vector3 pp = player.transform.localPosition / Cfg.Scale;
        float px = pp.x;
        float py = pp.y;

        float ground = 0f;
        bool crash = false;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            Item it = items[i];
            if (it.kind == Kind.Tie) continue;

            float itemX = Cfg.LaneX(it.lane);
            if (Mathf.Abs(itemX - px) > it.halfWidth + PlayerHalfWidth) continue;

            if (it.kind == Kind.Coin)
            {
                if (Mathf.Abs(it.z) < 12f && Mathf.Abs(it.y - (py + StandHeight * 0.5f)) < 22f)
                {
                    Coins++;
                    Release(it);
                    items.RemoveAt(i);
                }
                continue;
            }

            float near = it.z - it.len * 0.5f - PlayerHalfDepth;
            float far = it.z + it.len * 0.5f + PlayerHalfDepth;
            if (far < 0f || near > 0f) continue;

            switch (it.kind)
            {
                case Kind.Ramp:
                {
                    float h = RampHeightAt(it);
                    if (py + 6f >= h) ground = Mathf.Max(ground, h);
                    else crash = true;
                    break;
                }
                case Kind.Train:
                {
                    // Land on the roof only from a ramp or by already being up there; otherwise
                    // the front or side of the train ends the run. The back end is safe.
                    if (py >= TopTrainHit) ground = Mathf.Max(ground, Cfg.TrainHeight);
                    else if (!(it.rampFed && OnRamp(it.lane, px))) crash = true;
                    break;
                }
                case Kind.Barrier:
                    if (py < 17.5f) crash = true;   // barrier is 18 high; a normal jump clears it
                    break;
                case Kind.Overhead:
                    if (!player.IsSliding && py < 70f) crash = true;
                    break;
            }
        }

        if (crash) { Die(); return; }
        player.GroundY = ground;
    }

    void Die()
    {
        Dead = true;
        deadTimer = 0f;
        Speed = 0f;
    }

    // ---------- editor autopilot ----------

    void AutoPilot(float dt)
    {
        if (player == null || Dead) return;
        autoCooldown -= dt;

        int cur = player.Lane;
        float[] clear = new float[Cfg.LaneCount];
        for (int l = 0; l < clear.Length; l++) clear[l] = 2000f;

        float hazardAhead = 2000f;
        Kind hazardKind = Kind.Coin;

        for (int i = 0; i < items.Count; i++)
        {
            Item it = items[i];
            bool hazard = it.kind == Kind.Barrier || it.kind == Kind.Overhead || (it.kind == Kind.Train && !it.rampFed);
            if (!hazard) continue;
            float dist = it.z - it.len * 0.5f;
            if (it.z + it.len * 0.5f < -PlayerHalfDepth) continue;
            if (dist < 0f) dist = 0f;
            if (dist < clear[it.lane]) clear[it.lane] = dist;
            if (it.lane == cur && dist < hazardAhead && (it.kind != Kind.Train)) { hazardAhead = dist; hazardKind = it.kind; }
        }

        // A train in the lane means move; barriers and bars mean jump or slide.
        int best = cur;
        for (int l = 0; l < clear.Length; l++)
            if (clear[l] > clear[best] + 40f) best = l;

        if (clear[cur] < 300f && best != cur && autoCooldown <= 0f)
        {
            player.MoveLane(best > cur ? 1 : -1);
            autoCooldown = 0.25f;
        }

        if (hazardAhead < 70f)
        {
            if (hazardKind == Kind.Barrier) player.Jump();
            else if (hazardKind == Kind.Overhead) player.Slide();
        }
    }

    // ---------- HUD ----------

    void DrawText(Rect r, string s, GUIStyle st)
    {
        Color c = st.normal.textColor;
        st.normal.textColor = Color.black;
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), s, st);
        st.normal.textColor = c;
        GUI.Label(r, s, st);
    }

    void OnGUI()
    {
        if (hudLeft == null)
        {
            hudLeft = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontStyle = FontStyle.Bold };
            hudRight = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontStyle = FontStyle.Bold };
            centerBig = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            centerSmall = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            hudLeft.normal.textColor = hudRight.normal.textColor = centerBig.normal.textColor = centerSmall.normal.textColor = Color.white;
        }

        float w = Screen.width;
        float h = Screen.height;
        hudLeft.fontSize = hudRight.fontSize = Mathf.RoundToInt(h / 22f);
        centerBig.fontSize = Mathf.RoundToInt(h / 11f);
        centerSmall.fontSize = Mathf.RoundToInt(h / 26f);

        DrawText(new Rect(24, 16, w * 0.5f, h * 0.1f), "Score " + Score, hudLeft);
        DrawText(new Rect(w * 0.5f - 24, 16, w * 0.5f, h * 0.1f), "Coins " + Coins, hudRight);
        if (autoPilot) DrawText(new Rect(0, h * 0.12f, w, h * 0.08f), "AUTOPILOT", centerSmall);

        if (Dead)
        {
            DrawText(new Rect(0, h * 0.30f, w, h * 0.15f), "GAME OVER", centerBig);
            DrawText(new Rect(0, h * 0.45f, w, h * 0.08f), "Score " + Score + "   Coins " + Coins, centerSmall);
            DrawText(new Rect(0, h * 0.53f, w, h * 0.08f), "Press Space or tap to run again", centerSmall);
        }
    }
}
