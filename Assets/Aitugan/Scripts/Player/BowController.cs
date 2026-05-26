using System.Collections.Generic;
using UnityEngine;

namespace Aitugan.Player
{
    /// <summary>
    /// Tracks firepot positions placed in the world so the bow controller can
    /// upgrade a standard arrow to a fire arrow when fired near one.
    /// </summary>
    public static class BowController
    {
        static readonly List<Vector3> firepots = new();

        public static void RegisterFirepot(Vector3 worldPos) => firepots.Add(worldPos);
        public static void ClearFirepots() => firepots.Clear();

        public static bool IsNearFirepot(Vector3 p, float radius = 1.6f)
        {
            for (int i = 0; i < firepots.Count; i++)
                if ((firepots[i] - p).sqrMagnitude < radius * radius) return true;
            return false;
        }
    }
}
