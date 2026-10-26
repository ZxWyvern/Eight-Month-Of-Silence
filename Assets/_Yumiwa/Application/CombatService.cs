using Yumiwa.Domain;
namespace Yumiwa.Application
{
    public sealed class CombatService
    {
        private readonly WeaponState weapon;
        private readonly IEventBus bus;
        public CombatService(WeaponState weapon, IEventBus bus) { this.weapon = weapon; this.bus = bus; }
        public int Ammo => weapon.Current;
        public int MaxAmmo => weapon.Max;
        public bool HasAmmo => weapon.HasAmmo;
        public bool TryFire()
        {
            if (!weapon.TryFire()) return false;
            bus?.Publish(new AmmoChangedEvent(weapon.Current, weapon.Max));
            return true;
        }
        public void Reload()
        {
            weapon.Reload();
            bus?.Publish(new AmmoChangedEvent(weapon.Current, weapon.Max));
        }
    }
}
