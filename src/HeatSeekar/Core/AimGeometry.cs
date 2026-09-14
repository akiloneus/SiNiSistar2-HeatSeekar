using System.Numerics;

namespace HeatSeekar.Core;

public static class AimGeometry
{
    /// <summary>Keep the facing chosen at attack start; a target behind it falls back to horizontal fire.</summary>
    public static Vector3 ConstrainToFacing(Vector3 launchPoint, Vector3 target, bool left, bool pointerValid)
    {
        var sign = left ? -1 : 1;
        return pointerValid && IsFinite(target) && (target.X - launchPoint.X) * sign >= 0
            ? target : launchPoint + new Vector3(sign * 10, 0, 0);
    }

    /// <summary>Intersect the camera ray with the XY plane through the actual launch point.</summary>
    public static bool TryGetTarget(Vector3 rayOrigin, Vector3 rayDirection, Vector3 launchPoint,
        float minimumDistance, out Vector3 target)
    {
        target = default;
        if (!IsFinite(rayOrigin) || !IsFinite(rayDirection) || !IsFinite(launchPoint)
            || !float.IsFinite(minimumDistance) || minimumDistance < 0
            || MathF.Abs(rayDirection.Z) < 0.00001f)
            return false;

        var distance = (launchPoint.Z - rayOrigin.Z) / rayDirection.Z;
        if (!float.IsFinite(distance) || distance <= 0)
            return false;

        var candidate = rayOrigin + distance * rayDirection;
        candidate.Z = launchPoint.Z;
        var squaredDistance = Vector3.DistanceSquared(candidate, launchPoint);
        if (!IsFinite(candidate) || !float.IsFinite(squaredDistance)
            || squaredDistance <= MathF.Max(minimumDistance * minimumDistance, 0.00000001f))
            return false;

        target = candidate;
        return true;
    }

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
