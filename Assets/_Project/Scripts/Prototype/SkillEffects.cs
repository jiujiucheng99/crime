using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Central skill effect formulas. Driving is intentionally left unused for
    /// now; the other skills must produce a visible gameplay benefit per level.
    /// </summary>
    public static class SkillEffects
    {
        public static float CookingGreenHalf(int level)
        {
            return Mathf.Lerp(0.09f, 0.24f, Mathf.Clamp01((level - 1) / 4f));
        }

        public static float MechanicsGreenHalf(int level)
        {
            return Mathf.Lerp(0.08f, 0.20f, Mathf.Clamp01((level - 1) / 4f));
        }

        public static float LogisticsSpeedMultiplier(int level)
        {
            return Mathf.Clamp(0.62f + level * 0.04f, 0.62f, 0.82f);
        }

        public static float LockpickNoiseMultiplier(int level)
        {
            return Mathf.Clamp(1f - (level - 1) * 0.15f, 0.4f, 1f);
        }

        public static float LockpickPaperclipSaveChance(int level)
        {
            return Mathf.Clamp((level - 1) * 0.1f, 0f, 0.4f);
        }

        public static int MechanicsBonusCap(int level)
        {
            return 4 + Mathf.Max(0, level - 1);
        }

        public static int LogisticsBonusCap(int level)
        {
            return 10 + Mathf.Max(0, level - 1) * 2;
        }
    }
}
