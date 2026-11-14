using System.Numerics;
using Content.Shared._OH;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server._OH;

public sealed class OrbitalGravitySystem : SharedOrbitalGravitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrbitalBodyComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<OrbitalBodyComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.InitCircular)
            return;

        if (!TryComp(ent, out PhysicsComponent? body) || !TryComp(ent, out TransformComponent? xform))
            return;

        if (xform.MapUid is not { } map || !GravQuery.TryComp(map, out var grav))
            return;

        var r = GetRelative(grav, xform, body);
        var dist = r.Length();

        if (dist < 1f || body.Mass <= 0f)
            return;

        var speed = GetCircularSpeed(grav, dist);

        // Перпендикуляр к радиус-вектору, по часовой стрелке
        Physics.SetLinearVelocity(ent, new Vector2(r.Y, -r.X) * (speed / dist), body: body);
    }

    public override void Update(float frameTime)
    {
        if (frameTime <= 0f)
            return;

        var query = EntityQueryEnumerator<OrbitalBodyComponent, PhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var body, out var xform))
        {
            if (body.BodyType != BodyType.Dynamic || body.Mass <= 0f)
                continue;

            if (xform.MapUid is not { } map || !GravQuery.TryComp(map, out var grav))
                continue;

            ApplyGravity(uid, body, xform, grav, frameTime);
        }
    }

    private void ApplyGravity(
        EntityUid uid,
        PhysicsComponent body,
        TransformComponent xform,
        OrbitalGravityComponent grav,
        float frameTime)
    {
        var r = Xform.GetWorldPosition(xform) - grav.Center;
        var dist = r.Length();

        if (dist < 1f)
            return;

        var force = GetGravityForce(grav, dist, body.Mass);

        Physics.ApplyLinearImpulse(uid, -Vector2.Normalize(r) * (float) force * frameTime, body: body);
    }

}
