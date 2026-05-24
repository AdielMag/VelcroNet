namespace VelcroNet;

public enum ForceMode
{
    Force,          // continuous force — mass-dependent (integrated each tick)
    Impulse,        // instantaneous velocity change — mass-dependent
    VelocityChange, // instantaneous velocity change — mass-independent
    Acceleration,   // continuous acceleration — mass-independent
}
