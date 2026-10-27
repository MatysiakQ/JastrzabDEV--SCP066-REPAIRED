using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.CustomRoles.API.Features;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace Scp066
{
    public class Scp066Role : CustomRole
    {
        public override uint Id { get; set; } = 660;
        public override string Name { get; set; } = "SCP-066";
        public override RoleTypeId Role { get; set; } = RoleTypeId.Scp0492;
        public override int MaxHealth { get; set; } = 2000;
        public override float SpawnChance { get; set; } = 0f;
        public override string CustomInfo { get; set; } = "SCP-066";
        public override string Description { get; set; } = "Eric's Toy";

        protected override void RoleAdded(Player player)
        {
            base.RoleAdded(player);
            if (player == null) return;

            Timing.CallDelayed(0.5f, () =>
            {
                if (player == null || !player.IsAlive || !Check(player)) return;

                int maxHp = Plugin.Instance != null ? Plugin.Instance.Config.MaxHealth : MaxHealth;
                float humeShield = Plugin.Instance != null ? Plugin.Instance.Config.HumeShield : 500f;

                player.MaxHealth = maxHp;
                player.Health = maxHp;
                player.HumeShield = humeShield;
                player.MaxHumeShield = humeShield;

                player.Scale = new Vector3(0.5f, 0.5f, 0.5f);

                player.EnableEffect<Slowness>(10, 9999f);
                player.EnableEffect(EffectType.Fade, 255, 9999f);
                player.InfoArea = (PlayerInfoArea)0;

                player.ClearInventory();
            });

            if (player.GameObject != null && !player.GameObject.TryGetComponent(out Scp066AbilityComponent _))
                player.GameObject.AddComponent<Scp066AbilityComponent>();
        }

        protected override void RoleRemoved(Player player)
        {
            if (player != null && player.GameObject != null)
            {
                if (player.GameObject.TryGetComponent(out Scp066AbilityComponent comp))
                    Object.Destroy(comp);

                if (player.IsConnected)
                {
                    player.Scale = Vector3.one;
                    player.DisableEffect<Slowness>();
                    player.DisableEffect(EffectType.Fade);
                    player.InfoArea = PlayerInfoArea.Nickname | PlayerInfoArea.CustomInfo | PlayerInfoArea.Badge | PlayerInfoArea.Role;
                }
            }

            base.RoleRemoved(player);
        }
    }
}