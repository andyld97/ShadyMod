using System;
using System.Collections.Generic;
using Unity.Netcode;
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

        public static IEnumerable<EnemyAI> GetNearbyEnemys(Vector3 playerPos, float detectionRadius = 10.0f)
        {
            foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
            {
                var enemy = obj.Value.GetComponent<EnemyAI>();
                if (enemy == null)
                    continue;

                if (enemy.isEnemyDead)
                    continue;

                if (Vector3.Distance(playerPos, enemy.transform.position) <= detectionRadius)
                    yield return enemy;
            }
        }
    }
}
