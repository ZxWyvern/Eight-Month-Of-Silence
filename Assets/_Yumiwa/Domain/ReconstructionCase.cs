namespace Yumiwa.Domain
{
    /// <summary>
    /// Pure deduction case per docs/03 (rekonstruksi): the player concludes from
    /// documented evidence instead of typing a keypad code. No UnityEngine.
    /// Content is authored in ReconstructionCaseAsset; this struct is the
    /// engine-free snapshot the Application service reasons about.
    /// </summary>
    public readonly struct ReconstructionCase
    {
        public readonly string CaseId;
        public readonly string Title;
        public readonly string[] RequiredEvidenceIds;
        public readonly int MinHotspotsPerEvidence;
        public readonly string[] OptionIds;
        public readonly string[] OptionTexts;
        public readonly string CorrectOptionId;
        public readonly Evidence Deduction;

        public ReconstructionCase(
            string caseId,
            string title,
            string[] requiredEvidenceIds,
            int minHotspotsPerEvidence,
            string[] optionIds,
            string[] optionTexts,
            string correctOptionId,
            Evidence deduction)
        {
            CaseId = caseId;
            Title = title;
            RequiredEvidenceIds = requiredEvidenceIds ?? System.Array.Empty<string>();
            MinHotspotsPerEvidence = minHotspotsPerEvidence < 0 ? 0 : minHotspotsPerEvidence;
            OptionIds = optionIds ?? System.Array.Empty<string>();
            OptionTexts = optionTexts ?? System.Array.Empty<string>();
            CorrectOptionId = correctOptionId;
            Deduction = deduction;
        }

        public bool IsValid =>
            !string.IsNullOrEmpty(CaseId)
            && RequiredEvidenceIds.Length > 0
            && OptionIds.Length > 0
            && OptionIds.Length == OptionTexts.Length
            && !string.IsNullOrEmpty(CorrectOptionId)
            && Deduction.IsValid;

        public bool HasOption(string optionId)
        {
            if (string.IsNullOrEmpty(optionId))
                return false;
            for (int i = 0; i < OptionIds.Length; i++)
                if (OptionIds[i] == optionId)
                    return true;
            return false;
        }
    }
}
