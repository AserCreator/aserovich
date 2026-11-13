using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._OH;

public abstract partial class SharedOrbitalGravitySystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    //private EntityQuery<OrbitalGravityComponent> _gravQuery;

    // Временная отладка: раз в 5 секунд выводим состояние каждого орбитального тела.
    //private TimeSpan _nextDebug;
    //private bool _debugTick;
}
