using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-time helpers that wire up the open scene. Each one skips itself if its work is already done.
[InitializeOnLoad]
public static class TestSceneSetup
{
    const string SceneKey = "FF_TestSceneBuilt_v1";
    const string RunnerKey = "FF_RunnerBuilt_v1";
    const string RigKey = "FF_RigBuilt_v1";
    const string EnvKey = "FF_EnvBuilt_v1";

    static TestSceneSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorPrefs.GetBool(SceneKey, false)) { Build(); EditorPrefs.SetBool(SceneKey, true); }
            if (!EditorPrefs.GetBool(RunnerKey, false)) { AddRunner(); EditorPrefs.SetBool(RunnerKey, true); }
            if (!EditorPrefs.GetBool(RigKey, false)) { UseRig(true); EditorPrefs.SetBool(RigKey, true); }
            if (!EditorPrefs.GetBool(EnvKey, false)) { AddEnvironment(); EditorPrefs.SetBool(EnvKey, true); }
        };
    }

    [MenuItem("Tools/Finn & Forrest/Build Test Scene")]
    public static void Build()
    {
        if (GameObject.Find("Player") != null)
        {
            Debug.Log("[FF] Player already exists, skipping scene build.");
            return;
        }

        GameObject player = new GameObject("Player");
        player.transform.position = Vector3.zero;

        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        model.name = "Model";
        model.transform.SetParent(player.transform, false);
        model.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        model.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

        PlayerController pc = player.AddComponent<PlayerController>();
        pc.model = model.transform;

        GameObject track = GameObject.CreatePrimitive(PrimitiveType.Cube);
        track.name = "Track";
        track.transform.position = new Vector3(0f, -0.5f, 80f);
        track.transform.localScale = new Vector3(18f, 1f, 200f);

        Camera cam = Camera.main;
        if (cam != null)
        {
            // Beau's tuned values from the last session: Y 7 and FOV about 60 (Z was still being tuned).
            cam.transform.position = new Vector3(0f, 7f, -10f);
            cam.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
            cam.fieldOfView = 60f;
        }

        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveScene(player.scene);
        Debug.Log("[FF] Test scene built and saved.");
    }

    [MenuItem("Tools/Finn & Forrest/Add Runner")]
    public static void AddRunner()
    {
        if (Object.FindFirstObjectByType<GameManager>() != null)
        {
            Debug.Log("[FF] GameManager already exists, skipping.");
            return;
        }

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("[FF] No Player in the open scene.");
            return;
        }

        GameObject gm = new GameObject("GameManager");
        GameManager g = gm.AddComponent<GameManager>();
        PlayerController pc = player.GetComponent<PlayerController>();
        g.player = pc;

        Camera cam = Camera.main;
        if (cam != null)
        {
            ChaseCamera cc = cam.GetComponent<ChaseCamera>();
            if (cc == null) cc = cam.gameObject.AddComponent<ChaseCamera>();
            cc.game = g;
            cc.player = pc;
        }

        EditorSceneManager.MarkSceneDirty(gm.scene);
        EditorSceneManager.SaveScene(gm.scene);
        Debug.Log("[FF] Runner added and scene saved.");
    }

    [MenuItem("Tools/Finn & Forrest/Add Environment")]
    public static void AddEnvironment()
    {
        if (Object.FindFirstObjectByType<EnvironmentSpawner>() != null)
        {
            Debug.Log("[FF] Environment already exists, skipping.");
            return;
        }
        GameObject env = new GameObject("Environment");
        EnvironmentSpawner es = env.AddComponent<EnvironmentSpawner>();
        es.game = Object.FindFirstObjectByType<GameManager>();
        EditorSceneManager.MarkSceneDirty(env.scene);
        EditorSceneManager.SaveScene(env.scene);
        Debug.Log("[FF] Environment added and scene saved.");
    }

    [MenuItem("Tools/Finn & Forrest/Toggle Autopilot")]
    public static void ToggleAutopilot()
    {
        bool on = !EditorPrefs.GetBool("FF_AutoPilot", false);
        EditorPrefs.SetBool("FF_AutoPilot", on);
        Debug.Log("[FF] Autopilot " + (on ? "ON" : "OFF"));
    }

    [MenuItem("Tools/Finn & Forrest/Use Finn")]
    public static void UseFinn() { UseRig(true); }

    [MenuItem("Tools/Finn & Forrest/Use Forrest")]
    public static void UseForrest() { UseRig(false); }

    // Swaps the capsule for the primitive-built character and sits it on the ground.
    static void UseRig(bool finn)
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.LogWarning("[FF] No Player in the open scene."); return; }

        Transform model = player.transform.Find("Model");
        if (model == null) { Debug.LogWarning("[FF] Player has no Model child."); return; }

        CharacterRig rig = model.GetComponent<CharacterRig>();
        if (rig == null) rig = model.gameObject.AddComponent<CharacterRig>();
        rig.player = player.GetComponent<PlayerController>();
        rig.isFinn = finn;
        rig.Build();

        // The rig stands on its own origin, so drop the capsule's 1.5 m lift and scale to about 3 m tall.
        model.localPosition = Vector3.zero;
        model.localScale = Vector3.one * 2f;

        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveScene(player.scene);
        Debug.Log("[FF] " + (finn ? "Finn" : "Forrest") + " rig applied and scene saved.");
    }
}
