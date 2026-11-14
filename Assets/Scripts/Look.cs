using UnityEngine;

// One place to make materials, so every object gets the blue pencil look.
// Falls back to URP Lit if the shader can't be found.
public static class Look
{
    static Shader pencil;

    public static Material Make(Color c)
    {
        if (pencil == null) pencil = Shader.Find("FF/Pencil");

        Material m;
        if (pencil != null) m = new Material(pencil);
        else m = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        return m;
    }
}
