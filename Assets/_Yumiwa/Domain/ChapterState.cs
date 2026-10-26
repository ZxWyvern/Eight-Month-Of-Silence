namespace Yumiwa.Domain
{
    public enum ChapterState
    {
        None = 0,
        Chapter1_BloodTrail = 1,
        Chapter2_MotelMurder = 2,
        Chapter3_SurvivorWitness = 3,
        Chapter4_FoggyFace = 4,
        Chapter5_NoExitHouse = 5
    }

    public enum ObjectiveStatus
    {
        Inactive,
        Active,
        Completed,
        Failed
    }
}