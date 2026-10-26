namespace Yumiwa.Domain
{
    public sealed class WeaponState : IAmmoReadModel
    {
        public int Current { get; private set; }
        public int Max { get; }
        public bool HasAmmo => Current > 0;
        public WeaponState(int max = 7) { Max = max; Current = max; } // <!-- TBD#6 -->
        public bool TryFire() { if (Current <= 0) return false; Current--; return true; }
        public void Reload() => Current = Max;
        public void Set(int v) => Current = v < 0 ? 0 : v > Max ? Max : v;
    }
    public readonly struct AmmoChangedEvent { public readonly int Current; public readonly int Max; public AmmoChangedEvent(int c, int m) { Current = c; Max = m; } }
}
