using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.CustomRoles.API;
using Exiled.CustomRoles.API.Features;
using PlayerRoles;
using UnityEngine;

namespace Scp066
{
    public class Scp066Role : CustomRole
    {
        internal Plugin Owner { get; set; }
        private readonly Dictionary<Player, Scp066AbilityComponent> _components = new Dictionary<Player, Scp066AbilityComponent>();
        internal Scp066AbilityComponent GetComponent(Player player) => _components.TryGetValue(player, out var component) ? component : null;
        public override uint Id { get; set; } = 660;
        public override string Name { get; set; } = "SCP-066";
        public override RoleTypeId Role { get; set; } = RoleTypeId.Scp0492;
        public override int MaxHealth { get; set; } = 2000;
        public override float SpawnChance { get; set; } = 0f;
        public override string CustomInfo { get; set; } = "SCP-066";
        public override string Description { get; set; } = "Eric's Toy";
        public override Vector3 Scale { get; set; } = new Vector3(0.5f, 0.5f, 0.5f);
        // The game may already be changing the role. Only an explicit RA remove
        // should force Spectator, not an internal CustomRoles cleanup callback.
        public override bool RemovalKillsPlayer { get; set; } = false;
        // Avoid CustomRoles' delayed inventory clear affecting a later role.
        public override bool KeepInventoryOnSpawn { get; set; } = true;

        public override void AddRole(Player player)
        {
            if (Owner == null || !Owner.IsOperational || player == null || !player.IsConnected || player.HasAnyCustomRole()) return;
            PlayerRoleBase previousRole = player.ReferenceHub.roleManager.CurrentRole;
            float previousMaxHealth = player.MaxHealth;
            float previousHealth = player.Health;
            Vector3 previousScale = player.Scale;
            PlayerInfoArea previousInfoArea = player.InfoArea;
            string previousCustomInfo = player.CustomInfo;
            MaxHealth = Owner.Config.MaxHealth;
            base.AddRole(player);
            if (player.Role.Type == RoleTypeId.Scp0492 && Check(player)) return;
            if (Check(player)) RemoveRole(player);
            // CustomRoles applies stats even if another plugin cancels Role.Set.
            // Roll them back only if the original native role is still present.
            if (player.IsConnected && ReferenceEquals(previousRole, player.ReferenceHub.roleManager.CurrentRole))
            {
                player.Health = previousHealth;
                player.MaxHealth = previousMaxHealth;
                player.Scale = previousScale;
                player.InfoArea = previousInfoArea;
                player.CustomInfo = previousCustomInfo;
            }
            else if (player.IsConnected && player.IsAlive && player.Role.Type == RoleTypeId.Scp0492)
                player.Role.Set(RoleTypeId.Scp0492);
        }

        protected override void RoleAdded(Player player)
        {
            base.RoleAdded(player);
            if (player == null || !player.IsAlive || player.Role.Type != RoleTypeId.Scp0492) return;
            player.ClearInventory();
            var component = player.GameObject.AddComponent<Scp066AbilityComponent>();
            _components[player] = component;
            try { component.Initialize(Owner, this, player); }
            catch (Exception e)
            {
                Log.Error($"[SCP-066] Inicjalizacja postaci: {e}");
                component.Shutdown();
                UnityEngine.Object.Destroy(component);
                RemoveRole(player);
            }
        }

        protected override void RoleRemoved(Player player)
        {
            if (player != null && _components.TryGetValue(player, out var component))
            {
                _components.Remove(player);
                component.Shutdown();
                UnityEngine.Object.Destroy(component);
            }
            base.RoleRemoved(player);
        }
    }
}
