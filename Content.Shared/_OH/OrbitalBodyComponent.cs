using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._OH;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OrbitalBodyComponent : Component
{
    /// Выдать круговую скорость при MapInit.
    [DataField, AutoNetworkedField] public bool InitCircular = true;

    /// Фиксированный радиус орбиты, который не позволит объекту улетать или падать.
    [DataField, AutoNetworkedField] public double OrbitRadius = 0;
}
