using System;
using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.DamageHandlers;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp0492;
using MEC;
using Mirror;
using PlayerRoles;
using PlayerRoles.PlayableScps.HumeShield;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using UnityEngine;
using NativeDamageHandler = PlayerStatsSystem.DamageHandlerBase;

namespace Scp066
{
    public class Scp066AbilityComponent : MonoBehaviour
    {
        private Plugin _owner;
        private Scp066Role _role;
        private Player _player;
        private PlayerRoleBase _nativeRole;
        private Config _config;
        private SchematicObject _schematic;
        private AudioPlayer _audioPlayer;
        private Speaker _speaker;
        private AudioClipPlayback _attackPlayback;
        private DynamicHumeShieldController _shieldController;
        private float _oldRegenerationRate;
        private float _oldRegenerationMultiplier;
        private float _oldMaxShield;
        private float _oldShield;
        private PlayerInfoArea _oldInfoArea;
        private bool _shieldConfigured;
        private bool _modelHidden;
        private bool _slownessApplied;
        private bool _subscribed;
        private bool _stopped;
        private bool _isAttackActive;
        private float _cdEric, _cdMusic, _cdAttack;
        private float _hintTimer, _nextFeedbackTime, _nextRegenTime;
        private CoroutineHandle _attackCoroutine;
        private CoroutineHandle _hideModelCoroutine;

        private bool HasSession => !_stopped && _owner != null && ReferenceEquals(Plugin.Instance, _owner)
            && _owner.IsOperational && _player != null && _player.IsConnected && _player.IsAlive
            && ReferenceEquals(_nativeRole, _player.ReferenceHub.roleManager.CurrentRole)
            && _role.Check(_player) && _role.GetComponent(_player) == this;

        internal bool CanAct => HasSession && _owner.IsRoundActive;

        internal void Initialize(Plugin owner, Scp066Role role, Player player)
        {
            _owner = owner;
            _role = role;
            _player = player;
            _config = owner.Config;
            _nativeRole = player.ReferenceHub.roleManager.CurrentRole;
            if (!HasSession) throw new InvalidOperationException("Nieaktualna sesja SCP-066.");

            // Keep the normal stat synchronization path. Suppress only this zombie's
            // native regen rate; the independent regen below honors shield blockers.
            _shieldController = (_nativeRole as IHumeShieldedRole)?.HumeShieldModule as DynamicHumeShieldController;
            _oldMaxShield = player.MaxHumeShield;
            _oldShield = player.HumeShield;
            _oldRegenerationMultiplier = player.HumeShieldRegenerationMultiplier;
            if (_shieldController != null)
            {
                _oldRegenerationRate = _shieldController.RegenerationRate;
                _shieldController.RegenerationRate = 0f;
            }
            _shieldConfigured = true;
            player.HumeShieldRegenerationMultiplier = 1f;
            player.MaxHumeShield = _config.HumeShield;
            player.HumeShield = _config.HumeShield;
            _nextRegenTime = Time.time + _config.HumeShieldRegenDelay;
            _oldInfoArea = player.InfoArea | PlayerInfoArea.Nickname | PlayerInfoArea.Role;

            player.ReferenceHub.playerStats.OnThisPlayerDamaged += OnDamaged;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Scp0492.ConsumingCorpse += OnConsumingCorpse;
            _subscribed = true;
            player.EnableEffect<Slowness>(10, 0f);
            _slownessApplied = true;
            CreateModel();
            CreateAudio();
        }

