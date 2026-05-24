namespace AetherNet;

/// <summary>
/// Single source of truth for physics layer definitions and the collision matrix.
/// Both the headless server and the Unity client compile against this class, so
/// changes here are automatically reflected on both sides with no extra sync step.
///
/// EDITING GUIDE
/// ─────────────
/// 1. Add a new layer constant (next available int, 0–15 max).
/// 2. Add a case in MaskFor() for the new layer AND update any existing cases
///    that should interact with it — the matrix must be symmetric.
/// 3. Re-bake your scenes so the updated masks are written to the map JSON.
/// </summary>
public static class PhysicsLayers
{
    // ── Layer indices (0–15) ──────────────────────────────────────────────────

    public const int Default     = 0;
    public const int Player      = 1;
    public const int Environment = 2;
    public const int Projectile  = 3;
    public const int Trigger     = 4;

    // ── Collision matrix ──────────────────────────────────────────────────────
    // Returns which layers the given layer generates contacts with, as a bitmask.
    // Rule: if A collides with B, B must also include A — keep it symmetric.

    public static int MaskFor(int layer) => layer switch
    {
        Default     => All,
        Player      => Bit(Default) | Bit(Environment) | Bit(Trigger),
        Environment => All,
        Projectile  => Bit(Default) | Bit(Environment) | Bit(Player),
        Trigger     => Bit(Player),   // sensors; IsSensor=true prevents physical response
        _           => All,
    };

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Single-layer bitmask for layer index <paramref name="n"/>.</summary>
    public static int Bit(int n) => 1 << n;

    /// <summary>Interact with every layer.</summary>
    public const int All  = 0xFFFF;

    /// <summary>Interact with no layer (ghost / invisible fixture).</summary>
    public const int None = 0;
}
