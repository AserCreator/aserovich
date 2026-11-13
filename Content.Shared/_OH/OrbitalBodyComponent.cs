using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;

namespace Content.Shared._OH;

[RegisterComponent, NetworkedComponent]
public sealed partial class OrbitalBodyComponent : Component
{
    /// Выдать круговую скорость при MapInit.
    [DataField] public bool InitCircular = true;
}