        private void CreateModel()
        {
            try
            {
                _schematic = ObjectSpawner.SpawnSchematic(_config.SchematicName, _player.Position);
                if (_schematic == null)
                {
                    Log.Warn($"[SCP-066] Brak schematu '{_config.SchematicName}'. Pozostawiono widoczny model zombie.");
                    return;
                }
                _schematic.transform.parent = null;
                _schematic.transform.localScale = Vector3.one;
                _modelHidden = true;
                _player.EnableEffect(EffectType.Fade, 255, 0f);
                _player.InfoArea = (PlayerInfoArea)0;
                _hideModelCoroutine = Timing.CallDelayed(1.5f, () =>
                {
                    if (this == null || !HasSession || _schematic == null || _player.Connection == null) return;
                    foreach (NetworkIdentity identity in _schematic.gameObject.GetComponentsInChildren<NetworkIdentity>())
                        if (identity != null && identity.netId != 0)
                            _player.Connection.Send(new ObjectDestroyMessage { netId = identity.netId });
                });
            }
            catch (Exception e)
            {
                RestoreVisibility();
                Log.Error($"[SCP-066] Błąd modelu; pozostawiono widocznego zombie: {e}");
            }
        }

        private void RestoreVisibility()
        {
            if (!_modelHidden) return;
            _modelHidden = false;
            if (_player != null && _player.IsConnected && ReferenceEquals(_nativeRole, _player.ReferenceHub.roleManager.CurrentRole))
            {
                _player.DisableEffect(EffectType.Fade);
                _player.InfoArea = _oldInfoArea;
            }
        }

        private void CreateAudio()
        {
            if (_owner.Audio == null || !_owner.Audio.HasClips) return;
            try
            {
                _audioPlayer = AudioPlayer.CreateOrGet("SCP066-" + Guid.NewGuid().ToString("N"));
                if (_audioPlayer == null) throw new InvalidOperationException("Brak wolnego odtwarzacza.");
                _speaker = _audioPlayer.AddSpeaker("Main", _player.Position, 1f, true, _config.AttackRadius, _config.AttackRadius + 0.1f);
                if (_speaker == null) throw new InvalidOperationException("Nie udało się utworzyć głośnika.");
                _speaker.transform.parent = null;
            }
            catch (Exception e) { Log.Warn("[SCP-066] Audio niedostępne; obrażenia nadal działają: " + e.Message); }
        }

        private void OnHurting(HurtingEventArgs ev)
        {
            // Only block the base zombie melee attack, not unrelated plugins' damage.
            if (HasSession && ev.Attacker == _player && ev.DamageHandler.Type == DamageType.Scp0492)
            {
                ev.IsAllowed = false;
                ev.Amount = 0f;
            }
        }

        private void OnConsumingCorpse(ConsumingCorpseEventArgs ev)
        {
            if (HasSession && ev.Player == _player) ev.IsAllowed = false;
        }

        private void OnDamaged(NativeDamageHandler handler)
        {
            if (!_stopped) _nextRegenTime = Time.time + _config.HumeShieldRegenDelay;
        }

        private void Update()
        {
            if (_stopped) return;
            if (!HasSession) { Shutdown(); return; }
            if (_schematic != null)
            {
                _schematic.transform.position = _player.Position + new Vector3(0f, -0.55f, 0f);
                _schematic.transform.rotation = Quaternion.Euler(0f, _player.Rotation.eulerAngles.y, 0f);
            }
            else if (_modelHidden)
            {
                RestoreVisibility();
                Log.Warn("[SCP-066] Model usunięto w trakcie gry; przywrócono widocznego zombie.");
            }
            if (_speaker != null) _speaker.transform.position = _player.Position;
            if (!_owner.IsRoundActive) return;

            if (Time.time >= _nextRegenTime && _config.HumeShieldRegenRate > 0f
                && (_shieldController == null || !_shieldController.IsBlocked) && _player.HumeShield < _config.HumeShield)
                _player.HumeShield = Mathf.MoveTowards(_player.HumeShield, _config.HumeShield, _config.HumeShieldRegenRate * Time.deltaTime);

            _cdEric = Mathf.Max(0f, _cdEric - Time.deltaTime);
            _cdMusic = Mathf.Max(0f, _cdMusic - Time.deltaTime);
            if (!_isAttackActive) _cdAttack = Mathf.Max(0f, _cdAttack - Time.deltaTime);
            _hintTimer += Time.deltaTime;
            if (_hintTimer >= 1f) { ShowHints(); _hintTimer = 0f; }
        }

