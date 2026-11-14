# Finn & Forrest's Running Adventure

Endless runner in Unity 6.3 LTS (6000.3.28f1), Universal 3D (URP), Input System.

Rebuilt on Nov 14, 2026 after a local machine wipe. `GameManager.cs` was rewritten from the handoff rules; `CharacterRig.cs` is the early blocky version.

## Open it
1. Unity Hub, Add, pick this folder (editor 6000.3.28f1)
2. Open Assets/Scenes/SampleScene
3. Edit > Play Mode > Play (arrows or WASD, Space to jump)

## Rules
Lane x = (lane - 1) * 52; player fixed, world scrolls; gravity 720; jump 265; sneakers x1.1; trains 60 high and never jumpable; Cfg.Scale = 0.1 converts game units to meters.
