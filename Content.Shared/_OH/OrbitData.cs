namespace Content.Shared._OH;

public readonly record struct OrbitData(double Mu, double P, double E, double Omega)
{
    public bool IsBound => E < 1;
    public double Periapsis => P / (1 + E);
    public double Apoapsis => IsBound ? P / (1 - E) : double.PositiveInfinity;

    /// Положение относительно центра при истинной аномалии nu.
    public DoubleVec PositionAt(double nu)
    {
        var r = P / (1 + E * Math.Cos(nu));
        var a = nu + Omega;
        return new DoubleVec(r * Math.Cos(a), r * Math.Sin(a));
    }
}
