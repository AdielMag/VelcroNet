namespace AetherNet.Collision;

/// <summary>
/// Reference wrapper for entityId stored in Body.UserData.
/// Avoids boxing an int every time a collision is detected.
/// One instance is allocated per body at CreateBody time — never again.
/// </summary>
internal sealed class EntityToken
{
    public int EntityId;
}
