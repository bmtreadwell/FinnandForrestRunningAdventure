using UnityEngine;

// All gameplay numbers live here, in "game units" carried over from the Godot and three.js builds.
// Cfg.Scale converts game units to Unity meters (52 game units = 5.2 m).
public static class Cfg
{
    public const float Scale = 0.1f;

    public const int LaneCount = 3;
    public const float LaneWidth = 52f;

    public const float Gravity = 720f;
    public const float JumpVelocity = 265f;      // apex is about 49 units
    public const float SneakerBoost = 1.1f;
    public const float FastFallVelocity = -520f;

    public const float TrainHeight = 60f;        // never jumpable, the apex is only 49

    public const float LaneSwitchSpeed = 450f;   // game units per second
    public const float SlideTime = 0.7f;

    public const float SwipeThresholdScreenFraction = 0.06f;

    public static float LaneX(int lane)
    {
        return (lane - 1) * LaneWidth;
    }
}
