namespace VelcroNet.Collision;

public interface ICollisionEnterSink { void OnCollisionEnter(ref CollisionData data); }
public interface ICollisionExitSink  { void OnCollisionExit (ref CollisionData data); }
public interface ITriggerEnterSink   { void OnTriggerEnter  (ref TriggerData   data); }
public interface ITriggerExitSink    { void OnTriggerExit   (ref TriggerData   data); }

public interface IFullCollisionSink
    : ICollisionEnterSink, ICollisionExitSink, ITriggerEnterSink, ITriggerExitSink { }
