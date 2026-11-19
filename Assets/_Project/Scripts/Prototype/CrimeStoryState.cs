namespace AmberCity.Prototype
{
    /// <summary>
    /// Runtime-only story flags for the first burglary loop. A save system can
    /// serialize this later without changing the narrative flow.
    /// </summary>
    public static class CrimeStoryState
    {
        public static bool LockpickTeacherMet;
        public static bool OwesFavor;
        public static bool NeighborReported;
        public static bool PoliceArrived;
        public static bool GangContacted;
        public static bool GangResolved;
        public static bool GangHostile;
        public static string LastNeighborName;
    }
}
