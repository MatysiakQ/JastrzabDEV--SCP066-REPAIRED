using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.CustomRoles.API.Features;
using CustomPlayerEffects;
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
            if (player == null || !Check(player)) return;

            Timing.CallDelayed(0.5f, () =>
            {
                if (player == null || !player.IsAlive) return;

                // Podpięte HP z pliku konfiguracyjnego
                player.MaxHealth = Plugin.Instance.Config.MaxHealth;
                player.Health = Plugin.Instance.Config.MaxHealth;

                player.HumeShield = 500f;
                player.MaxHumeShield = 500f;

                player.Scale = new Vector3(0.5f, 0.5f, 0.5f);

                player.EnableEffect<Slowness>(10, 9999f);

                // Ukrycie zombiaka - Fade 255 zamiast Invisible
                player.EnableEffect(EffectType.Fade, 255, 9999f);

                // Ukrycie nicku/ról nad głową
                player.InfoArea = (PlayerInfoArea)0;

                player.ClearInventory();
            });

            if (!player.GameObject.TryGetComponent(out Scp066AbilityComponent _))
                player.GameObject.AddComponent<Scp066AbilityComponent>();

            base.RoleAdded(player);
        }

        protected override void RoleRemoved(Player player)
        {
            player.Scale = Vector3.one;
            player.DisableEffect<Slowness>();
            player.DisableEffect(EffectType.Fade);
            player.InfoArea = PlayerInfoArea.Nickname | PlayerInfoArea.CustomInfo | PlayerInfoArea.Badge | PlayerInfoArea.Role;

            if (player.GameObject.TryGetComponent(out Scp066AbilityComponent comp))
                Object.Destroy(comp);

            base.RoleRemoved(player);
        }
    }
}