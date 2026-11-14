using UnityEngine;

// Primitive-built Finn (8) and Forrest (4). Add to the player's "Model" object, set isFinn, call Build().
// Animates from PlayerController state: run swing, jump tuck, slide lean. Forrest is smaller and bouncier.
// NOTE: this is the early blocky version saved in the project docs. The rounder pass from Nov 13 was lost.
public class CharacterRig : MonoBehaviour
{
    public PlayerController player;
    public bool isFinn = true;
    public float size = 0.6f;

    Transform armL, armR, legL, legR, body;
    float phase;

    static readonly Color Skin = new Color(0.96f, 0.78f, 0.64f);

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
        Color shirt = isFinn ? new Color(0.15f, 0.45f, 0.85f) : new Color(0.95f, 0.45f, 0.15f);
        Color pants = isFinn ? new Color(0.2f, 0.22f, 0.3f) : new Color(0.25f, 0.55f, 0.35f);
        Color hair = isFinn ? new Color(0.45f, 0.3f, 0.15f) : new Color(0.85f, 0.65f, 0.3f);

        Transform root = New("Rig", transform, Vector3.zero);
        root.localScale = Vector3.one * s;
        body = New("Body", root, new Vector3(0, 1.0f, 0));
        Part(PrimitiveType.Cube, body, new Vector3(0, 0.45f, 0), new Vector3(0.7f, 0.8f, 0.4f), shirt);
        float headS = isFinn ? 0.62f : 0.78f; // little kids have bigger heads
        Part(PrimitiveType.Sphere, body, new Vector3(0, 1.15f, 0), Vector3.one * headS, Skin);
        Part(PrimitiveType.Cube, body, new Vector3(0, 1.15f + headS * 0.35f, -0.02f), new Vector3(headS * 0.95f, isFinn ? 0.2f : 0.28f, headS * 0.95f), hair);
        Part(PrimitiveType.Sphere, body, new Vector3(-0.12f, 1.17f, 0.27f * headS / 0.62f), Vector3.one * 0.08f, Color.black);
        Part(PrimitiveType.Sphere, body, new Vector3(0.12f, 1.17f, 0.27f * headS / 0.62f), Vector3.one * 0.08f, Color.black);
        armL = Limb(body, new Vector3(-0.47f, 0.8f, 0), 0.7f, 0.2f, Skin, shirt);
        armR = Limb(body, new Vector3(0.47f, 0.8f, 0), 0.7f, 0.2f, Skin, shirt);
        legL = Limb(root, new Vector3(-0.18f, 1.0f, 0), 0.9f, 0.26f, pants, pants);
        legR = Limb(root, new Vector3(0.18f, 1.0f, 0), 0.9f, 0.26f, pants, pants);
        Part(PrimitiveType.Cube, legL, new Vector3(0, -0.9f, 0.08f), new Vector3(0.3f, 0.16f, 0.45f), Color.white);
        Part(PrimitiveType.Cube, legR, new Vector3(0, -0.9f, 0.08f), new Vector3(0.3f, 0.16f, 0.45f), Color.white);
    }

    Transform Limb(Transform parent, Vector3 pivot, float len, float w, Color a, Color b)
    {
        Transform t = New("Limb", parent, pivot);
        Part(PrimitiveType.Cube, t, new Vector3(0, -len / 2f, 0), new Vector3(w, len, w), a);
        return t;
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
        Renderer r = g.GetComponent<Renderer>();
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        r.sharedMaterial = m;
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
            body.localRotation = Quaternion.Euler(12, 0, 0);
            armL.localRotation = Quaternion.Euler(sw * 60f, 0, 0);
            armR.localRotation = Quaternion.Euler(-sw * 60f, 0, 0);
            legL.localRotation = Quaternion.Euler(-sw * 55f, 0, 0);
            legR.localRotation = Quaternion.Euler(sw * 55f, 0, 0);
            float bob = Mathf.Abs(sw) * (isFinn ? 0.05f : 0.09f);
            body.localPosition = new Vector3(0, 1.0f + bob, 0);
        }
    }
}
