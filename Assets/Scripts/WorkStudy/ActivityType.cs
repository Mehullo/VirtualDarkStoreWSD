namespace VirtualDarkStore.WorkStudy
{
    /// <summary>
    /// Enumeration of standardized industrial work-study and simulation activities.
    /// Strictly excludes uninstrumented physiological or cognitive claims.
    /// </summary>
    public enum ActivityType
    {
        Walking,
        Waiting,
        Searching,
        Approaching,
        Picking,
        MovingBetweenLocations,
        Packing,
        OrderCompleted
    }
}
