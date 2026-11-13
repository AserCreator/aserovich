using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Content.Shared._OH;

namespace Content.Server._OH;

public sealed partial class OrbitalGravitySystem : SharedOrbitalGravitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    private EntityQuery<OrbitalGravityComponent> _gravQuery;
    public override void Initialize()
    {
        _gravQuery = GetEntityQuery<OrbitalGravityComponent>();
        SubscribeLocalEvent<OrbitalBodyComponent, MapInitEvent>(OnMapInit);
    }
    private void OnMapInit(Entity<OrbitalBodyComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out PhysicsComponent? body) || !TryComp(ent, out TransformComponent? xform))
            return;

        if (xform.MapUid is not { } map || !_gravQuery.TryComp(map, out var grav))
            return;

        if (!ent.Comp.InitCircular)
            return;

        Vector2 r = xform.WorldPosition - grav.Center;
        double dist = getLen(r);

        if (dist < 1f || body.Mass <= 0f)
            return;

        double targetGM = grav.G * grav.Mass / body.Mass;
        double speed = Math.Sqrt(targetGM / dist);

        _physics.SetLinearVelocity(ent, new Vector2(r.Y, -r.X) * (float) (speed / dist), body: body);
    }
    public override void Update(float frameTime)
    {
        if (frameTime <= 0f)
            return;

        //_debugTick = _net.IsServer && _timing.CurTime >= _nextDebug;

        var query = EntityQueryEnumerator<OrbitalBodyComponent, PhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var orbital, out var body, out var xform))
        {
            if (body.BodyType != BodyType.Dynamic || body.Mass <= 0f)
                continue;

            if (xform.MapUid is not { } map || !_gravQuery.TryComp(map, out var grav))
            {
                continue;
            }
            TickPhysics(uid, orbital, body, xform, grav, frameTime);
        }
    }
    private void TickPhysics(
        EntityUid uid,
        OrbitalBodyComponent orbital,
        PhysicsComponent body,
        TransformComponent xform,
        OrbitalGravityComponent grav,
        float frameTime)
    {
        Vector2 r = xform.WorldPosition - grav.Center;//GetRelative(grav, xform, body);
        double dist = getLen(r);

        double impulse = ((grav.Mass*(float)grav.G)/(dist*dist));

        if (dist < 1f)
            return;

        _physics.ApplyLinearImpulse(uid, -r.Normalized()  *   (float)impulse  * frameTime, body: body);
    }
    public double getLen(Vector2 vec)
    {
        return Math.Sqrt(vec.X*vec.X + vec.Y*vec.Y);
    }
    private Vector2 GetRelative(OrbitalGravityComponent grav, TransformComponent xform, PhysicsComponent body)
    {
        var com = Vector2.Transform(body.LocalCenter, _xform.GetWorldMatrix(xform));
        return new Vector2(com.X - grav.Center.X, com.Y - grav.Center.Y);
    }
}
