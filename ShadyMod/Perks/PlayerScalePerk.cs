using GameNetcodeStuff;
using ShadyMod.Model;
using ShadyMod.Network;
using ShadyMod.Patches;
using UnityEngine;

namespace ShadyMod.Perks
{
    public class ScaleSmallPerk : PerkBase
    {
        private const float playerJumpForce = 6f;
        private const float playerScale = 0.5f;

        private readonly Vector3 defaultPlayerScale = Vector3.zero;
        private readonly float defaultPlayerJumpForce = 0f;
        private readonly Vector3 defaultCameraPos = Vector3.zero;

        public ScaleSmallPerk(Vector3 playerScaleDefault, float playerJumpForceDefault, Vector3 defaultCameraPos)
        {
            defaultPlayerScale = playerScaleDefault;
            defaultPlayerJumpForce = playerJumpForceDefault;
            this.defaultCameraPos = defaultCameraPos;
        }   

        public override string Name => "Scale Small Perk";

        public override string Description => "Small crewmate (really smol)";

        public override string TriggerItemName => "paul";

        public override bool CanPerkBeIncreased => true;

        public override void Apply(PlayerControllerB player, bool force = false)
        {
            base.Apply(player);

            if (!force)
            {
                if (isApplied)
                    return;
            }

            float scale = playerScale;
            float jumpForce = playerJumpForce;
            if (ShouldIncreasePerk(player))
            {
                scale = 0.4f;
                jumpForce -= 1f;
            }

            if (!force)
                Helper.SendChatMessage($"{player.playerUsername} einfach kleinste Spieler!");

            player.transform.localScale = new Vector3(scale, scale, scale);
            player.gameplayCamera.transform.localPosition = new Vector3(0, scale, 0);
            player.jumpForce = jumpForce;

            if (!force)
            {
                NetworkMessage nm = new NetworkMessage()
                {
                    PlayerName = player.playerUsername,
                    Action = "apply",
                    PerkName = Name,
                    PlayerId = player.actualClientId,
                };

                // NetworkObjectManager.SendEventToClients(nm.ToString());
                PerkNetworkHandler.Instance.EventServerRpc(nm.ToString());
            }

            if (!force)
                isApplied = true;
        }

        public override void Reset(PlayerControllerB player, bool force = false)
        {          
            if (!force)
            {
                if (!isApplied)
                    return;
            }

            base.Reset(player);

            player.transform.localScale = defaultPlayerScale;
            player.gameplayCamera.transform.localPosition = defaultCameraPos;
            player.jumpForce = defaultPlayerJumpForce;

            if (!force)
            {
                NetworkMessage nm = new NetworkMessage()
                {
                    PlayerName = player.playerUsername,
                    Action = "reset",
                    PerkName = Name,
                    PlayerId = player.actualClientId,
                };

                // NetworkObjectManager.SendEventToClients(nm.ToString());
                PerkNetworkHandler.Instance.EventServerRpc(nm.ToString());
            }

            if (!force)
                isApplied = false;
        }
    }

    public class ScaleBigPerk : PerkBase
    {
        private const float playerJumpForce = 20f;
        private const float playerScale = 1.25f;

        private readonly Vector3 defaultPlayerScale = Vector3.zero;
        private readonly float defaultPlayerJumpForce = 0f;
        private readonly Vector3 defaultCameraPos = Vector3.zero;

        public ScaleBigPerk(Vector3 playerScaleDefault, float playerJumpForceDefault, Vector3 defaultCameraPos)
        {
            defaultPlayerScale = playerScaleDefault;
            defaultPlayerJumpForce = playerJumpForceDefault;
            this.defaultCameraPos = defaultCameraPos;
        }

        public override string Name => "Scale Big Perk";

        public override string Description => "Big crewmate (fr)";

        public override string TriggerItemName => "aveloth";

        public override bool CanPerkBeIncreased => true;

        public override void Apply(PlayerControllerB player, bool force = false)
        {
            if (!force)
            {
                if (isApplied)
                    return;
            }

            base.Apply(player);

            float scaleFactor = playerScale;
            float jumpForce = playerJumpForce;
            if (ShouldIncreasePerk(player))
            {
                scaleFactor += .5f;
                jumpForce += 2f;
            }

            player.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            player.gameplayCamera.transform.localPosition = new Vector3(0, scaleFactor, 0);
            player.jumpForce = jumpForce;

            if (!force)
            {
                NetworkMessage nm = new NetworkMessage()
                {
                    PlayerName = player.playerUsername,
                    Action = "apply",
                    PerkName = Name,
                    PlayerId = player.actualClientId,
                };

                // NetworkObjectManager.SendEventToClients(nm.ToString());
                PerkNetworkHandler.Instance.EventServerRpc(nm.ToString());
            }

            if (!force)
                isApplied = true;
        }

        public override void Reset(PlayerControllerB player, bool force = false)
        {
            if (!force)
            {
                if (!isApplied)
                    return;
            }

            base.Reset(player);
            player.transform.localScale = defaultPlayerScale;
            player.gameplayCamera.transform.localPosition = defaultCameraPos;
            player.jumpForce = defaultPlayerJumpForce;

            if (!force)
            {
                NetworkMessage nm = new NetworkMessage()
                {
                    PlayerName = player.playerUsername,
                    Action = "reset",
                    PerkName = Name,
                    PlayerId = player.actualClientId,
                };

                // NetworkObjectManager.SendEventToClients(nm.ToString());
                PerkNetworkHandler.Instance.EventServerRpc(nm.ToString());
            }

            if (!force)
                isApplied = false;
        }
    }
}