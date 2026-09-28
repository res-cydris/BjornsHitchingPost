using System.Collections.Generic;

namespace BjornsHitchingPost;

internal static class OwnershipPolicy
{
    internal static long Select(long server, long preferred, bool serverActive, IReadOnlyList<long> compatibleNearby)
    {
        if (preferred == server && serverActive) return server;
        for (int i = 0; i < compatibleNearby.Count; i++)
            if (compatibleNearby[i] == preferred) return preferred;
        if (serverActive) return server;
        if (compatibleNearby.Count > 0) return compatibleNearby[0];
        return server; // Park safely; never use a vanilla client as simulator.
    }
}
