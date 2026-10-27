using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp0492;
using MEC;
using Mirror;
using PlayerRoles;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Scp066
{
    public class Scp066AbilityComponent : MonoBehaviour
    {
        private Player _player;
        private SchematicObject _schematic;
        private AudioPlayer _audioPlayer;
        private Speaker _speaker;

        private bool _isAttackActive;
        private float _cdEric, _cdMusic, _cdAttack;
        private float _hintTimer;

        private CoroutineHandle _attackCoroutine;
        private CoroutineHandle _initSchematicCoroutine;
        private CoroutineHandle _initAudioCoroutine;

        private readonly string _audioPath = Path.Combine(Paths.Configs, "Plugins", "scp066", "Audio");

        private void Start()
        {
            _player = Player.Get(gameObject);
            if (_player == null || Plugin.Instance == null)
            {
                Destroy(this);
                return;
            }

            try
            {
                _schematic = ObjectSpawner.SpawnSchematic(Plugin.Instance.Config.SchematicName, _player.Position);
                if (_schematic != null)
                {
                    _schematic.transform.parent = null;
                    _schematic.transform.localScale = Vector3.one;

                    _initSchematicCoroutine = Timing.CallDelayed(1.5f, () =>
                    {
                        if (this == null || _player == null || _player.Connection == null || _schematic == null) return;

                        // Ukrywamy wszystkie części schematu przed samym graczem SCP-066, żeby nie zasłaniały mu kamery
                        foreach (NetworkIdentity ni in _schematic.gameObject.GetComponentsInChildren<NetworkIdentity>())
                        {
                            if (ni != null)
                                _player.Connection.Send(new ObjectDestroyMessage { netId = ni.netId });
                        }
                    });
                }
            }
            catch (Exception e)
            {
                Log.Error($"[MER] Błąd schematu: {e}");
            }

            _initAudioCoroutine = Timing.CallDelayed(0.5f, () =>
            {
                if (this == null || _player == null || !_player.IsAlive) return;

                try
                {
                    if (!Directory.Exists(_audioPath))
                    {
                        Log.Warn($"[SCP-066] Brak folderu audio! Utwórz go: {_audioPath}");
                    }
                    else
                    {
                        LoadClipIfExists("Beethoven");
                        LoadClipIfExists("Eric1");
                        LoadClipIfExists("Eric2");
                        LoadClipIfExists("Eric3");
                        LoadClipIfExists("Notes1");
                        LoadClipIfExists("Notes2");
                        LoadClipIfExists("Notes3");
                        LoadClipIfExists("Notes4");
                        LoadClipIfExists("Notes5");
                        LoadClipIfExists("Notes6");
                    }

                    string botId = $"SCP066-{_player.Id}-{UnityEngine.Random.Range(1, 99999)}";
                    float radius = Plugin.Instance.Config.AttackRadius;

                    _audioPlayer = AudioPlayer.CreateOrGet(botId, onIntialCreation: (p) =>
                    {
                        // Zasięg dźwięku 1:1 z obrażeniami:
                        // minDistance = radius (100% głośności w całej strefie obrażeń)
                        // maxDistance = radius + 0.1f (natychmiastowe wyciszenie poza strefą obrażeń)
                        _speaker = p.AddSpeaker("Main", _player.Position, 1f, true, radius, radius + 0.1f);
                        if (_speaker != null)
                            _speaker.transform.parent = null;
                    });

                    if (_speaker != null)
                    {
                        _speaker.IsSpatial = true;
                        _speaker.Volume = 1f;
                        _speaker.MinDistance = radius;
                        _speaker.MaxDistance = radius + 0.1f;
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"[SCP-066] Błąd Inicjalizacji Audio: {e}");
                }
            });

            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Scp0492.ConsumingCorpse += OnConsumingCorpse;
        }

        private void LoadClipIfExists(string alias)
        {
            if (AudioClipStorage.AudioClips.ContainsKey(alias))
                return;

            string path = Path.Combine(_audioPath, alias + ".ogg");
            if (File.Exists(path))
            {
                AudioClipStorage.LoadClip(path, alias);
            }
            else
            {
                Log.Warn($"[SCP-066] Brak pliku audio: {path}");
            }
        }

        private void OnHurting(HurtingEventArgs ev)
        {
            // Blokujemy zwykły atak zombie z ręki, przepuszczamy wyłącznie obrażenia z symfonii (DamageType.Custom)
            if (ev.Attacker == _player && ev.DamageHandler.Type != DamageType.Custom)
            {
                ev.IsAllowed = false;
                ev.Amount = 0f;
            }
        }

        private void OnConsumingCorpse(ConsumingCorpseEventArgs ev)
        {
            // Blokujemy jedzenie ciał przez SCP-066 (mechanika bazowej roli Scp0492)
            if (ev.Player == _player)
            {
                ev.IsAllowed = false;
            }
        }

        private void Update()
        {
            if (_player == null || !_player.IsAlive) return;

            if (_schematic != null)
            {
                _schematic.transform.position = _player.Position + new Vector3(0f, -0.55f, 0f);
                // Obracamy schemat tylko w osi Y (poziomo), żeby nie wbijał się w podłogę przy patrzeniu w dół
                _schematic.transform.rotation = Quaternion.Euler(0f, _player.Rotation.eulerAngles.y, 0f);
            }

            if (_speaker != null)
            {
                _speaker.transform.position = _player.Position;
            }

            if (_cdEric > 0f) _cdEric -= Time.deltaTime;
            if (_cdMusic > 0f) _cdMusic -= Time.deltaTime;

            // Cooldown Beethovena schodzi dopiero po zakończeniu trwania ataku
            if (!_isAttackActive && _cdAttack > 0f)
                _cdAttack -= Time.deltaTime;

            _hintTimer += Time.deltaTime;
            if (_hintTimer >= 1f)
            {
                ShowHints();
                _hintTimer = 0f;
            }
        }

        private bool TryPlayAudio(string alias, bool interruptCurrent = false)
        {
            if (_audioPlayer == null)
            {
                _player.ShowHint("<color=red>System Audio jeszcze się inicjalizuje lub brak plików!</color>", 2f);
                return false;
            }

            if (!AudioClipStorage.AudioClips.ContainsKey(alias))
            {
                _player.ShowHint($"<color=red>Brak klipu audio: {alias}.ogg!</color>", 2f);
                return false;
            }

            if (_speaker != null)
                _speaker.transform.position = _player.Position;

            if (interruptCurrent)
                _audioPlayer.RemoveAllClips();

            _audioPlayer.AddClip(alias, Plugin.Instance.Config.Volume);
            return true;
        }

        public void TriggerBeethoven()
        {
            if (_isAttackActive)
            {
                _player.ShowHint("<color=red>Beethoven już trwa!</color>", 2f);
                return;
            }

            if (_cdAttack > 0f)
            {
                _player.ShowHint($"<color=red>Cooldown Beethoven: {Mathf.CeilToInt(_cdAttack)}s</color>", 2f);
                return;
            }

            TryPlayAudio("Beethoven", interruptCurrent: true);

            _cdAttack = Plugin.Instance.Config.CooldownAttack;
            _attackCoroutine = Timing.RunCoroutine(AttackProcess());
        }

        public void PlayEric()
        {
            if (_isAttackActive) return;

            if (_cdEric > 0f)
            {
                _player.ShowHint($"<color=red>Cooldown Eric: {Mathf.CeilToInt(_cdEric)}s</color>", 2f);
                return;
            }

            if (TryPlayAudio($"Eric{UnityEngine.Random.Range(1, 4)}"))
            {
                _cdEric = Plugin.Instance.Config.CooldownEric;
            }
        }

        public void PlayMusic()
        {
            if (_isAttackActive) return;

            if (_cdMusic > 0f)
            {
                _player.ShowHint($"<color=red>Cooldown Music: {Mathf.CeilToInt(_cdMusic)}s</color>", 2f);
                return;
            }

            if (TryPlayAudio($"Notes{UnityEngine.Random.Range(1, 7)}"))
            {
                _cdMusic = Plugin.Instance.Config.CooldownMusic;
            }
        }

        private IEnumerator<float> AttackProcess()
        {
            _isAttackActive = true;

            int duration = Plugin.Instance.Config.AttackDuration;
            float damage = Plugin.Instance.Config.Damage;
            float radius = Plugin.Instance.Config.AttackRadius;

            for (int i = 0; i < duration; i++)
            {
                if (_player == null || !_player.IsAlive) break;

                foreach (Player target in Player.List.ToList())
                {
                    if (target == null || target == _player || !target.IsAlive) continue;
                    if (target.Role.Team == Team.SCPs || target.Role.Type == RoleTypeId.Tutorial) continue;
                    if (target.IsGodModeEnabled || target.IsSpawnProtected) continue;

                    if (Vector3.Distance(_player.Position, target.Position) > radius) continue;

                    try
                    {
                        target.Hurt(_player, damage, DamageType.Custom, null, "SCP-066 (Symphony)");
                        target.EnableEffect(EffectType.Concussed, 2f);
                    }
                    catch (Exception e)
                    {
                        Log.Warn($"[SCP-066] Błąd przy zadawaniu obrażeń: {e.Message}");
                    }
                }

                yield return Timing.WaitForSeconds(1f);
            }

            _isAttackActive = false;
        }

        private void ShowHints()
        {
            string att = _isAttackActive
                ? "<color=red>GRA</color>"
                : (_cdAttack > 0f ? $"<color=orange>{Mathf.CeilToInt(_cdAttack)}s</color>" : "<color=green>GOTOWY</color>");

            string eric = _cdEric > 0f ? $"<color=orange>{Mathf.CeilToInt(_cdEric)}s</color>" : "<color=green>OK</color>";
            string music = _cdMusic > 0f ? $"<color=orange>{Mathf.CeilToInt(_cdMusic)}s</color>" : "<color=green>OK</color>";

            _player.ShowHint(
                $"<align=right><b>SCP-066</b>\nBeethoven: {att}\nEric: {eric}\nNotes: {music}</align>",
                1.5f
            );
        }

        private void OnDestroy()
        {
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            Exiled.Events.Handlers.Scp0492.ConsumingCorpse -= OnConsumingCorpse;

            Timing.KillCoroutines(_attackCoroutine, _initSchematicCoroutine, _initAudioCoroutine);

            if (_schematic != null)
            {
                try { _schematic.Destroy(); } catch { }
            }

            if (_audioPlayer != null)
            {
                try
                {
                    _audioPlayer.RemoveAllClips();
                    _audioPlayer.Destroy();
                }
                catch { }
            }
        }
    }
}