        private AudioClipPlayback TryPlayAudio(string name, bool interrupt = false, bool loop = false)
        {
            try
            {
                if (interrupt && _audioPlayer != null) _audioPlayer.RemoveAllClips();
                string alias = _owner.Audio?.GetAlias(name);
                if (_audioPlayer == null || _speaker == null || alias == null)
                {
                    ShowFeedback("<color=orange>Brak dostępnego audio. Atak działa bez dźwięku.</color>");
                    return null;
                }
                _speaker.transform.position = _player.Position;
                return _audioPlayer.AddClip(alias, _config.Volume, loop: loop);
            }
            catch (Exception e)
            {
                Log.Warn("[SCP-066] Nie udało się odtworzyć audio: " + e.Message);
                return null;
            }
        }

        public void TriggerBeethoven()
        {
            if (!CanAct) return;
            if (_isAttackActive) { ShowFeedback("<color=red>Beethoven już trwa!</color>"); return; }
            if (_cdAttack > 0f) { ShowFeedback($"<color=red>Cooldown Beethoven: {Mathf.CeilToInt(_cdAttack)}s</color>"); return; }
            _isAttackActive = true;
            _cdAttack = _config.CooldownAttack;
            _attackPlayback = TryPlayAudio("Beethoven", interrupt: true, loop: true);
            try
            {
                _attackCoroutine = Timing.RunCoroutine(AttackProcess());
                if (_stopped) Timing.KillCoroutines(_attackCoroutine);
            }
            catch
            {
                FinishAttack();
                throw;
            }
        }

        public void PlayEric() => PlayRandom("Eric", ref _cdEric, _config?.CooldownEric ?? 0f);
        public void PlayMusic() => PlayRandom("Notes", ref _cdMusic, _config?.CooldownMusic ?? 0f);

        private void PlayRandom(string prefix, ref float cooldown, float duration)
        {
            if (!CanAct || _isAttackActive) return;
            if (cooldown > 0f) { ShowFeedback($"<color=red>Cooldown {prefix}: {Mathf.CeilToInt(cooldown)}s</color>"); return; }
            // Throttle missing-audio feedback as well as valid requests.
            cooldown = Mathf.Max(duration, 0.5f);
            string[] available = _owner.Audio?.GetAvailable(prefix) ?? Array.Empty<string>();
            if (available.Length == 0) { ShowFeedback("<color=orange>Brak dostępnych klipów audio.</color>"); return; }
            TryPlayAudio(available[UnityEngine.Random.Range(0, available.Length)], interrupt: true);
        }

        private IEnumerator<float> AttackProcess()
        {
            try
            {
                for (int i = 0; i < _config.AttackDuration && CanAct; i++)
                {
                    foreach (Player target in Player.List.ToArray())
                    {
                        if (!CanAct) yield break;
                        if (target == null || target == _player || !target.IsConnected || !target.IsAlive) continue;
                        if (target.Role.Team == Team.SCPs || target.Role.Type == RoleTypeId.Tutorial) continue;
                        if (target.IsGodModeEnabled || target.IsSpawnProtected || _config.Damage <= 0f) continue;
                        if (Vector3.Distance(_player.Position, target.Position) > _config.AttackRadius) continue;
                        try
                        {
                            PlayerRoleBase hitRole = target.ReferenceHub.roleManager.CurrentRole;
                            var handler = new GenericDamageHandler(target, _player, _config.Damage, DamageType.Custom, null, "SCP-066 (Symphony)", false);
                            // Hurt() discards this result; DealDamage keeps the same
                            // event pipeline and reports cancelled/ineffective hits.
                            bool accepted = target.ReferenceHub.playerStats.DealDamage(handler);
                            if (accepted && target.IsConnected && target.IsAlive
                                && ReferenceEquals(hitRole, target.ReferenceHub.roleManager.CurrentRole))
                                target.EnableEffect(EffectType.Concussed, 2f);
                        }
                        catch (Exception e) { Log.Warn("[SCP-066] Błąd trafienia: " + e.Message); }
                    }
                    yield return Timing.WaitForSeconds(1f);
                }
            }
            finally { FinishAttack(); }
        }

