using System;
using System.IO;
using System.Linq;
using Exiled.API.Features;
using Exiled.CustomRoles.API;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace Scp066
{
    public class Plugin : Plugin<Config>
    {
        public static Plugin Instance { get; private set; }
        public override string Name => "Scp066";
        public override string Prefix => "scp066";
        public override Version Version => new Version(2, 0, 1);
        public override Version RequiredExiledVersion => new Version(9, 13, 2);

        internal Scp066Role Role { get; private set; }
        internal AudioCatalog Audio { get; private set; }
        internal bool IsOperational => _enabled && Role != null && CustomRole.Registered.Contains(Role);
        internal bool IsRoundActive => _roundActive;
        private bool _enabled;
        private bool _roundActive;
        private int _roundGeneration;
        private CoroutineHandle _spawnCoroutine;
        private CoroutineHandle _teleportCoroutine;
        private ServerSpecificSettingBase[] _settings = Array.Empty<ServerSpecificSettingBase>();

        public override void OnEnabled()
        {
            if (_enabled) return;
            if (Instance != null && !ReferenceEquals(Instance, this))
            {
                Log.Error("[SCP-066] Inna instancja pluginu jest już aktywna.");
                return;
            }
            Instance = this;
            try
            {
                foreach (string correction in Config.Normalize()) Log.Warn("[SCP-066] Config: " + correction);
                var ids = new[] { Config.BeethovenKeybindId, Config.EricKeybindId, Config.MusicKeybindId };
                var existing = ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>();
                if (ids.Distinct().Count() != ids.Length || existing.Any(x => x != null && ids.Contains(x.SettingId)))
                    throw new InvalidOperationException("Konflikt ID keybindów. Ustaw unikalne *_keybind_id w konfiguracji.");
                if (CustomRole.Registered.Any(r => r.Id == Config.RoleId || r.Name == "SCP-066" || r is Scp066Role))
                    throw new InvalidOperationException("Konflikt ID lub nazwy CustomRole SCP-066.");

                Role = new Scp066Role { Owner = this, Id = Config.RoleId, MaxHealth = Config.MaxHealth };
                Role.Register();
                if (!CustomRole.Registered.Contains(Role))
                    throw new InvalidOperationException("CustomRoles nie zarejestrował SCP-066. Sprawdź, czy moduł jest włączony.");

                Audio = new AudioCatalog(message => Log.Warn("[SCP-066] " + message));
                Audio.Load(Path.Combine(Paths.Configs, "Plugins", "scp066", "Audio"));
                _settings = new ServerSpecificSettingBase[]
                {
                    new SSGroupHeader("SCP-066"),
                    new SSKeybindSetting(Config.BeethovenKeybindId, "Atak: Beethoven", KeyCode.None, allowSpectatorTrigger: false, hint: "Atak obszarowy"),
                    new SSKeybindSetting(Config.EricKeybindId, "Dźwięk: Eric", KeyCode.None, allowSpectatorTrigger: false),
                    new SSKeybindSetting(Config.MusicKeybindId, "Dźwięk: Notes", KeyCode.None, allowSpectatorTrigger: false)
                };
                ServerSpecificSettingsSync.DefinedSettings = existing.Concat(_settings).ToArray();
                ServerSpecificSettingsSync.ServerOnSettingValueReceived += ProcessUserInput;
                Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
                Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;
                Exiled.Events.Handlers.Server.RestartingRound += OnRoundReset;
                Exiled.Events.Handlers.Server.WaitingForPlayers += OnRoundReset;
                _roundActive = Round.IsStarted && !Round.IsEnded;
                _enabled = true;
                ServerSpecificSettingsSync.SendToAll();
                base.OnEnabled();
            }
            catch (Exception e)
            {
                Log.Error("[SCP-066] Nie udało się włączyć pluginu: " + e);
                Cleanup();
            }
        }

        public override void OnDisabled()
        {
            Cleanup();
            base.OnDisabled();
        }

        private static void Safely(string operation, Action action)
        {
            try { action(); }
            catch (Exception e) { Log.Error($"[SCP-066] {operation}: {e}"); }
        }

        private void Cleanup()
        {
            _enabled = false;
            _roundActive = false;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;
            Exiled.Events.Handlers.Server.RestartingRound -= OnRoundReset;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnRoundReset;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= ProcessUserInput;
            Safely("Anulowanie callbacków", CancelPending);
            Safely("Usuwanie keybindów", () =>
            {
                var current = ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>();
                ServerSpecificSettingsSync.DefinedSettings = current.Where(x => !_settings.Any(own => ReferenceEquals(own, x))).ToArray();
                _settings = Array.Empty<ServerSpecificSettingBase>();
                ServerSpecificSettingsSync.SendToAll();
            });
            RemoveOwnPlayers(resetZombie: true);
            if (Role != null && CustomRole.Registered.Contains(Role))
                Safely("Wyrejestrowanie SCP-066", () => Role.Unregister());
            Role = null;
            Safely("Usuwanie klipów audio", () => Audio?.Dispose());
            Audio = null;
            if (ReferenceEquals(Instance, this)) Instance = null;
        }

        private void RemoveOwnPlayers(bool resetZombie)
        {
            if (Role == null) return;
            foreach (Player player in Role.TrackedPlayers.ToArray())
            {
                Safely("Sprzątanie postaci", () =>
                {
                    Role.GetComponent(player)?.Shutdown();
                    Role.RemoveRole(player);
                    // On unload, do not leave an ordinary zombie with the custom HP.
                    if (resetZombie && player.IsConnected && player.IsAlive && player.Role.Type == RoleTypeId.Scp0492)
                        player.Role.Set(RoleTypeId.Scp0492);
                });
            }
        }

        private void ProcessUserInput(ReferenceHub sender, ServerSpecificSettingBase setting)
        {
            if (!IsOperational || !_roundActive || sender == null || !(setting is SSKeybindSetting key) || !key.SyncIsPressed) return;
            Player player = Player.Get(sender);
            if (player == null || !Role.Check(player)) return;
            Scp066AbilityComponent component = Role.GetComponent(player);
            if (component == null || !component.CanAct) return;
            if (setting.SettingId == Config.BeethovenKeybindId) component.TriggerBeethoven();
            else if (setting.SettingId == Config.EricKeybindId) component.PlayEric();
            else if (setting.SettingId == Config.MusicKeybindId) component.PlayMusic();
        }

        private void CancelPending()
        {
            ++_roundGeneration;
            Timing.KillCoroutines(_spawnCoroutine, _teleportCoroutine);
            _spawnCoroutine = default(CoroutineHandle);
            _teleportCoroutine = default(CoroutineHandle);
        }

        private bool IsCurrentRound(int generation) =>
            IsOperational && _roundActive && generation == _roundGeneration && ReferenceEquals(Instance, this);

        private void OnRoundStarted()
        {
            CancelPending();
            _roundActive = true;
            int generation = _roundGeneration;
            _spawnCoroutine = Timing.CallDelayed(3f, () =>
            {
                if (!IsCurrentRound(generation) || UnityEngine.Random.Range(1, 101) > Config.SpawnChance) return;
                var pool = Player.List.Where(p => p.IsConnected && p.IsAlive && p.Role.Team == Team.SCPs
                    && p.Role.Type != RoleTypeId.Scp079 && !p.HasAnyCustomRole()).ToList();
                if (pool.Count == 0) return;
                Player selected = pool[UnityEngine.Random.Range(0, pool.Count)];
                Role.AddRole(selected);
                Scp066AbilityComponent session = Role.GetComponent(selected);
                if (session == null) return;
                _teleportCoroutine = Timing.CallDelayed(0.8f, () =>
                {
                    if (!IsCurrentRound(generation) || session == null || !session.CanAct || Role.GetComponent(selected) != session) return;
                    Room room = Room.List.FirstOrDefault(r => r.Type == Exiled.API.Enums.RoomType.HczArmory);
                    if (room == null)
                    {
                        var rooms = Room.List.Where(r => r.Zone == Exiled.API.Enums.ZoneType.HeavyContainment).ToList();
                        if (rooms.Count != 0) room = rooms[UnityEngine.Random.Range(0, rooms.Count)];
                    }
                    if (room != null) selected.Teleport(room.Position + Vector3.up * 2.5f);
                });
            });
        }

        private void OnRoundEnded(RoundEndedEventArgs ev)
        {
            _roundActive = false;
            CancelPending();
            if (Role == null) return;
            foreach (Player player in Role.TrackedPlayers.ToArray())
                Safely("Zakończenie sesji", () => Role.GetComponent(player)?.Shutdown());
        }

        private void OnRoundReset()
        {
            _roundActive = false;
            CancelPending();
            RemoveOwnPlayers(resetZombie: false);
        }
    }
}
