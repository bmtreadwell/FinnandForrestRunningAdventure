using System.Collections.Generic;
using UnityEngine;

// Downtown Manhattan, open-air subway. The track sits in a cut; sidewalks with iron railings run along
// both sides at street level (3 m up), then a street with parked cars, a far sidewalk, and buildings.
// Everything is purely visual (no colliders), pooled in 20 m slots on each side, and scrolls with the
// world by following GameManager.Distance. Units here are meters (game units x Cfg.Scale).
public class EnvironmentSpawner : MonoBehaviour
{
    public GameManager game;

    const float SlotLen = 20f;
    const int SlotCount = 16;
    const float FirstZ = -30f;
    const float RecycleZ = -40f;
    const int VariantsPerSide = 20;

    const float G = 3f;        // street level above the track
    const float FaceX = 31f;   // where building facades start, measured from the track center

    static readonly string[] Stations =
    {
        "DELANCEY ST", "ESSEX ST", "GRAND ST", "CANAL ST", "SPRING ST", "PRINCE ST", "BROADWAY-LAFAYETTE",
        "BLEECKER ST", "ASTOR PL", "14 ST-UNION SQ", "23 ST", "34 ST-HERALD SQ", "TIMES SQ-42 ST",
        "GRAND CENTRAL-42 ST", "FULTON ST", "WALL ST", "CHAMBERS ST", "CITY HALL", "BOWERY", "2 AV"
    };
    static readonly string[] Shops =
    {
        "CORNER DELI", "SPRING CAFE", "BODEGA", "LAUNDRY", "PIZZA", "RECORDS", "TAILOR", "NOODLES", "BAKERY", "BOOKS"
    };

    static readonly Color[] Brick =
    {
        new Color(0.62f, 0.30f, 0.22f), new Color(0.55f, 0.27f, 0.21f), new Color(0.70f, 0.36f, 0.26f),
        new Color(0.48f, 0.30f, 0.25f), new Color(0.76f, 0.62f, 0.45f), new Color(0.45f, 0.50f, 0.58f),
        new Color(0.80f, 0.74f, 0.62f)
    };
    static readonly Color[] Brown =
    {
        new Color(0.45f, 0.30f, 0.24f), new Color(0.55f, 0.36f, 0.28f), new Color(0.50f, 0.33f, 0.27f)
    };
    static readonly Color[] Awnings =
    {
        new Color(0.15f, 0.45f, 0.30f), new Color(0.70f, 0.15f, 0.15f), new Color(0.15f, 0.30f, 0.55f),
        new Color(0.90f, 0.85f, 0.65f), new Color(0.90f, 0.50f, 0.15f)
    };
    static readonly Color[] CarColors =
    {
        new Color(0.96f, 0.78f, 0.10f), new Color(0.96f, 0.78f, 0.10f), new Color(0.96f, 0.78f, 0.10f),
        new Color(0.96f, 0.78f, 0.10f), new Color(0.14f, 0.15f, 0.18f), new Color(0.90f, 0.90f, 0.90f),
        new Color(0.45f, 0.48f, 0.52f), new Color(0.18f, 0.25f, 0.45f)
    };
    static readonly Color[] Leaves =
    {
        new Color(0.30f, 0.55f, 0.28f), new Color(0.38f, 0.62f, 0.30f), new Color(0.25f, 0.48f, 0.30f),
        new Color(0.85f, 0.55f, 0.15f), new Color(0.75f, 0.30f, 0.15f), new Color(0.90f, 0.72f, 0.20f)
    };

    class Slot
    {
        public Transform t;
        public Transform variant;
        public int side;
        public int index;
        public float z;
    }

    readonly Dictionary<int, Material> mats = new Dictionary<int, Material>();
    readonly List<Transform>[] pool = { new List<Transform>(), new List<Transform>() };
    readonly List<Slot> slots = new List<Slot>();
    System.Random r;
    int sd = 1;
    float metroZ = 999f;
    float crossZ = 999f;
    Font font;
    float lastDistance;

    // ---------------------------------------------------------------- lifecycle

