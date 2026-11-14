using UnityEngine;

// Primitive-built Finn (8) and Forrest (4), rounded shapes. Add to the player's "Model" object, set isFinn, call Build().
// Animates from PlayerController state: run swing, jump tuck, slide lean. Forrest is smaller, with a bigger head,
// and carries his orange plush giraffe in his right arm.
// v2 (Nov 14): redone from the Nov 13 notes, since the earlier pass was lost with the wipe.
public class CharacterRig : MonoBehaviour
{
    public PlayerController player;
    public bool isFinn = true;
    public float size = 0.6f;

    Transform armL, armR, legL, legR, body;
    float phase;

    static readonly Color Skin = new Color(0.96f, 0.78f, 0.64f);
    static readonly Color Black = new Color(0.07f, 0.07f, 0.08f);
    static readonly Color Jeans = new Color(0.22f, 0.38f, 0.68f);

    // The limb references are not saved with the scene, so rebuild them when Play starts.
    void Awake()
    {
        if (armL == null) Build();
    }

    public void Build()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject g = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(g); else DestroyImmediate(g);
        }
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        float s = isFinn ? size : size * 0.8f;
        Color top = isFinn ? Black : new Color(0.25f, 0.55f, 0.32f);        // black hoodie, green sweater
        Color shoe = isFinn ? Black : new Color(0.42f, 0.26f, 0.14f);       // black shoes, brown shoes
        Color hair = isFinn ? new Color(0.85f, 0.36f, 0.14f) : new Color(0.80f, 0.62f, 0.30f);
        float headS = isFinn ? 0.62f : 0.78f;                               // little kids have bigger heads
        float headY = 0.89f + headS * 0.5f;

        Transform root = New("Rig", transform, Vector3.zero);
        root.localScale = Vector3.one * s;
        body = New("Body", root, new Vector3(0, 1.0f, 0));

        // Torso, tapered, with a slim neck.
        Part(PrimitiveType.Capsule, body, new Vector3(0, 0.42f, 0), new Vector3(0.58f, 0.44f, 0.38f), top);
        if (isFinn) Part(PrimitiveType.Sphere, body, new Vector3(0, 0.84f, -0.10f), new Vector3(0.40f, 0.20f, 0.24f), top); // hood bunched at the neck
        Part(PrimitiveType.Capsule, body, new Vector3(0, 0.92f, 0), new Vector3(0.13f, 0.07f, 0.13f), Skin);

        // Head: narrow sides, no ears, no cheeks.
        Part(PrimitiveType.Sphere, body, new Vector3(0, headY, 0), new Vector3(headS * 0.86f, headS, headS * 0.92f), Skin);

        // Hair lies downward like bangs: a cap over the top and back, plus a fringe over the forehead.
        Part(PrimitiveType.Sphere, body, new Vector3(0, headY + headS * 0.12f, -headS * 0.10f), new Vector3(headS * 0.90f, headS * 0.82f, headS * 0.98f), hair);
        Part(PrimitiveType.Sphere, body, new Vector3(0, headY + headS * 0.26f, headS * 0.30f), new Vector3(headS * 0.80f, headS * 0.20f, headS * 0.30f), hair);

        // Small flat oval eyes.
        Part(PrimitiveType.Sphere, body, new Vector3(-headS * 0.17f, headY - headS * 0.02f, headS * 0.41f), new Vector3(0.075f, 0.05f, 0.03f), Black);
        Part(PrimitiveType.Sphere, body, new Vector3(headS * 0.17f, headY - headS * 0.02f, headS * 0.41f), new Vector3(0.075f, 0.05f, 0.03f), Black);

        // Arms start on sloped shoulders; hands are skin.
        armL = Arm(body, new Vector3(-0.35f, 0.80f, 0), 0.70f, 0.17f, top);
        armR = Arm(body, new Vector3(0.35f, 0.80f, 0), 0.70f, 0.17f, top);

        // Legs: one uniform jeans color top to bottom, real shoe shapes.
        legL = Leg(root, new Vector3(-0.16f, 1.0f, 0), 0.95f, 0.26f, shoe);
        legR = Leg(root, new Vector3(0.16f, 1.0f, 0), 0.95f, 0.26f, shoe);

        if (!isFinn) Giraffe(armR);
    }

    Transform Arm(Transform parent, Vector3 pivot, float len, float w, Color sleeve)
    {
        Transform t = New("Arm", parent, pivot);
        Part(PrimitiveType.Capsule, t, new Vector3(0, -len / 2f, 0), new Vector3(w, len / 2f, w), sleeve);
        Part(PrimitiveType.Sphere, t, new Vector3(0, -len, 0), Vector3.one * (w * 1.05f), Skin);
        return t;
    }

    Transform Leg(Transform parent, Vector3 pivot, float len, float w, Color shoe)
    {
        Transform t = New("Leg", parent, pivot);
        Part(PrimitiveType.Capsule, t, new Vector3(0, -len / 2f, 0), new Vector3(w, len / 2f, w), Jeans);
        Part(PrimitiveType.Sphere, t, new Vector3(0, -(len - 0.01f), 0.09f), new Vector3(0.28f, 0.16f, 0.50f), shoe);
        return t;
    }

    // Round orange plush with a snout, small ears, ball-tipped horns and spots.
    void Giraffe(Transform arm)
    {
        Color orange = new Color(0.95f, 0.60f, 0.15f);
        Color dark = new Color(0.70f, 0.38f, 0.10f);
        Color tan = new Color(0.98f, 0.82f, 0.50f);

        Transform g = New("Giraffe", arm, new Vector3(0.05f, -0.50f, 0.17f));
        Part(PrimitiveType.Sphere, g, Vector3.zero, Vector3.one * 0.34f, orange);
        Part(PrimitiveType.Sphere, g, new Vector3(0, 0.20f, 0.10f), Vector3.one * 0.20f, orange);
        Part(PrimitiveType.Sphere, g, new Vector3(0, 0.18f, 0.22f), new Vector3(0.10f, 0.08f, 0.10f), tan);
        Part(PrimitiveType.Sphere, g, new Vector3(-0.09f, 0.30f, 0.08f), Vector3.one * 0.06f, orange);
        Part(PrimitiveType.Sphere, g, new Vector3(0.09f, 0.30f, 0.08f), Vector3.one * 0.06f, orange);
        Part(PrimitiveType.Capsule, g, new Vector3(-0.04f, 0.34f, 0.10f), new Vector3(0.025f, 0.05f, 0.025f), tan);
        Part(PrimitiveType.Capsule, g, new Vector3(0.04f, 0.34f, 0.10f), new Vector3(0.025f, 0.05f, 0.025f), tan);
        Part(PrimitiveType.Sphere, g, new Vector3(-0.04f, 0.40f, 0.10f), Vector3.one * 0.045f, dark);
        Part(PrimitiveType.Sphere, g, new Vector3(0.04f, 0.40f, 0.10f), Vector3.one * 0.045f, dark);
        Part(PrimitiveType.Sphere, g, new Vector3(0.12f, 0.06f, 0.12f), Vector3.one * 0.07f, dark);
        Part(PrimitiveType.Sphere, g, new Vector3(-0.10f, -0.05f, 0.13f), Vector3.one * 0.06f, dark);
        Part(PrimitiveType.Sphere, g, new Vector3(0.0f, 0.10f, -0.15f), Vector3.one * 0.07f, dark);
    }

    Transform New(string n, Transform p, Vector3 pos)
    {
        GameObject g = new GameObject(n);
        g.transform.SetParent(p, false);
        g.transform.localPosition = pos;
        return g.transform;
    }

    void Part(PrimitiveType t, Transform p, Vector3 pos, Vector3 scale, Color c)
    {
        GameObject g = GameObject.CreatePrimitive(t);
        Collider col = g.GetComponent<Collider>();
        if (col != null) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
        g.transform.SetParent(p, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = Look.Make(c);
    }

    void Update()
    {
        if (armL == null || player == null) return;
        float rate = isFinn ? 11f : 15f;
        phase += Time.deltaTime * rate;
        float sw = Mathf.Sin(phase);
        if (!player.IsGrounded)
        {
            armL.localRotation = Quaternion.Euler(-150, 0, 10);
            armR.localRotation = Quaternion.Euler(-150, 0, -10);
            legL.localRotation = Quaternion.Euler(-50, 0, 0);
            legR.localRotation = Quaternion.Euler(20, 0, 0);
            body.localRotation = Quaternion.identity;
        }
        else if (player.IsSliding)
        {
            body.localRotation = Quaternion.Euler(35, 0, 0);
            armL.localRotation = Quaternion.Euler(-70, 0, 0);
            armR.localRotation = Quaternion.Euler(-70, 0, 0);
            legL.localRotation = Quaternion.Euler(-80, 0, 0);
            legR.localRotation = Quaternion.Euler(-60, 0, 0);
        }
        else
        {
            body.localRotation = Quaternion.Euler(14f, sw * 8f, sw * 4f);
            armL.localRotation = Quaternion.Euler(sw * 75f, 0, 6f);
            armR.localRotation = Quaternion.Euler(-sw * 75f, 0, -6f);
            legL.localRotation = Quaternion.Euler(-sw * 65f, 0, 0);
            legR.localRotation = Quaternion.Euler(sw * 65f, 0, 0);
            float bob = Mathf.Abs(sw) * (isFinn ? 0.05f : 0.09f);
            body.localPosition = new Vector3(0, 1.0f + bob, 0);
        }

        // Forrest keeps the giraffe cradled in his right arm instead of swinging it.
        if (!isFinn && player.IsGrounded && !player.IsSliding) armR.localRotation = Quaternion.Euler(-65f, 0, -8f);
    }
}
