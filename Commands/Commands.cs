using System;
using CommandSystem;
using Exiled.API.Features;
using Exiled.CustomRoles.API;
using Exiled.Permissions.Extensions;
using PlayerRoles;

namespace Scp066
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class Scp066Command : ICommand
    {
        public string Command => "scp066";
        public string[] Aliases => new[] { "066" };
        public string Description => "SCP-066: give/remove <nick/id>. Remove przenosi do obserwatorów.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission("scp066.admin")) { response = "Brak uprawnien."; return false; }
            if (arguments.Count < 2 || !IsOperation(arguments.At(0)))
            {
                response = "Uzycie: scp066 give/remove <nick/id>. Remove konczy zycie SCP-066.";
                return false;
            }
            Plugin plugin = Plugin.Instance;
            if (plugin == null || !plugin.IsOperational) { response = "SCP-066 nie jest aktywny."; return false; }
            Player target = Player.Get(string.Join(" ", arguments.Array, arguments.Offset + 1, arguments.Count - 1));
            if (target == null || !target.IsConnected) { response = "Nie znaleziono gracza."; return false; }
            Scp066Role role = plugin.Role;
            if (string.Equals(arguments.At(0), "give", StringComparison.OrdinalIgnoreCase))
            {
                if (target.HasAnyCustomRole()) { response = "Gracz ma juz CustomRole. Najpierw ja usun."; return false; }
                role.AddRole(target);
                bool added = role.Check(target) && target.Role.Type == RoleTypeId.Scp0492;
                response = added ? "Nadano SCP-066." : "Nie udalo sie nadac SCP-066.";
                return added;
            }
            if (!role.Check(target)) { response = "Gracz nie ma SCP-066."; return false; }
            role.RemoveRole(target);
            if (target.IsConnected && target.IsAlive) target.Role.Set(RoleTypeId.Spectator);
            response = "Odebrano SCP-066; gracz zostal przeniesiony do obserwatorow.";
            return true;
        }

        internal static bool IsOperation(string operation) =>
            string.Equals(operation, "give", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(operation, "remove", StringComparison.OrdinalIgnoreCase);
    }
}
