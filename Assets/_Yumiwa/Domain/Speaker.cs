namespace Yumiwa.Domain
{
    /// <summary>
    /// Pure dialogue speaker definition per docs/09 data-driven dialog.
    /// NameColorHex drives the subtitle speaker-prefix color ("#RRGGBB" or "#RRGGBBAA").
    /// </summary>
    public readonly struct Speaker
    {
        public readonly string SpeakerId;
        public readonly string DisplayName;
        public readonly string NameColorHex;

        public Speaker(string speakerId, string displayName, string nameColorHex)
        {
            SpeakerId = speakerId;
            DisplayName = displayName;
            NameColorHex = nameColorHex;
        }

        public bool IsValid => !string.IsNullOrEmpty(SpeakerId);
    }
}
