using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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

        public static void SendChatMessage(string chatMessage)
        {
            try
            {
                string item = $"<color=#FF00FF>Shady</color>: <color=#FFFF00>{chatMessage}</color>";
                HUDManager.Instance.ChatMessageHistory.Add(item);
                UpdateChatText();
            }
            catch (Exception ex)
            {
                ShadyMod.Logger.LogError($"#### Failed to send chat message: {ex.Message}");
            }
        }

        public static void DisplayTooltip(string message)
        {
            HUDManager.Instance.DisplayTip("ShadyMod", message, false, false, "LC_Tip1");
        }

        /// <summary>
        /// This currently only works for displaying local chat messages!
        /// </summary>
        private static void UpdateChatText()
        {
            ((TMP_Text)HUDManager.Instance.chatText).text = string.Join("\n", HUDManager.Instance.ChatMessageHistory);
        }
    }
}