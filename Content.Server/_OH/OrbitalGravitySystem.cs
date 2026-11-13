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

    // Временная отладка: раз в 5 секунд выводим состояние каждого орбитального тела.
    //private TimeSpan _nextDebug;
    //private bool _debugTick;

    public override void Initialize()
    {
        _gravQuery = GetEntityQuery<OrbitalGravityComponent>();
        SubscribeLocalEvent<OrbitalBodyComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<OrbitalBodyComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out PhysicsComponent? body) || !TryComp(ent, out TransformComponent? xform))
            return;

        // Физический режим (тяга, столкновения) тоже должен быть без потерь энергии.
        _physics.SetBodyType(ent, BodyType.Dynamic, body: body);
        _physics.SetLinearDamping(ent, body, 0f);
        _physics.SetAngularDamping(ent, body, 0f);
        _physics.SetSleepingAllowed(ent, body, false);

        if (!ent.Comp.InitCircular || xform.MapUid is not { } map || !_gravQuery.TryComp(map, out var grav))
            return;

        Vector2 r = xform.WorldPosition - grav.Center;//GetRelative(grav, xform, body);
        double dist = getLen(r);
        var tangent = new Vector2((float) (-r.Y / dist), (float) (r.X / dist));

        _physics.SetLinearVelocity(ent, tangent.Normalized() * (float)((grav.Mass*(float)grav.G)/(dist*dist))  * (1/body.Mass) * 0.016f, body: body);





        //var r = GetRelative(grav, xform, body);
        //var dist = r.Length();
        //if (dist < 1e-6)
        //    return;

        //// Касательная к радиусу, против часовой стрелки.
        //var tangent = new Vector2((float) (-r.Y / dist), (float) (r.X / dist));

        //// Снаружи MinRadius — v = sqrt(Mu/r), внутри (гравитация линейная по r) — v = r*sqrt(Mu/MinRadius^3).
        //var speed = dist >= grav.MinRadius
        //    ? Math.Sqrt(grav.Mu / dist)
        //    : dist * Math.Sqrt(grav.Mu / (grav.MinRadius * grav.MinRadius * grav.MinRadius));
        //_physics.SetLinearVelocity(ent, tangent * (float) speed, body: body);

        //// Сразу на рельсы.
        //TryEnterRails(ent, ent.Comp, body, xform, grav);
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
                //if (orbital.OnRails && _net.IsServer)
                //    LeaveRails(uid, orbital, body, "нет гравитации на карте");
                continue;
            }
            TickPhysics(uid, orbital, body, xform, grav, frameTime);
            //// Шаттловые системы могут вернуть демпфирование после MapInit — оно съедает скорость каждый тик.
            //orbital.DampingSeen = body.LinearDamping; // до нашего сброса — показывает, кто его возвращает
            //if (body.LinearDamping > 0f)
            //    _physics.SetLinearDamping(uid, body, 0f);

            //if (orbital.OnRails)
            //    TickRails(uid, orbital, body, xform, grav, frameTime);

            //// TickRails мог сбросить флаг (тяга/столкновение) — тогда в этом же тике считаем физику.
            //if (!orbital.OnRails)
            //    TickPhysics(uid, orbital, body, xform, grav, frameTime);

            //if (_debugTick)
            //    DebugState(uid, orbital, body, xform, grav);
        }

        //if (_debugTick)
        //    _nextDebug = _timing.CurTime + TimeSpan.FromSeconds(5);
    }


    private void TickPhysics(
        EntityUid uid,
        OrbitalBodyComponent orbital,
        PhysicsComponent body,
        TransformComponent xform,
        OrbitalGravityComponent grav,
        float frameTime)
    {
        // Вектор от центра к телу, считаем в double.


        //if (_net.IsServer)
        //{
        // Невязка: насколько скорость изменилась сверх нашей же гравитации прошлого тика.
        //var residual = (body.LinearVelocity - orbital.PrevVel) / frameTime - orbital.PrevAccel;
        //orbital.LastResidual = residual;
        //orbital.Settled = residual.Length() < orbital.ThrustThreshold ? orbital.Settled + 1 : 0;


        Vector2 r = xform.WorldPosition - grav.Center;//GetRelative(grav, xform, body);
        double dist = getLen(r);

        double impulse = ((grav.Mass*(float)grav.G)/(dist*dist));

        if (dist < 1f)
            return;

        // _physics.ApplyLinearImpulse(uid, -new Vector2((float)r.X,(float)r.Y)  *   impulse  * frameTime, body: body);
        _physics.ApplyLinearImpulse(uid, -r.Normalized()  *   (float)impulse  * frameTime, body: body);
        //var tangent = new Vector2((float) (-r.Y / dist), (float) (r.X / dist));

        //_physics.SetLinearVelocity(ent, tangent * (grav.Mass*(float)grav.G*body.Mass)/(dist*dist), body: body);
        //if (orbital.Settled >= orbital.SettleTicks)
        //{
        //    if (TryEnterRails(uid, orbital, body, xform, grav))
        //        return;

        //    orbital.Settled = 0; // повторная попытка через SettleTicks тиков
        //}
        //}

        // Снаружи MinRadius — закон Ньютона. Внутри — как внутри однородного шара: сила растёт линейно с r
        // и плавно обращается в ноль в центре (нет сингулярности и скачка направления).
        //var dist = Math.Max(len, grav.MinRadius);
        //var k = grav.Mu / (dist * dist * dist);
        //var a = new Vector2((float) (-r.X * k), (float) (-r.Y * k));
        // Импульс вместо силы: сила живёт только внутри шага физики и очищается, поэтому при подшагах или
        // расхождении частоты тиков и физики действует не весь тик. Импульс даёт ровно a*dt.
        //var v0 = body.LinearVelocity; // скорость ДО нашего толчка — с ней сравнивается следующий тик
        //_physics.ApplyLinearImpulse(uid, a * body.Mass * frameTime, body: body);
        //_physics.ApplyLinearImpulse(uid, -new Vector2((float)r.X,(float)r.Y)*(float)(grav.Mu * body.Mass / (len * len))   * frameTime, body: body);

        //orbital.PrevVel = v0;
        //orbital.PrevAccel = a;
    }


    /*
    private void DebugState(
        EntityUid uid,
        OrbitalBodyComponent orbital,
        PhysicsComponent body,
        TransformComponent xform,
        OrbitalGravityComponent grav)
    {
        var r = GetRelative(grav, xform, body);
        var v = new DoubleVec(body.LinearVelocity.X, body.LinearVelocity.Y);
        var eps = (v.X * v.X + v.Y * v.Y) / 2 - grav.Mu / r.Length();

        var text = $"[orbit] {ToPrettyString(uid)}: rails={orbital.OnRails} settled={orbital.Settled}/{orbital.SettleTicks} " +
                   $"r={r.Length():F1} |v|={body.LinearVelocity.Length():F2} eps={eps:F4} dampSeen={orbital.DampingSeen:F3} " +
                   $"|aGrav|={orbital.PrevAccel.Length():F3} resid=({orbital.LastResidual.X:F3};{orbital.LastResidual.Y:F3}) mass={body.Mass:F0}";

        if (OrbitalMath.TryGetOrbit(r, v, grav.Mu, out var o))
            text += $" e={o.E:F4} peri={o.Periapsis:F1} apo={o.Apoapsis:F1} minR={grav.MinRadius}";

        Log.Warning(text);
    }




    /// Режим "на рельсах": позиция и скорость берутся из аналитической орбиты.
    private void TickRails(
        EntityUid uid,
        OrbitalBodyComponent orbital,
        PhysicsComponent body,
        TransformComponent xform,
        OrbitalGravityComponent grav,
        float frameTime)
    {
        // Режим переключает только сервер, клиент повторяет по сети.
        if (_net.IsServer)
        {
            // На рельсах гравитацию физика не считает, значит любое изменение скорости — внешнее воздействие.
            var accel = (body.LinearVelocity - orbital.ExpectedVel).Length() / frameTime;
            if (accel > orbital.ThrustThreshold)
            {
                LeaveRails(uid, orbital, body,
                    $"внешнее ускорение {accel:F3} тайл/с², |v|={body.LinearVelocity.Length():F2}, ожидалось {orbital.ExpectedVel.Length():F2}, демпфирование {body.LinearDamping:F3}");
                return;
            }

            if (Math.Abs(orbital.Mu - grav.Mu) > 1e-9)
            {
                LeaveRails(uid, orbital, body, "изменился Mu");
                return;
            }
        }

        var orbit = new OrbitData(orbital.Mu, orbital.P, orbital.Ecc, orbital.Omega, orbital.Dir);

        // Время в double, поэтому дрейфа нет сколько бы ни шёл раунд.
        var t = (_timing.CurTime - orbital.Epoch).TotalSeconds;
        var m = orbital.M0 + orbit.MeanMotion * t;
        var nu = OrbitalMath.MeanToTrue(orbit, m);

        var pos = orbit.PositionAt(nu);
        var vel = orbit.VelocityAt(nu);

        // Позиция задаётся для центра масс, а двигаем начало координат грида.
        var com = new Vector2((float) (grav.Center.X + pos.X), (float) (grav.Center.Y + pos.Y));
        var origin = com - _xform.GetWorldRotation(xform).RotateVec(body.LocalCenter);
        _xform.SetWorldPosition((uid, xform), origin);

        var v = new Vector2((float) vel.X, (float) vel.Y);
        _physics.SetLinearVelocity(uid, v, body: body);
        orbital.ExpectedVel = v;
    }

    /// Физический режим: гравитация силой, плюс ожидание момента, когда можно вернуться на рельсы.
    private void TickPhysics(
        EntityUid uid,
        OrbitalBodyComponent orbital,
        PhysicsComponent body,
        TransformComponent xform,
        OrbitalGravityComponent grav,
        float frameTime)
    {
        // Вектор от центра к телу, считаем в double.
        var r = GetRelative(grav, xform, body);
        var len = r.Length();
        if (len < 1e-6)
            return;

        //if (_net.IsServer)
        //{
        // Невязка: насколько скорость изменилась сверх нашей же гравитации прошлого тика.
        var residual = (body.LinearVelocity - orbital.PrevVel) / frameTime - orbital.PrevAccel;
        orbital.LastResidual = residual;
        //orbital.Settled = residual.Length() < orbital.ThrustThreshold ? orbital.Settled + 1 : 0;

        //if (orbital.Settled >= orbital.SettleTicks)
        //{
        //    if (TryEnterRails(uid, orbital, body, xform, grav))
        //        return;

        //    orbital.Settled = 0; // повторная попытка через SettleTicks тиков
        //}
        //}

        // Снаружи MinRadius — закон Ньютона. Внутри — как внутри однородного шара: сила растёт линейно с r
        // и плавно обращается в ноль в центре (нет сингулярности и скачка направления).
        var dist = Math.Max(len, grav.MinRadius);
        var k = grav.Mu / (dist * dist * dist);
        var a = new Vector2((float) (-r.X * k), (float) (-r.Y * k));
        // Импульс вместо силы: сила живёт только внутри шага физики и очищается, поэтому при подшагах или
        // расхождении частоты тиков и физики действует не весь тик. Импульс даёт ровно a*dt.
        var v0 = body.LinearVelocity; // скорость ДО нашего толчка — с ней сравнивается следующий тик
        //_physics.ApplyLinearImpulse(uid, a * body.Mass * frameTime, body: body);
        _physics.ApplyLinearImpulse(uid, -new Vector2((float)r.X,(float)r.Y)*(float)(grav.Mu * body.Mass / (len * len))   * frameTime, body: body);

        orbital.PrevVel = v0;
        orbital.PrevAccel = a;
    }

    /// Пробует перевести тело на рельсы по текущим положению и скорости.
    private bool TryEnterRails(
        EntityUid uid,
        OrbitalBodyComponent orbital,
        PhysicsComponent body,
        TransformComponent xform,
        OrbitalGravityComponent grav)
    {
        var r = GetRelative(grav, xform, body);
        var v = new DoubleVec(body.LinearVelocity.X, body.LinearVelocity.Y);

        if (!OrbitalMath.TryGetOrbit(r, v, grav.Mu, out var orbit))
        {
            Log.Warning($"[orbit] {ToPrettyString(uid)}: на рельсы нельзя — орбита не определена (почти радиальное движение)");
            return false;
        }

        // Физика гравитацию ниже MinRadius смягчает, а Кеплер нет — такие орбиты оставляем физике.
        if (orbit.Periapsis < grav.MinRadius)
        {
            Log.Warning($"[orbit] {ToPrettyString(uid)}: на рельсы нельзя — периапсис {orbit.Periapsis:F1} < MinRadius {grav.MinRadius}");
            return false;
        }

        // Параболу формулы не покрывают.
        if (Math.Abs(orbit.E - 1) < 1e-4)
        {
            Log.Warning($"[orbit] {ToPrettyString(uid)}: на рельсы нельзя — орбита почти параболическая (e={orbit.E:F5})");
            return false;
        }

        orbital.Mu = orbit.Mu;
        orbital.P = orbit.P;
        orbital.Ecc = orbit.E;
        orbital.Omega = orbit.Omega;
        orbital.Dir = orbit.Direction;
        orbital.M0 = OrbitalMath.TrueToMean(orbit, orbit.TrueAnomalyOf(r));
        orbital.Epoch = _timing.CurTime;
        orbital.ExpectedVel = body.LinearVelocity;
        orbital.Settled = 0;
        orbital.OnRails = true;
        Dirty(uid, orbital);
        Log.Warning($"[orbit] {ToPrettyString(uid)} встал на рельсы: e={orbit.E:F4} peri={orbit.Periapsis:F1} dir={orbit.Direction}");
        return true;
    }

    private void LeaveRails(EntityUid uid, OrbitalBodyComponent orbital, PhysicsComponent body, string reason)
    {
        Log.Warning($"[orbit] {ToPrettyString(uid)} сошёл с рельсов: {reason}");

        orbital.OnRails = false;
        orbital.Settled = 0;
        orbital.PrevVel = body.LinearVelocity;
        orbital.PrevAccel = Vector2.Zero;
        Dirty(uid, orbital);
    }

    /// Положение центра масс грида относительно центра притяжения.

    */
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
