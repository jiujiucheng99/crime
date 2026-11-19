using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Local noise during a burglary. This is not a wanted level; it only
    /// affects how quickly the neighbour notices and reacts.
    /// </summary>
    public static class TheftNoise
    {
        public static float Value { get; private set; }
        public static bool InsideTheft { get; set; }

        public static void Add(float amount)
        {
            if (amount <= 0f) return;
            Value = Mathf.Clamp(Value + amount, 0f, 100f);
        }

        public static void Decay(float deltaTime)
        {
            if (Value <= 0f) return;
            Value = Mathf.MoveTowards(Value, 0f, 5f * deltaTime);
        }

        public static void Reset()
        {
            Value = 0f;
            InsideTheft = false;
        }

        public static string Label()
        {
            if (Value < 30f) return "安静";
            if (Value < 60f) return "可疑";
            if (Value < 85f) return "很吵";
            return "惊动";
        }
    }
}
