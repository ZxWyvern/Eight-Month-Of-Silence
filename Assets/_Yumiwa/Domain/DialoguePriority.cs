namespace Yumiwa.Domain
{
    /// <summary>
    /// Subtitle queue priority per plan.json — lower value wins (Story &gt; Radio &gt; Ambient).
    /// Higher priority interrupts lower; equal-or-lower enqueues with expiry.
    /// </summary>
    public enum DialoguePriority
    {
        Story = 0,
        Radio = 1,
        Ambient = 2
    }
}
