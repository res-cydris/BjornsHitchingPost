using System;

namespace BjornsHitchingPost;

internal static class TetherMath
{
    // Positions are relative to the post. Preserve tangential/inward velocity at the boundary.
    internal static bool Constrain(float radius, ref float x, ref float y, ref float z,
        ref float vx, ref float vy, ref float vz)
    {
        double distance = Math.Sqrt((double)x * x + (double)y * y + (double)z * z);
        if (distance <= radius || distance == 0) return false;
        float nx = (float)(x / distance), ny = (float)(y / distance), nz = (float)(z / distance);
        x = nx * radius; y = ny * radius; z = nz * radius;
        float speed = vx * nx + vy * ny + vz * nz;
        if (speed > 0) { vx -= nx * speed; vy -= ny * speed; vz -= nz * speed; }
        return true;
    }
}
