using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    /// <summary>ITimeSource bound to Unity Time.time.</summary>
    public sealed class UnityTimeSource : ITimeSource
    {
        public float Now => Time.time;
    }
}
