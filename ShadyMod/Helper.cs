using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace ShadyMod
{
    public static class Helper
    {
        private static readonly List<string> ignoreEnemies = ["FlowerSnakeEnemy", "DoublewingedBird"];

        public static string FormatVector3(this Vector3 vector3)
        {
            return $"{vector3.x}|{vector3.y}|{vector3.z}";
        }

        public static bool GetRandomBoolean()
        {
            return UnityEngine.Random.value > 0.5f;
        }

        public static List<EnemyAI> GetNearbyEnemys(Vector3 playerPos, float detectionRadius = 10.0f)
        {           
            List<EnemyAI> enemies = [];
            foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
            {
                var enemy = obj.Value.GetComponent<EnemyAI>();
                if (enemy == null)
                    continue;

                if (enemy.isEnemyDead)
                    continue;

                if (ignoreEnemies.Any(e => enemy.name.Contains(e, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (Vector3.Distance(playerPos, enemy.transform.position) <= detectionRadius)
                    enemies.Add(enemy);
            }

            return enemies;
        }
    }
}