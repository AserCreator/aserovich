using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._OH;

public sealed partial class OrbitalGravitySystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

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

        // Орбитальному гриду нужна честная динамика без потерь энергии.
        _physics.SetBodyType(ent, BodyType.Dynamic, body: body);
        _physics.SetLinearDamping(ent, body, 0f);
        _physics.SetAngularDamping(ent, body, 0f);
        _physics.SetSleepingAllowed(ent, body, false);

        if (!ent.Comp.InitCircular || xform.MapUid is not { } map || !_gravQuery.TryComp(map, out var grav))
            return;

        var r = GetRelative(grav, xform, body);
        var dist = r.Length();
        if (dist < grav.MinRadius)
            return;

        // Касательная к радиусу, против часовой стрелки.
        var tangent = new Vector2((float) (-r.Y / dist), (float) (r.X / dist));
        var speed = Math.Sqrt(grav.Mu / dist);
        _physics.SetLinearVelocity(ent, tangent * (float) speed, body: body);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<OrbitalBodyComponent, PhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var body, out var xform))
        {
            if (body.BodyType != BodyType.Dynamic || body.Mass <= 0f)
                continue;
            if (xform.MapUid is not { } map || !_gravQuery.TryComp(map, out var grav))
                continue;

            // Вектор от тела к центру, считаем в double.
            var r = GetRelative(grav, xform, body);
            var dist = Math.Max(r.Length(), grav.MinRadius);
            var accel = grav.Mu / (dist * dist);
            var dir = new Vector2((float) (-r.X / dist), (float) (-r.Y / dist));

            _physics.ApplyForce(uid, dir * (float) (accel * body.Mass), body: body);
        }
    }

    /// Положение центра масс грида относительно центра притяжения.
    private DoubleVec GetRelative(OrbitalGravityComponent grav, TransformComponent xform, PhysicsComponent body)
{
    var com = Vector2.Transform(body.LocalCenter, _xform.GetWorldMatrix(xform));
    return new DoubleVec(com.X - grav.Center.X, com.Y - grav.Center.Y);
}
}
