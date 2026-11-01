using System.Collections.Generic;
using System.Linq;

namespace Lemegeton.Core
{
    internal static class ScopedMarkerRules
    {
        internal static bool CanAssign(ulong actor, ulong holder, IEnumerable<ulong> markedActors) =>
            actor != 0 && actor != 0xE0000000 && (holder == 0 || holder == 0xE0000000) && !markedActors.Contains(actor);
    }
}
