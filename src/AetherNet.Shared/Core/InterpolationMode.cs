namespace AetherNet;

public enum InterpolationMode
{
    None,        // snap to last physics position — may stutter at low tick rates
    Interpolate, // lerp between previous and current tick position (recommended)
    Extrapolate, // project forward from current tick — smoother but can overshoot
}