        private void FinishAttack()
        {
            _isAttackActive = false;
            if (_attackPlayback == null) return;
            try { if (_audioPlayer != null) _audioPlayer.RemoveClipById(_attackPlayback.Id); }
            catch (Exception e) { Log.Warn("[SCP-066] Zatrzymanie Beethovena: " + e.Message); }
            _attackPlayback = null;
        }

        private void ShowFeedback(string message)
        {
            if (!CanAct || Time.time < _nextFeedbackTime) return;
            _nextFeedbackTime = Time.time + 0.5f;
            _hintTimer = -1f;
            _player.ShowHint(message, 2f);
        }

        private void ShowHints()
        {
            string attack = _isAttackActive ? "<color=red>GRA</color>" :
                (_cdAttack > 0f ? $"<color=orange>{Mathf.CeilToInt(_cdAttack)}s</color>" : "<color=green>GOTOWY</color>");
            string eric = _cdEric > 0f ? $"{Mathf.CeilToInt(_cdEric)}s" : "OK";
            string music = _cdMusic > 0f ? $"{Mathf.CeilToInt(_cdMusic)}s" : "OK";
            _player.ShowHint($"<align=right><b>SCP-066</b>\nBeethoven: {attack}\nEric: {eric}\nNotes: {music}</align>", 1.5f);
        }

        internal void Shutdown()
        {
            if (_stopped) return;
            _stopped = true;
            if (_subscribed)
            {
                Exiled.Events.Handlers.Player.Hurting -= OnHurting;
                Exiled.Events.Handlers.Scp0492.ConsumingCorpse -= OnConsumingCorpse;
                if (_player?.ReferenceHub != null) _player.ReferenceHub.playerStats.OnThisPlayerDamaged -= OnDamaged;
                _subscribed = false;
            }
            CleanupStep("Anulowanie coroutine", () => Timing.KillCoroutines(_attackCoroutine, _hideModelCoroutine));
            FinishAttack();
            CleanupStep("Przywracanie widoczności", RestoreVisibility);
            if (_player != null && _player.IsConnected && ReferenceEquals(_nativeRole, _player.ReferenceHub.roleManager.CurrentRole))
            {
                if (_slownessApplied) CleanupStep("Usuwanie spowolnienia", () => _player.DisableEffect<Slowness>());
                if (_shieldConfigured)
                {
                    CleanupStep("Przywracanie HS", () =>
                    {
                        if (_shieldController != null) _shieldController.RegenerationRate = _oldRegenerationRate;
                        _player.HumeShieldRegenerationMultiplier = _oldRegenerationMultiplier;
                        _player.MaxHumeShield = _oldMaxShield;
                        _player.HumeShield = _oldShield;
                    });
                }
            }
            if (_schematic != null)
            {
                try { _schematic.Destroy(); }
                catch (Exception e) { Log.Warn("[SCP-066] Usuwanie modelu: " + e.Message); }
            }
            if (_audioPlayer != null)
            {
                try { _audioPlayer.RemoveAllClips(); }
                catch (Exception e) { Log.Warn("[SCP-066] Usuwanie odtwarzanych klipów: " + e.Message); }
                try { _audioPlayer.Destroy(); }
                catch (Exception e) { Log.Warn("[SCP-066] Usuwanie odtwarzacza: " + e.Message); }
            }
        }

        private static void CleanupStep(string operation, Action action)
        {
            try { action(); }
            catch (Exception e) { Log.Warn($"[SCP-066] {operation}: {e.Message}"); }
        }

        private void OnDestroy() => Shutdown();
    }
}
