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

        _physics.SetBodyType(ent, BodyType.Kinematic, body: body);
        _physics.SetLinearDamping(ent, body, 0f);
        _physics.SetAngularDamping(ent, body, 0f);
        _physics.SetSleepingAllowed(ent, body, false);

        if (xform.MapUid is not { } map || !_gravQuery.TryComp(map, out var grav))
            return;

        var r = GetRelative(grav, xform, body);
        var dist = r.Length();

        // Фиксируем радиус при инициализации (если он еще не был задан, например, через маппинг)
        if (ent.Comp.OrbitRadius <= 0)
        {
            ent.Comp.OrbitRadius = Math.Max(dist, grav.MinRadius);
            Dirty(ent);
        }

        if (!ent.Comp.InitCircular || ent.Comp.OrbitRadius < grav.MinRadius)
            return;

        var speed = Math.Sqrt(grav.Mu / ent.Comp.OrbitRadius);
        var tangent = new Vector2((float)(-r.Y / dist), (float)(r.X / dist));
        _physics.SetLinearVelocity(ent, tangent * (float)speed, body: body);
    }

    public override void Update(float frameTime)
    {
        if (frameTime <= 0f)
            return;

        var query = EntityQueryEnumerator<OrbitalBodyComponent, PhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var orbitalBody, out var body, out var xform))
        {
            if (xform.MapUid is not { } map || !_gravQuery.TryComp(map, out var grav))
                continue;

            // Получаем вектор направления, но используем ЖЕСТКО зафиксированный радиус
            var r = GetRelative(grav, xform, body);
            var currentDist = r.Length();

            if (currentDist < 1e-6)
                continue;

            // Если радиус в компоненте почему-то не инициализировался, берем текущий
            if (orbitalBody.OrbitRadius <= 0)
            {
                orbitalBody.OrbitRadius = Math.Max(currentDist, grav.MinRadius);
                Dirty(uid, orbitalBody);
            }

            var dist = orbitalBody.OrbitRadius;

            // 1. Математически точная скорость для этого радиуса
            var speed = Math.Sqrt(grav.Mu / dist);

            // 2. Текущий угол вычисляем из фактического положения
            var currentAngle = Math.Atan2(r.Y, r.X);

            // 3. Шаг по орбите без накопления погрешности расстояния
            var angularVelocity = speed / dist;
            var newAngle = currentAngle + (angularVelocity * frameTime);

            // 4. Проекция на строгую окружность вокруг Center
            var newX = grav.Center.X + (dist * Math.Cos(newAngle));
            var newY = grav.Center.Y + (dist * Math.Sin(newAngle));

            var targetWorldPos = new Vector2((float)newX, (float)newY) - body.LocalCenter;
            _xform.SetWorldPosition(uid, targetWorldPos);

            // 5. Обновляем вектор физической скорости для плавности движения сущностей внутри грида
            var tangent = new Vector2((float)(-Math.Sin(newAngle)), (float)Math.Cos(newAngle));
            _physics.SetLinearVelocity(uid, tangent * (float)speed, body: body);
        }
    }

    private DoubleVec GetRelative(OrbitalGravityComponent grav, TransformComponent xform, PhysicsComponent body)
    {
        var com = Vector2.Transform(body.LocalCenter, _xform.GetWorldMatrix(xform));
        return new DoubleVec(com.X - grav.Center.X, com.Y - grav.Center.Y);
    }
}
