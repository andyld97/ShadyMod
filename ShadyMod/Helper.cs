using System;
using UnityEngine;

namespace ShadyMod
{
    public static class Helper
    {
        public static string FormatVector3(this Vector3 vector3)
        {
            return $"{vector3.x}|{vector3.y}|{vector3.z}";
        }

        public static bool GetRandomBoolean()
        {
            return UnityEngine.Random.value > 0.5f;
        }

        public static int GetRandomNumber(int min, int max)
        {
            return Convert.ToInt32((UnityEngine.Random.value + min) % max);
        }
    }
}
