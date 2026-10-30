using System;
using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Interfaces;

namespace Scp066
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
        [Description("Szansa na spawn na początku rundy (0-100%).")]
        public int SpawnChance { get; set; } = 35;
        public int MaxHealth { get; set; } = 2000;
        [Description("Początkowe i maksymalne HS. To Hume Shield, nie AHP.")]
        public float HumeShield { get; set; } = 500f;
        [Description("Niezależna regeneracja HS na sekundę; 0 wyłącza regenerację.")]
        public float HumeShieldRegenRate { get; set; } = 10f;
        [Description("Opóźnienie regeneracji HS po otrzymaniu obrażeń (sekundy).")]
        public float HumeShieldRegenDelay { get; set; } = 10f;
        public string SchematicName { get; set; } = "scp066";
        [Description("Promień w metrach. Dźwięk i obrażenia przenikają ściany.")]
        public float AttackRadius { get; set; } = 15f;
        public float Damage { get; set; } = 7f;
        public int AttackDuration { get; set; } = 22;
        [Description("Mnożnik głośności 0-1; 1 oznacza 100%, nie 100.")]
        public float Volume { get; set; } = 0.8f;
        [Description("Cooldown liczony dopiero od zakończenia ataku.")]
        public float CooldownAttack { get; set; } = 25f;
        public float CooldownEric { get; set; } = 5f;
        public float CooldownMusic { get; set; } = 8f;
        [Description("Globalne ID CustomRole; zmień w razie konfliktu z innym pluginem.")]
        public uint RoleId { get; set; } = 660;
        public int BeethovenKeybindId { get; set; } = 6601;
        public int EricKeybindId { get; set; } = 6602;
        public int MusicKeybindId { get; set; } = 6603;

        // Validate before roles or input are registered. Repairs existing volume: 100 YAMLs too.
        public IReadOnlyList<string> Normalize()
        {
            var changes = new List<string>();
            SpawnChance = NormalizeInt(nameof(SpawnChance), SpawnChance, 0, 100, changes);
            MaxHealth = NormalizeInt(nameof(MaxHealth), MaxHealth, 1, 65535, changes);
            HumeShield = NormalizeFloat(nameof(HumeShield), HumeShield, 0f, 65535f, 500f, changes);
            HumeShieldRegenRate = NormalizeFloat(nameof(HumeShieldRegenRate), HumeShieldRegenRate, 0f, 500f, 10f, changes);
            HumeShieldRegenDelay = NormalizeFloat(nameof(HumeShieldRegenDelay), HumeShieldRegenDelay, 0f, 300f, 10f, changes);
            AttackRadius = NormalizeFloat(nameof(AttackRadius), AttackRadius, 0.1f, 100f, 15f, changes);
            Damage = NormalizeFloat(nameof(Damage), Damage, 0f, 1000f, 7f, changes);
            AttackDuration = NormalizeInt(nameof(AttackDuration), AttackDuration, 1, 300, changes);
            Volume = NormalizeFloat(nameof(Volume), Volume, 0f, 1f, 0.8f, changes);
            CooldownAttack = NormalizeFloat(nameof(CooldownAttack), CooldownAttack, 0f, 3600f, 25f, changes);
            CooldownEric = NormalizeFloat(nameof(CooldownEric), CooldownEric, 0f, 3600f, 5f, changes);
            CooldownMusic = NormalizeFloat(nameof(CooldownMusic), CooldownMusic, 0f, 3600f, 8f, changes);
            BeethovenKeybindId = NormalizeInt(nameof(BeethovenKeybindId), BeethovenKeybindId, 1, int.MaxValue, changes);
            EricKeybindId = NormalizeInt(nameof(EricKeybindId), EricKeybindId, 1, int.MaxValue, changes);
            MusicKeybindId = NormalizeInt(nameof(MusicKeybindId), MusicKeybindId, 1, int.MaxValue, changes);
            if (RoleId == 0) { RoleId = 660; changes.Add("RoleId: 0 -> 660"); }
            if (string.IsNullOrWhiteSpace(SchematicName)) { SchematicName = "scp066"; changes.Add("SchematicName: empty -> scp066"); }
            return changes;
        }

        private static int NormalizeInt(string name, int value, int min, int max, List<string> changes)
        {
            int result = Math.Max(min, Math.Min(max, value));
            if (result != value) changes.Add($"{name}: {value} -> {result}");
            return result;
        }

        private static float NormalizeFloat(string name, float value, float min, float max, float fallback, List<string> changes)
        {
            float result = float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value));
            if (result != value) changes.Add($"{name}: {value} -> {result}");
            return result;
        }
    }
}