    void Start()
    {
        if (game == null) game = FindFirstObjectByType<GameManager>();
        r = new System.Random(20261114);

        SetupSky();
        BuildStatic();

        for (int s = 0; s < 2; s++)
        {
            sd = s == 0 ? -1 : 1;
            for (int i = 0; i < VariantsPerSide; i++)
            {
                Transform v = BuildVariant(i);
                v.gameObject.SetActive(false);
                pool[s].Add(v);
            }
        }

        for (int s = 0; s < 2; s++)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                GameObject go = new GameObject("Slot" + (s == 0 ? "L" : "R") + i);
                go.transform.SetParent(transform, false);
                Slot sl = new Slot { t = go.transform, side = s, index = i, z = FirstZ + i * SlotLen };
                Assign(sl);
                sl.t.position = new Vector3(0f, 0f, sl.z);
                slots.Add(sl);
            }
        }

        lastDistance = game != null ? game.Distance : 0f;
    }

    void Update()
    {
        if (game == null || slots.Count == 0) return;

        float d = game.Distance;
        if (d < lastDistance - 1f)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].z = FirstZ + slots[i].index * SlotLen;
                slots[i].t.position = new Vector3(0f, 0f, slots[i].z);
            }
            lastDistance = d;
            return;
        }

        float dz = (d - lastDistance) * Cfg.Scale;
        lastDistance = d;

        for (int i = 0; i < slots.Count; i++)
        {
            Slot sl = slots[i];
            sl.z -= dz;
            if (sl.z < RecycleZ)
            {
                sl.z += SlotCount * SlotLen;
                Assign(sl);
            }
            sl.t.position = new Vector3(0f, 0f, sl.z);
        }
    }

    void Assign(Slot sl)
    {
        List<Transform> free = pool[sl.side];
        if (sl.variant != null)
        {
            sl.variant.gameObject.SetActive(false);
            free.Add(sl.variant);
        }
        int k = r.Next(free.Count);
        Transform v = free[k];
        free.RemoveAt(k);
        v.SetParent(sl.t, false);
        v.localPosition = Vector3.zero;
        v.gameObject.SetActive(true);
        sl.variant = v;
    }

    void SetupSky()
    {
        Color sky = new Color(0.78f, 0.83f, 0.88f);
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = sky;
            if (cam.farClipPlane < 700f) cam.farClipPlane = 700f;
        }
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = sky;
        RenderSettings.fogStartDistance = 120f;
        RenderSettings.fogEndDistance = 300f;
    }

    // ---------------------------------------------------------------- static scenery

    void BuildStatic()
    {
        sd = 1;
        GameObject go = new GameObject("Static");
        go.transform.SetParent(transform, false);
        Transform s = go.transform;

        // wide ground under everything
        B(s, 0f, -0.55f, 150f, 420f, 1f, 700f, new Color(0.36f, 0.35f, 0.33f));
        // track extension past the end of the GameManager slab
        B(s, 0f, -0.5f, 250f, 18f, 1f, 140f, new Color(0.42f, 0.38f, 0.34f));

        // rails
        Color steel = new Color(0.55f, 0.57f, 0.60f);
        float[] lanes = { -5.2f, 0f, 5.2f };
        for (int i = 0; i < lanes.Length; i++)
        {
            B(s, lanes[i] - 0.75f, 0.07f, 150f, 0.12f, 0.14f, 350f, steel);
            B(s, lanes[i] + 0.75f, 0.07f, 150f, 0.12f, 0.14f, 350f, steel);
        }

        // skyline wall at the end of the street
        for (int i = 0; i < 26; i++)
        {
            float h = 40f + r.Next(0, 90);
            float w = 12f + r.Next(0, 10);
            float x = -190f + i * 15f + r.Next(-4, 5);
            float tone = 0.62f + r.Next(0, 12) * 0.01f;
            B(s, x, h / 2f - 1f, 310f + r.Next(0, 25), w, h, w, new Color(tone - 0.05f, tone, tone + 0.07f));
        }
        // Empire State style spire
        Color esb = new Color(0.60f, 0.65f, 0.72f);
        B(s, 30f, 70f, 335f, 22f, 140f, 22f, esb);
        B(s, 30f, 160f, 335f, 15f, 40f, 15f, esb);
        B(s, 30f, 190f, 335f, 9f, 30f, 9f, esb);
        B(s, 30f, 215f, 335f, 1.5f, 40f, 1.5f, esb);

        // far side towers, fixed, fading into the haze
        for (int side = -1; side <= 1; side += 2)
        {
            for (int z = -20; z < 320; z += 30)
            {
                float h = 45f + r.Next(0, 70);
                float tone = 0.60f + r.Next(0, 10) * 0.01f;
                B(s, side * (100f + r.Next(0, 12)), h / 2f - 1f, z + r.Next(-5, 6), 22f, h, 18f,
                    new Color(tone - 0.04f, tone, tone + 0.07f));
            }
        }
    }

    // ---------------------------------------------------------------- variants

    Transform BuildVariant(int i)
    {
        GameObject go = new GameObject("Variant" + i + (sd > 0 ? "R" : "L"));
        go.transform.SetParent(transform, false);
        Transform v = go.transform;
        metroZ = 999f;
        crossZ = 999f;

        Ground(v);

        if (i < 3) Metro(v);
        Crosswalk(v, i);

        if (i == 0 || i == 2 || (i >= 3 && i <= 8)) Tenement(v);
        else if (i == 1 || (i >= 9 && i <= 11)) Brownstone(v);
        else if (i == 12) Warehouse(v);
        else if (i == 13 || i == 14) Tower(v);
        else if (i == 15 || i == 16) Park(v);
        else if (i == 17 || i == 18) Plaza(v);
        else Construction(v);

        Dressing(v);
        return v;
    }

    void Ground(Transform v)
    {
        Color wall = new Color(0.50f, 0.45f, 0.42f);
        Color conc = new Color(0.66f, 0.64f, 0.60f);
        Color road = new Color(0.26f, 0.27f, 0.30f);
        Color yellow = new Color(0.90f, 0.75f, 0.20f);
        Color iron = new Color(0.14f, 0.16f, 0.15f);
        float len = SlotLen + 0.04f;

        B(v, 9.3f, 1.5f, 0f, 0.6f, 3f, len, wall);                    // retaining wall
        B(v, 9.5f, 3.0f, 0f, 1.0f, 0.14f, len, Sh(conc, 1.1f));        // coping
        B(v, 13.3f, 2.9f, 0f, 7.4f, 0.2f, len, conc);                  // near sidewalk
        B(v, 21.5f, 2.78f, 0f, 9f, 0.2f, len, road);                   // street
        B(v, 28.5f, 2.9f, 0f, 5f, 0.2f, len, conc);                    // far sidewalk
        B(v, 21.4f, 2.89f, 0f, 0.1f, 0.02f, len, yellow);              // double yellow
        B(v, 21.6f, 2.89f, 0f, 0.1f, 0.02f, len, yellow);
        B(v, 8.97f, 2.2f, 0f, 0.06f, 0.35f, len, new Color(0.18f, 0.35f, 0.60f)); // tile band on the wall

        // iron railing along the edge
        for (int k = 0; k < 9; k++) B(v, 9.8f, 3.5f, -9f + k * 2.25f, 0.08f, 1f, 0.08f, iron);
        B(v, 9.8f, 4.0f, 0f, 0.08f, 0.08f, len, iron);
        B(v, 9.8f, 3.4f, 0f, 0.06f, 0.06f, len, iron);

        // overhead wire gantry, one per slot
        Color beam = new Color(0.30f, 0.34f, 0.45f);
        B(v, 9.8f, 7.25f, 0f, 0.35f, 8.5f, 0.35f, beam);
        if (sd > 0) B(v, 0f, 11.5f, 0f, 19.6f, 0.3f, 0.3f, beam);
        B(v, 3.5f, 10.4f, 0f, 0.05f, 0.05f, len, iron);
        B(v, 3.5f, 10.95f, 0f, 0.04f, 1.1f, 0.04f, iron);
    }

    void Crosswalk(Transform v, int i)
    {
        if (i % 4 != 1) return;
        crossZ = r.Next(-5, 6);
        Color white = new Color(0.92f, 0.92f, 0.90f);
        for (int k = 0; k < 8; k++) B(v, 17.8f + k * 1.0f, 2.895f, crossZ, 0.5f, 0.02f, 3f, white);
    }

    void Dressing(Transform v)
    {
        // street trees on the near sidewalk
        float[] zs = { -7f, -2.5f, 2.5f, 7f };
        for (int k = 0; k < zs.Length; k++)
        {
            if (r.Next(0, 100) > 38) continue;
            if (Mathf.Abs(zs[k] - metroZ) < 3.8f) continue;
            float z = zs[k];
            Color leaf = Pick(Leaves);
            Cy(v, 16.3f, G + 1.4f, z, 0.35f, 2.8f, new Color(0.35f, 0.26f, 0.20f));
            Sp(v, 16.3f, G + 4.2f, z, 3.2f, leaf);
            Sp(v, 16.3f, G + 5.1f, z + 0.5f, 2.2f, Sh(leaf, 1.12f));
        }

        // lamp and hydrant
        if (r.Next(0, 100) < 65)
        {
            float z = r.Next(-8, 9);
            if (Mathf.Abs(z - metroZ) > 3.8f)
            {
                Cy(v, 16.7f, G + 2.75f, z, 0.14f, 5.5f, new Color(0.15f, 0.17f, 0.16f));
                B(v, 16.1f, G + 5.4f, z, 1.2f, 0.1f, 0.1f, new Color(0.15f, 0.17f, 0.16f));
                B(v, 15.5f, G + 5.3f, z, 0.5f, 0.2f, 0.4f, new Color(0.98f, 0.92f, 0.70f));
            }
        }
        if (r.Next(0, 100) < 40)
        {
            float z = r.Next(-9, 10);
            Cy(v, 16.6f, G + 0.37f, z, 0.35f, 0.75f, new Color(0.75f, 0.15f, 0.12f));
            Cy(v, 16.6f, G + 0.8f, z, 0.25f, 0.12f, new Color(0.65f, 0.12f, 0.10f));
        }

        // curb lip and slab joints
        B(v, 17.0f, G + 0.04f, 0f, 0.3f, 0.1f, 20f, new Color(0.64f, 0.64f, 0.62f));
        for (float jz = -8.75f; jz < 10f; jz += 2.5f)
            B(v, 13.3f, G + 0.005f, jz, 7.2f, 0.01f, 0.05f, new Color(0.52f, 0.52f, 0.50f));

        // sidewalk furniture on free spots
        float[] spots = { -8f, -4.5f, 4.5f, 8f };
        for (int k = 0; k < spots.Length; k++)
        {
            float z = spots[k];
            if (Mathf.Abs(z - metroZ) < 4.2f) continue;
            int roll = r.Next(0, 100);
            if (roll < 22) Bench(v, 14.2f, z);
            else if (roll < 34) Newsstand(v, 12.4f, z);
            else if (roll < 44) HotDogCart(v, 14.0f, z);
            else if (roll < 52) TrashCan(v, 15.2f, z);
            else if (roll < 58) Mailbox(v, 15.6f, z);
        }
        if (r.Next(0, 100) < 12 && Mathf.Abs(metroZ) > 5f) SidewalkShed(v, 6.5f);

        // parked cars
        float[] cz = { -6.5f, 0f, 6.5f };
        for (int k = 0; k < cz.Length; k++)
        {
            if (r.Next(0, 100) > 50) continue;
            if (Mathf.Abs(cz[k] - crossZ) < 3.6f) continue;
            if (r.Next(0, 100) < 25) Van(v, 18.7f, cz[k]);
            else Car(v, 18.7f, cz[k], Pick(CarColors));
        }
    }

    void Bench(Transform v, float x, float z)
    {
        Color wood = new Color(0.45f, 0.30f, 0.18f);
        Color iron = new Color(0.10f, 0.12f, 0.12f);
        B(v, x, G + 0.5f, z, 0.6f, 0.08f, 1.8f, wood);
        B(v, x + 0.3f, G + 0.9f, z, 0.06f, 0.5f, 1.8f, wood);
        B(v, x, G + 0.25f, z - 0.75f, 0.5f, 0.5f, 0.08f, iron);
        B(v, x, G + 0.25f, z + 0.75f, 0.5f, 0.5f, 0.08f, iron);
    }

    void Newsstand(Transform v, float x, float z)
    {
        Color body = new Color(0.14f, 0.26f, 0.22f);
        B(v, x, G + 1.1f, z, 1.2f, 2.2f, 2.0f, body);
        B(v, x, G + 2.28f, z, 1.5f, 0.12f, 2.3f, new Color(0.08f, 0.16f, 0.13f));
        Color[] mag = { new Color(0.80f, 0.28f, 0.25f), new Color(0.95f, 0.80f, 0.25f), new Color(0.30f, 0.50f, 0.80f), new Color(0.92f, 0.92f, 0.88f) };
        for (int k = 0; k < 4; k++) B(v, x + 0.62f, G + 1.5f, z - 0.75f + k * 0.5f, 0.04f, 0.6f, 0.4f, mag[k]);
        B(v, x + 0.62f, G + 0.8f, z, 0.04f, 0.5f, 1.8f, new Color(0.25f, 0.22f, 0.20f));
    }

    void HotDogCart(Transform v, float x, float z)
    {
        Color steel = new Color(0.72f, 0.74f, 0.76f);
        B(v, x, G + 0.6f, z, 0.9f, 0.7f, 1.4f, steel);
        B(v, x, G + 1.0f, z, 0.95f, 0.08f, 1.5f, new Color(0.55f, 0.57f, 0.60f));
        Cy(v, x - 0.3f, G + 0.15f, z - 0.55f, 0.3f, 0.3f, new Color(0.08f, 0.08f, 0.09f));
        Cy(v, x - 0.3f, G + 0.15f, z + 0.55f, 0.3f, 0.3f, new Color(0.08f, 0.08f, 0.09f));
        Cy(v, x, G + 1.9f, z, 0.05f, 1.8f, new Color(0.30f, 0.30f, 0.32f));
        Color um = r.Next(0, 2) == 0 ? new Color(0.95f, 0.78f, 0.15f) : new Color(0.25f, 0.45f, 0.80f);
        Prim(PrimitiveType.Cylinder, v, x, G + 2.8f, z, 2.3f, 0.06f, 2.3f, um, 0f);
        Prim(PrimitiveType.Cylinder, v, x, G + 2.65f, z, 1.5f, 0.06f, 1.5f, Sh(um, 0.9f), 0f);
    }

    void TrashCan(Transform v, float x, float z)
    {
        Cy(v, x, G + 0.45f, z, 0.5f, 0.9f, new Color(0.12f, 0.20f, 0.17f));
        Cy(v, x, G + 0.93f, z, 0.55f, 0.08f, new Color(0.08f, 0.14f, 0.12f));
    }

    void Mailbox(Transform v, float x, float z)
    {
        B(v, x, G + 0.55f, z, 0.5f, 1.1f, 0.5f, new Color(0.10f, 0.25f, 0.60f));
        B(v, x, G + 1.15f, z, 0.52f, 0.12f, 0.52f, new Color(0.08f, 0.20f, 0.50f));
    }

    void SidewalkShed(Transform v, float z)
    {
        Color g = new Color(0.10f, 0.32f, 0.22f);
        B(v, 13.3f, G + 2.7f, z, 6.6f, 0.15f, 5.0f, g);
        for (int a = -1; a <= 1; a += 2)
            for (int b = -1; b <= 1; b += 2)
                B(v, 13.3f + a * 3.1f, G + 1.3f, z + b * 2.3f, 0.15f, 2.6f, 0.15f, g);
        B(v, 13.3f, G + 2.3f, z - 2.45f, 6.4f, 0.08f, 0.06f, new Color(0.95f, 0.78f, 0.1f));
    }

    void Car(Transform v, float x, float z, Color c)
    {
        float y0 = 2.88f;
        Color tire = new Color(0.07f, 0.07f, 0.08f);
        for (int a = -1; a <= 1; a += 2)
            for (int b = -1; b <= 1; b += 2)
                B(v, x + a * 0.95f * sd * sd, y0 + 0.3f, z + b * 1.4f, 0.25f, 0.6f, 0.6f, tire);
        B(v, x, y0 + 0.65f, z, 1.9f, 0.7f, 4.4f, c);
        B(v, x, y0 + 1.3f, z - 0.2f, 1.7f, 0.6f, 2.4f, c);
        B(v, x, y0 + 1.3f, z - 0.2f, 1.74f, 0.3f, 2.0f, new Color(0.15f, 0.2f, 0.26f));
    }

    void Van(Transform v, float x, float z)
    {
        float y0 = 2.88f;
        Color tire = new Color(0.07f, 0.07f, 0.08f);
        Color c = r.Next(0, 2) == 0 ? new Color(0.92f, 0.92f, 0.90f) : new Color(0.50f, 0.36f, 0.24f);
        for (int a = -1; a <= 1; a += 2)
            for (int b = -1; b <= 1; b += 2)
                B(v, x + a * 1.0f * sd * sd, y0 + 0.3f, z + b * 1.6f, 0.25f, 0.6f, 0.6f, tire);
        B(v, x, y0 + 1.4f, z, 2.0f, 1.8f, 4.8f, c);
        B(v, x, y0 + 1.7f, z + 2.0f, 1.96f, 0.6f, 0.9f, new Color(0.15f, 0.2f, 0.26f));
    }

    // ---------------------------------------------------------------- subway entrance

    static readonly string[][] Entrances =
    {
        new[]{"Times Sq-42 St","1237"}, new[]{"14 St-Union Sq","456N"}, new[]{"34 St-Herald Sq","BDFN"},
        new[]{"Grand Central-42 St","4567"}, new[]{"Canal St","JNQ6"}, new[]{"Fulton St","2345"},
        new[]{"W 4 St-Wash Sq","ABCD"}, new[]{"23 St","FM"}, new[]{"Bleecker St","6"},
        new[]{"59 St-Columbus Circle","ABCD"}, new[]{"Chambers St","123"}, new[]{"Spring St","6"},
        new[]{"Broadway-Lafayette","BDFM"}, new[]{"Delancey St","FJMZ"}, new[]{"Lexington Av/59 St","456N"}
    };

    void Metro(Transform v)
    {
        float zc = r.Next(-3, 4);
        metroZ = zc;
        Color green = new Color(0.10f, 0.30f, 0.22f);
        Color greenDk = new Color(0.07f, 0.22f, 0.16f);
        Color black = new Color(0.05f, 0.05f, 0.06f);
        Color steel = new Color(0.72f, 0.74f, 0.76f);
        Color hole = new Color(0.05f, 0.06f, 0.08f);
        float x0 = 10.95f, x1 = 15.4f, cx = 13.2f;

        // stairwell: dark opening with steps that darken as they descend
        B(v, cx, 3.012f, zc, 4.4f, 0.02f, 3.0f, hole);
        for (int i = 0; i < 5; i++)
        {
            float g = 0.40f - i * 0.06f;
            B(v, 14.8f - i * 0.8f, 3.024f, zc, 0.5f, 0.02f, 3.0f, new Color(g, g, g + 0.02f));
        }
        // diamond-plate top step with a yellow edge
        B(v, x1 + 0.35f, 3.03f, zc, 0.7f, 0.06f, 3.3f, new Color(0.55f, 0.56f, 0.58f));
        B(v, x1 + 0.03f, 3.07f, zc, 0.12f, 0.03f, 3.3f, new Color(0.98f, 0.82f, 0.1f));

        for (int s = -1; s <= 1; s += 2)
        {
            float z = zc + 1.6f * s;
            B(v, cx, G + 0.45f, z, 4.5f, 0.9f, 0.14f, green);            // solid base panel
            B(v, cx, G + 0.95f, z, 4.6f, 0.08f, 0.24f, black);           // black coping
            for (int k = 0; k < 9; k++)
                B(v, x0 + 0.3f + k * 0.48f, G + 1.12f, z, 0.07f, 0.4f, 0.07f, green); // balusters
            B(v, cx, G + 1.34f, z, 4.5f, 0.06f, 0.08f, steel);           // steel handrail
            // corner posts with caps
            B(v, x0, G + 0.85f, z, 0.3f, 1.7f, 0.3f, green);
            B(v, x0, G + 1.75f, z, 0.42f, 0.1f, 0.42f, greenDk);
            B(v, x1, G + 0.85f, z, 0.3f, 1.7f, 0.3f, green);
            B(v, x1, G + 1.75f, z, 0.42f, 0.1f, 0.42f, greenDk);
            // globe lamp on a tall post at the street end
            Cy(v, x1, G + 2.7f, z, 0.12f, 1.9f, green);
            Sp(v, x1, G + 3.85f, z, 0.62f, new Color(0.35f, 0.88f, 0.52f));
        }

        // station sign at the far end, facing the runner, with real names and colored route bullets
        string[] en = Entrances[r.Next(Entrances.Length)];
        float sz = zc + 2.0f;
        B(v, cx, G + 2.1f, sz, 4.7f, 1.4f, 0.18f, green);
        B(v, cx, G + 2.1f, sz - 0.1f, 4.4f, 1.15f, 0.06f, black);
        float nh = Mathf.Min(0.36f, 3.2f / (0.62f * en[0].Length));
        LabelF(v, cx - 0.55f, G + 2.38f, sz - 0.15f, en[0], nh, Color.white);
        for (int k = 0; k < en[1].Length; k++)
        {
            char c = en[1][k];
            float bx = cx - 1.75f + k * 0.5f;
            GameObject d = Prim(PrimitiveType.Cylinder, v, bx, G + 1.82f, sz - 0.14f, 0.42f, 0.02f, 0.42f, Lines.Of(c), 0f);
            d.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            LabelF(v, bx, G + 1.82f, sz - 0.18f, c.ToString(), 0.3f, Lines.Text(c));
        }
        LabelF(v, cx + 0.9f, G + 1.82f, sz - 0.15f, "ENTER", 0.2f, new Color(0.8f, 0.8f, 0.8f));

        // station name plaque on the wall, angled a little toward the camera
        string name = Stations[r.Next(Stations.Length)];
        GameObject pv = new GameObject("Plaque");
        pv.transform.SetParent(v, false);
        pv.transform.localPosition = new Vector3(8.9f * sd, 1.6f, 0f);
        pv.transform.localRotation = Quaternion.Euler(0f, -10f * sd, 0f);
        B(pv.transform, 0f, 0f, 0f, 0.1f, 1.7f, 10f, new Color(0.92f, 0.92f, 0.90f));
        B(pv.transform, -0.04f, 0f, 0f, 0.08f, 1.5f, 9.8f, new Color(0.10f, 0.20f, 0.45f));
        float th = Mathf.Min(1.0f, 9.0f / (0.62f * name.Length));
        Label(pv.transform, -0.1f, 0f, 0f, name, th, Color.white);
    }

    // ---------------------------------------------------------------- buildings

    void Tenement(Transform v)
    {
        float h = 14f + r.Next(0, 6) * 2f;
        float L = 19.6f;
        Color b = Pick(Brick);
        Color glass = new Color(0.62f, 0.76f, 0.80f);
        Color dark = new Color(0.20f, 0.26f, 0.34f);
        Color lit = new Color(0.98f, 0.86f, 0.55f);
        Color trim = Sh(b, 1.25f);
        Color iron = new Color(0.10f, 0.10f, 0.11f);

        B(v, FaceX + 5f, G + h / 2f, 0f, 10f, h, L, b);
        B(v, FaceX - 0.05f, G + 1.8f, 0f, 0.1f, 3.6f, L - 0.6f, new Color(0.18f, 0.18f, 0.20f));

        for (int s = 0; s < 2; s++)
        {
            float zc = -4.8f + s * 9.6f;
            B(v, FaceX - 0.12f, G + 1.5f, zc, 0.08f, 2.3f, 7.4f, glass);
            if (r.Next(0, 100) < 75) B(v, FaceX - 1.0f, G + 3.4f, zc, 2.0f, 0.1f, 7.4f, Pick(Awnings), 14f);
            B(v, FaceX - 0.15f, G + 4.3f, zc, 0.2f, 0.8f, 6.4f, new Color(0.93f, 0.88f, 0.72f));
            Label(v, FaceX - 0.27f, G + 4.3f, zc, Shops[r.Next(Shops.Length)], 0.5f, new Color(0.15f, 0.15f, 0.18f));
        }

        for (float y = G + 5.4f; y < G + h - 1.6f; y += 3.2f)
        {
            for (int k = -2; k <= 2; k++)
            {
                float z = k * 3.6f;
                Color wc = r.Next(0, 100) < 22 ? lit : dark;
                B(v, FaceX - 0.05f, y, z, 0.1f, 1.7f, 1.4f, wc);
                B(v, FaceX - 0.12f, y - 0.95f, z, 0.24f, 0.14f, 1.8f, trim);
            }
        }

        B(v, FaceX - 0.35f, G + h - 0.4f, 0f, 0.9f, 0.8f, L + 0.4f, Sh(b, 0.78f));

        if (r.Next(0, 100) < 55)
        {
            float zc = r.Next(-5, 6);
            for (float y = G + 4.6f; y < G + h - 2f; y += 3.2f)
            {
                B(v, FaceX - 0.8f, y, zc, 1.5f, 0.1f, 3.2f, iron);
                B(v, FaceX - 1.5f, y + 0.5f, zc, 0.06f, 1.0f, 3.2f, iron);
                B(v, FaceX - 1.2f, y + 1.6f, zc + 1.0f, 0.06f, 3.2f, 0.4f, iron);
            }
        }

        if (r.Next(0, 100) < 50)
        {
            float zc = r.Next(-6, 7);
            Color wood = new Color(0.50f, 0.36f, 0.24f);
            B(v, FaceX + 5f, G + h + 1.1f, zc, 1.8f, 2.2f, 1.8f, iron);
            Cy(v, FaceX + 5f, G + h + 3.7f, zc, 3f, 3f, wood);
            Cy(v, FaceX + 5f, G + h + 5.3f, zc, 3.4f, 0.3f, new Color(0.30f, 0.25f, 0.20f));
        }
    }

    void Brownstone(Transform v)
    {
        Color iron = new Color(0.10f, 0.10f, 0.11f);
        Color stone = new Color(0.62f, 0.58f, 0.52f);
        Color dark = new Color(0.20f, 0.26f, 0.34f);
        Color lit = new Color(0.98f, 0.86f, 0.55f);

        for (int k = 0; k < 2; k++)
        {
            float zc = -5f + k * 10f;
            float h = 11f + r.Next(0, 4);
            Color b = Pick(Brown);
            B(v, FaceX + 5f, G + h / 2f, zc, 10f, h, 9.8f, b);

            for (int s = 0; s < 5; s++)
                B(v, FaceX - 0.25f - (4 - s) * 0.5f, G + (s + 1) * 0.21f, zc, 0.5f, (s + 1) * 0.42f, 2.0f, stone);
            for (int side = -1; side <= 1; side += 2)
                B(v, FaceX - 1.3f, G + 2.5f, zc + 1.05f * side, 3.3f, 0.07f, 0.07f, iron, 40f);

            B(v, FaceX - 0.06f, G + 3.4f, zc, 0.08f, 2.5f, 1.5f, Sh(b, 1.3f));
            B(v, FaceX - 0.03f, G + 3.3f, zc, 0.1f, 2.3f, 1.1f, new Color(0.45f, 0.12f, 0.10f));
            B(v, FaceX - 0.03f, G + 0.9f, zc + 2.8f, 0.1f, 0.7f, 0.9f, dark);
            B(v, FaceX - 0.03f, G + 0.9f, zc - 2.8f, 0.1f, 0.7f, 0.9f, dark);

            for (int s = -1; s <= 1; s += 2)
                B(v, FaceX - 0.05f, G + 3.8f, zc + 3.3f * s, 0.1f, 1.9f, 1.3f, r.Next(0, 100) < 20 ? lit : dark);
            for (float y = G + 6.6f; y < G + h - 1.5f; y += 3f)
            {
                for (int s = -1; s <= 1; s++)
                {
                    B(v, FaceX - 0.05f, y, zc + 3.3f * s, 0.1f, 1.9f, 1.3f, r.Next(0, 100) < 20 ? lit : dark);
                    B(v, FaceX - 0.12f, y - 1.05f, zc + 3.3f * s, 0.24f, 0.14f, 1.7f, stone);
                }
            }
            B(v, FaceX - 0.5f, G + h - 0.35f, zc, 1.1f, 0.7f, 9.9f, Sh(b, 0.7f));
        }
    }

    void Warehouse(Transform v)
    {
        float h = 8f + r.Next(0, 4);
        B(v, FaceX + 7f, G + h / 2f, 0f, 14f, h, 19.6f, new Color(0.55f, 0.50f, 0.46f));
        B(v, FaceX - 0.05f, G + 2.1f, -4.5f, 0.1f, 4.2f, 5.5f, new Color(0.28f, 0.30f, 0.34f));
        B(v, FaceX - 1.2f, G + 4.6f, -4.5f, 2.4f, 0.15f, 6f, new Color(0.18f, 0.18f, 0.20f));
        Color[] mural =
        {
            new Color(0.15f, 0.60f, 0.62f), new Color(0.95f, 0.55f, 0.15f),
            new Color(0.90f, 0.35f, 0.55f), new Color(0.96f, 0.82f, 0.25f)
        };
        for (int i = 0; i < 4; i++) B(v, FaceX - 0.05f, G + 3.0f + i * 1.3f, 5f, 0.08f, 1.3f, 7f, mural[i]);
        for (int k = -3; k <= 3; k++)
            B(v, FaceX - 0.05f, G + h - 1.3f, k * 2.6f, 0.1f, 0.9f, 1.8f, new Color(0.25f, 0.32f, 0.40f));
        B(v, FaceX + 4f, G + h + 0.6f, 4f, 2.5f, 1.2f, 2.5f, new Color(0.4f, 0.4f, 0.42f));
        B(v, FaceX + 8f, G + h + 0.5f, -5f, 3f, 1f, 3f, new Color(0.4f, 0.4f, 0.42f));
    }

    void Tower(Transform v)
    {
        float ht = 30f + r.Next(0, 16);
        Color glass = new Color(0.46f, 0.58f, 0.68f);
        Color band = new Color(0.30f, 0.40f, 0.50f);
        B(v, FaceX + 4f, G + 3f, 0f, 8f, 6f, 19.6f, new Color(0.60f, 0.58f, 0.55f));
        B(v, FaceX - 0.05f, G + 2.4f, 0f, 0.1f, 3.6f, 17f, new Color(0.18f, 0.32f, 0.36f));
        B(v, FaceX + 9f, G + ht / 2f, 0f, 12f, ht, 15f, glass);
        float tx = FaceX + 2.93f;
        for (float y = G + 7f; y < G + ht - 1f; y += 3.3f) B(v, tx, y, 0f, 0.12f, 0.5f, 15.1f, band);
        for (int k = -3; k <= 2; k++) B(v, tx - 0.01f, G + 7f + (ht - 7f) / 2f, -7f + (k + 3) * 2.8f, 0.12f, ht - 7f, 0.25f, band);
        B(v, FaceX + 9f, G + ht + 1.5f, 0f, 6f, 3f, 8f, new Color(0.45f, 0.45f, 0.48f));
    }

    void Backdrop(Transform v)
    {
        float h = 30f + r.Next(0, 30);
        float tone = 0.62f + r.Next(0, 8) * 0.01f;
        B(v, 72f, G + h / 2f, r.Next(-3, 4), 14f, h, 16f, new Color(tone - 0.04f, tone, tone + 0.07f));
    }

    void Park(Transform v)
    {
        Color grass = new Color(0.45f, 0.62f, 0.35f);
        Color path = new Color(0.78f, 0.70f, 0.55f);
        Color iron = new Color(0.12f, 0.14f, 0.13f);
        float len = SlotLen + 0.04f;

        B(v, 53f, G, 0f, 54f, 0.08f, len, grass);
        B(v, 33f, G + 0.05f, 0f, 2f, 0.02f, len, path);
        B(v, 45f, G + 0.05f, r.Next(-4, 5), 26f, 0.02f, 2.2f, path);
        for (int k = 0; k < 8; k++) B(v, 26.3f, G + 0.7f, -8.75f + k * 2.5f, 0.06f, 1.3f, 0.06f, iron);
        B(v, 26.3f, G + 0.4f, 0f, 0.05f, 0.05f, len, iron);
        B(v, 26.3f, G + 1.2f, 0f, 0.05f, 0.05f, len, iron);

        for (int k = 0; k < 5; k++)
        {
            float x = 30f + r.Next(0, 27);
            float z = r.Next(-9, 10);
            Color leaf = Pick(Leaves);
            Cy(v, x, G + 1.5f, z, 0.5f, 3f, new Color(0.33f, 0.25f, 0.19f));
            Sp(v, x, G + 4.6f, z, 5f, leaf);
            Sp(v, x + 1f, G + 3.9f, z + 1f, 3.2f, Sh(leaf, 1.1f));
            Sp(v, x - 1f, G + 3.9f, z - 1f, 3.2f, Sh(leaf, 0.9f));
        }
        for (int k = 0; k < 2; k++)
        {
            float z = -6f + k * 12f;
            B(v, 31.5f, G + 0.5f, z, 0.5f, 0.1f, 1.8f, new Color(0.45f, 0.33f, 0.22f));
            B(v, 31.8f, G + 0.9f, z, 0.08f, 0.6f, 1.8f, new Color(0.45f, 0.33f, 0.22f));
            B(v, 31.5f, G + 0.25f, z - 0.7f, 0.4f, 0.5f, 0.08f, iron);
            B(v, 31.5f, G + 0.25f, z + 0.7f, 0.4f, 0.5f, 0.08f, iron);
        }
        Backdrop(v);
    }

    void Plaza(Transform v)
    {
        Color paving = new Color(0.70f, 0.68f, 0.64f);
        B(v, 48f, G, 0f, 44f, 0.08f, SlotLen + 0.04f, paving);
        int mode = r.Next(0, 3);
        if (mode == 0)
        {
            Cy(v, 42f, G + 0.39f, 0f, 9f, 0.7f, new Color(0.78f, 0.76f, 0.72f));
            Cy(v, 42f, G + 0.37f, 0f, 8.2f, 0.66f, new Color(0.40f, 0.62f, 0.75f));
            Cy(v, 42f, G + 1.9f, 0f, 1f, 2.6f, new Color(0.78f, 0.76f, 0.72f));
            Cy(v, 42f, G + 3.3f, 0f, 3f, 0.4f, new Color(0.78f, 0.76f, 0.72f));
            Sp(v, 42f, G + 4.1f, 0f, 1.2f, new Color(0.75f, 0.88f, 0.95f));
        }
        else if (mode == 1)
        {
            Color white = new Color(0.94f, 0.94f, 0.92f);
            B(v, 44f, G + 0.06f, 0f, 22f, 0.03f, 17f, new Color(0.72f, 0.42f, 0.30f));
            B(v, 44f, G + 0.08f, 0f, 0.15f, 0.02f, 17f, white);
            B(v, 33f, G + 0.08f, 0f, 0.15f, 0.02f, 17f, white);
            B(v, 55f, G + 0.08f, 0f, 0.15f, 0.02f, 17f, white);
            B(v, 44f, G + 0.08f, 8.5f, 22f, 0.02f, 0.15f, white);
            B(v, 44f, G + 0.08f, -8.5f, 22f, 0.02f, 0.15f, white);
            for (int e = -1; e <= 1; e += 2)
            {
                float x = 44f + e * 11.5f;
                Cy(v, x, G + 1.9f, 0f, 0.2f, 3.8f, new Color(0.3f, 0.3f, 0.32f));
                B(v, x - e * 0.4f, G + 3.6f, 0f, 0.06f, 1.1f, 1.6f, white);
                Cy(v, x - e * 0.8f, G + 3.2f, 0f, 0.5f, 0.04f, new Color(0.95f, 0.45f, 0.1f));
            }
        }
        else
        {
            B(v, 42f, G + 0.06f, 0f, 16f, 0.03f, 16f, new Color(0.30f, 0.50f, 0.45f));
            Color post = new Color(0.20f, 0.35f, 0.75f);
            for (int e = -1; e <= 1; e += 2)
            {
                Cy(v, 38f, G + 1.5f, e * 3f, 0.2f, 3f, post);
                Cy(v, 43f, G + 1.5f, e * 3f, 0.2f, 3f, post);
            }
            B(v, 40.5f, G + 3f, 0f, 5.4f, 0.2f, 0.2f, post);
            B(v, 40.5f, G + 3f, 3f, 5.4f, 0.2f, 0.2f, post);
            B(v, 40.5f, G + 3f, -3f, 5.4f, 0.2f, 0.2f, post);
            B(v, 38.5f, G + 0.5f, 0f, 0.5f, 0.1f, 0.5f, new Color(0.9f, 0.7f, 0.1f));
            B(v, 48f, G + 1.5f, 0f, 2.2f, 3f, 2.2f, new Color(0.85f, 0.25f, 0.20f));
            B(v, 48f, G + 3.2f, 0f, 2.6f, 0.4f, 2.6f, new Color(0.95f, 0.78f, 0.15f));
            B(v, 46f, G + 1.6f, 4f, 3.4f, 0.12f, 1.0f, new Color(0.90f, 0.75f, 0.15f), -35f);
        }
        Backdrop(v);
    }

    void Construction(Transform v)
    {
        Color conc = new Color(0.62f, 0.62f, 0.60f);
        Color yellow = new Color(0.90f, 0.70f, 0.10f);
        Color orange = new Color(0.95f, 0.50f, 0.10f);
        float x = 36f;

        B(v, x, G + 0.1f, 0f, 10f, 0.2f, 18f, new Color(0.5f, 0.47f, 0.43f));
        for (int f = 1; f <= 4; f++) B(v, x, G + f * 3.5f, 0f, 9f, 0.3f, 17f, conc);
        for (int a = -1; a <= 1; a += 2)
            for (int c = -1; c <= 1; c += 2)
                B(v, x + a * 4.2f, G + 7.2f, c * 8f, 0.5f, 14.4f, 0.5f, conc);

        for (int k = 0; k < 6; k++) B(v, FaceX - 0.9f, G + 7f, -8f + k * 3.2f, 0.1f, 14f, 0.1f, new Color(0.5f, 0.52f, 0.55f));
        for (int f = 1; f <= 4; f++) B(v, FaceX - 0.9f, G + f * 3.5f, 0f, 0.1f, 0.1f, 17f, new Color(0.5f, 0.52f, 0.55f));

        B(v, 40f, G + 14f, -3f, 1f, 28f, 1f, yellow);
        B(v, 36f, G + 28f, -3f, 20f, 0.8f, 0.8f, yellow);
        B(v, 47f, G + 27f, -3f, 3f, 2f, 2f, new Color(0.5f, 0.5f, 0.52f));
        B(v, 40f, G + 29.5f, -3f, 0.5f, 3f, 0.5f, yellow);

        for (int k = 0; k < 4; k++)
        {
            float z = -6f + k * 4f;
            B(v, 29f, G + 0.3f, z, 0.2f, 0.6f, 2f, orange);
            B(v, 29f, G + 0.75f, z, 0.2f, 0.3f, 2f, Color.white);
        }
        B(v, 28f, G + 0.8f, 6f, 1.8f, 1.2f, 3f, new Color(0.20f, 0.40f, 0.70f));
        Backdrop(v);
    }

    // ---------------------------------------------------------------- primitives

    // x is the distance from the track center and is mirrored by side; rz is a tilt that mirrors too.
    GameObject Prim(PrimitiveType t, Transform p, float x, float y, float z, float sx, float sy, float sz, Color c, float rz)
    {
        GameObject g = GameObject.CreatePrimitive(t);
        Collider col = g.GetComponent<Collider>();
        if (col != null) Destroy(col);
        g.transform.SetParent(p, false);
        g.transform.localPosition = new Vector3(x * sd, y, z);
        g.transform.localRotation = Quaternion.Euler(0f, 0f, rz * sd);
        g.transform.localScale = new Vector3(sx, sy, sz);
        Renderer rd = g.GetComponent<Renderer>();
        rd.sharedMaterial = Mat(c);
        rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rd.receiveShadows = false;
        return g;
    }

    GameObject B(Transform p, float x, float y, float z, float sx, float sy, float sz, Color c, float rz = 0f)
    {
        return Prim(PrimitiveType.Cube, p, x, y, z, sx, sy, sz, c, rz);
    }

    GameObject Cy(Transform p, float x, float y, float z, float d, float h, Color c)
    {
        return Prim(PrimitiveType.Cylinder, p, x, y, z, d, h / 2f, d, c, 0f);
    }

    GameObject Sp(Transform p, float x, float y, float z, float d, Color c)
    {
        return Prim(PrimitiveType.Sphere, p, x, y, z, d, d, d, c, 0f);
    }

    void Label(Transform p, float x, float y, float z, string text, float height, Color c)
    {
        GameObject g = new GameObject("Label");
        g.transform.SetParent(p, false);
        g.transform.localPosition = new Vector3(x * sd, y, z);
        g.transform.localRotation = Quaternion.Euler(0f, 90f * sd, 0f);
        TextMesh tm = g.AddComponent<TextMesh>();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.font = font;
        tm.text = text;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = 100;
        tm.characterSize = height / 10f;
        tm.color = c;
        MeshRenderer mr = g.GetComponent<MeshRenderer>();
        mr.sharedMaterial = font.material;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    // Text on a surface that faces the runner (-z).
    void LabelF(Transform p, float x, float y, float z, string text, float height, Color c)
    {
        Label(p, x, y, z, text, height, c);
        Transform t = p.GetChild(p.childCount - 1);
        t.localRotation = Quaternion.identity;
    }

    Material Mat(Color c)
    {
        int key = (Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f) << 16)
                | (Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f) << 8)
                | Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f);
        Material m;
        if (!mats.TryGetValue(key, out m))
        {
            m = Look.Make(c);
            mats[key] = m;
        }
        return m;
    }

    Color Pick(Color[] a) { return a[r.Next(a.Length)]; }

    static Color Sh(Color c, float f)
    {
        return new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), 1f);
    }
}
