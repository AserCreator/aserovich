namespace Content.Shared._OH;

public static class OrbitalMath
{
    public static bool TryGetOrbit(DoubleVec r, DoubleVec v, double mu, out OrbitData orbit)
    {
        orbit = default;

        var rLen = r.Length();
        if (rLen < 1e-6)
            return false;

        // Удельный угловой момент (скаляр, т.к. движение в плоскости).
        var h = r.X * v.Y - r.Y * v.X;
        if (Math.Abs(h) < 1e-6) // чисто радиальное падение, эллипса нет
            return false;

        // Вектор эксцентриситета.
        var v2 = v.X * v.X + v.Y * v.Y;
        var rv = r.X * v.X + r.Y * v.Y;
        var k = v2 - mu / rLen;
        var ex = (k * r.X - rv * v.X) / mu;
        var ey = (k * r.Y - rv * v.Y) / mu;

        var e = Math.Sqrt(ex * ex + ey * ey);
        var omega = e > 1e-9 ? Math.Atan2(ey, ex) : 0;

        orbit = new OrbitData(mu, h * h / mu, e, omega);
        return true;
    }
}
