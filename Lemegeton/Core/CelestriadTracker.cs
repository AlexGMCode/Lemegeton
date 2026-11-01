using System;
using System.Collections.Generic;
using System.Linq;

namespace Lemegeton.Core
{
    // No game dependencies: the same state machine is exercised by combat-event simulations.
    internal sealed class CelestriadTracker
    {
        internal const uint Cast = 0xBB42;
        internal static bool IsElement(uint status) => status == 0xB56 || status == 0xBB6 || status == 0xB57;
        private uint[] party = Array.Empty<uint>();
        private readonly Dictionary<uint, uint> recipients = new();
        private long deadline, settleAt;
        private bool collecting;
        internal Func<uint, int, bool> Apply { get; set; } = (_, _) => false;

        internal void Reset()
        {
            collecting = false;
            recipients.Clear();
            party = Array.Empty<uint>();
            // Keep the cast lockout: a delayed duplicate must not reopen a cancelled window.
        }

        internal void Begin(uint cast, IEnumerable<uint> members, long now, float castSeconds)
        {
            if (cast != Cast || now < deadline || !float.IsFinite(castSeconds)) return;
            Reset();
            party = members.OrderBy(x => x).ToArray();
            if (party.Length != 8 || party.Any(x => x == 0 || x == 0xE0000000) || party.Distinct().Count() != 8) return;
            deadline = now + (long)(Math.Clamp(castSeconds, 0, 25) * 1000) + 5000;
            collecting = true;
            settleAt = long.MaxValue;
        }

        internal void Status(uint actor, uint status, bool gained, long now)
        {
            if (!collecting || now >= deadline || !IsElement(status) || !party.Contains(actor)) return;
            if (!gained || (recipients.TryGetValue(actor, out uint previous) && previous != status))
            {
                Reset();
                return;
            }
            if (recipients.TryAdd(actor, status)) settleAt = now + 200;
            if (recipients.Count > 6) Reset();
        }

        internal void Tick(IEnumerable<uint> members, IReadOnlyDictionary<uint, uint> liveElements, long now, bool enabled)
        {
            if (!enabled || !party.SequenceEqual(members.OrderBy(x => x))) { Reset(); return; }
            if (!collecting) return;
            if (now >= deadline) { Reset(); return; }
            if (recipients.Count != 6 || now < settleAt) return;
            // Reject late/stale packet sets and any seventh currently affected party member.
            if (liveElements.Count != 6 || recipients.Any(x => !liveElements.TryGetValue(x.Key, out uint s) || s != x.Value)) return;
            collecting = false;
            uint[] unaffected = party.Where(x => !recipients.ContainsKey(x)).ToArray();
            // Duplicate-cast lockout only; markers are cleared manually in game.
            deadline = Math.Max(deadline, now + 10000);
            for (int i = 0; i < 2; i++)
            {
                if (!Apply(unaffected[i], i + 1)) { Reset(); return; }
            }
        }
    }
}
