using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Shared._OH;

public abstract class SharedOrbitalGravitySystem : EntitySystem
{
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] protected readonly SharedTransformSystem Xform = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    protected EntityQuery<OrbitalGravityComponent> GravQuery;

    public override void Initialize()
    {
        base.Initialize();

        GravQuery = GetEntityQuery<OrbitalGravityComponent>();
        SubscribeLocalEvent<PhysicsComponent, EntParentChangedMessage>(OnParentChanged);
    }
    private void OnParentChanged(EntityUid uid, PhysicsComponent body, ref EntParentChangedMessage args)
    {
        // При применении состояния с сервера скорость уже пришла готовой
        if (Timing.ApplyingState || body.BodyType != BodyType.Dynamic || args.OldParent is not { } oldParent)
            return;

        var newParent = args.Transform.ParentUid;

        if (!IsGridOrMap(oldParent) || !IsGridOrMap(newParent))
            return;

        var oldParentVel = Physics.GetMapLinearVelocity(oldParent);
        var newParentVel = Physics.GetMapLinearVelocity(newParent);

        Physics.SetLinearVelocity(uid, body.LinearVelocity + oldParentVel - newParentVel, body: body);
    }
    private bool IsGridOrMap(EntityUid uid)
    {
        return HasComp<MapGridComponent>(uid) || HasComp<MapComponent>(uid);
    }

    /// <summary>
    /// Вектор от центра гравитации до центра масс тела.
    /// </summary>
    public Vector2 GetRelative(OrbitalGravityComponent grav, TransformComponent xform, PhysicsComponent body)
    {
        var centerOfMass = Vector2.Transform(body.LocalCenter, Xform.GetWorldMatrix(xform));
        return centerOfMass - grav.Center;
    }

    /// <summary>
    /// Сила притяжения к центру на заданном расстоянии. Ниже MinRadius сила не растёт.
    /// </summary>
    public double GetGravityForce(OrbitalGravityComponent grav, double dist, float bodyMass)
    {
        double forceDist = Math.Max(dist, grav.MinRadius);
        return grav.Mass * (float) grav.G / (forceDist * forceDist) * bodyMass;
    }

    /// <summary>
    /// Скорость круговой орбиты на заданном расстоянии.
    /// </summary>
    public float GetCircularSpeed(OrbitalGravityComponent grav, float dist)
    {
        return (float) Math.Sqrt(grav.G * grav.Mass / dist);
    }
}
