namespace Yumiwa.Domain
{
    /// <summary>
    /// Evidence entry per GDD §03 Aturan Evidence.
    /// Setiap evidence punya 6 atribut wajib: nama, deskripsi, hotspot, fakta, dependency, consequence.
    /// Critical Evidence masuk Evidence Log, tidak pakai slot fisik, tidak bisa dibuang.
    /// </summary>
    public readonly struct Evidence
    {
        public readonly string EvidenceId;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly string Hotspot;
        public readonly string Fact;
        public readonly string Dependency;
        public readonly string Consequence;
        public readonly EvidenceType Type;
        public readonly EvidenceCategory Category;

        public Evidence(
            string evidenceId,
            string displayName,
            string description,
            string hotspot,
            string fact,
            string dependency,
            string consequence,
            EvidenceType type = EvidenceType.Critical,
            EvidenceCategory category = EvidenceCategory.Fact)
        {
            EvidenceId = evidenceId;
            DisplayName = displayName;
            Description = description;
            Hotspot = hotspot;
            Fact = fact;
            Dependency = dependency;
            Consequence = consequence;
            Type = type;
            Category = category;
        }

        public bool IsValid => !string.IsNullOrEmpty(EvidenceId);
    }
}