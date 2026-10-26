namespace Yumiwa.Domain
{
    /// <summary>Read-model seam for the ammo system; melee fallback keys off this until ammo ships.</summary>
    public interface IAmmoReadModel
    {
        bool HasAmmo { get; }
    }
}
