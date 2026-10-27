using System.ComponentModel;
using Exiled.API.Interfaces;

namespace Scp066
{
    public class Config : IConfig
    {
        [Description("Czy plugin jest włączony?")]
        public bool IsEnabled { get; set; } = true;

        [Description("Czy wyświetlać logi debugowania?")]
        public bool Debug { get; set; } = false;

        [Description("Szansa na spawn SCP-066 na początku rundy (0-100%).")]
        public int SpawnChance { get; set; } = 35;

        [Description("Nazwa schematu MapEditorReborn / ProjectMER.")]
        public string SchematicName { get; set; } = "scp066";

        [Description("Promień (w metrach) ataku: dokładnie w tym zasięgu słychać dźwięk i są zadawane obrażenia.")]
        public float AttackRadius { get; set; } = 15f;

        [Description("Obrażenia zadawane co sekundę przez Symfonię Beethovena.")]
        public float Damage { get; set; } = 7f;

        [Description("Czas trwania ataku Beethovena (w sekundach).")]
        public int AttackDuration { get; set; } = 22;

        [Description("Głośność odtwarzanych plików audio.")]
        public float Volume { get; set; } = 100f;

        [Description("Czas odnowienia (cooldown) ataku Beethovena w sekundach.")]
        public float CooldownAttack { get; set; } = 25f;

        [Description("Czas odnowienia (cooldown) dźwięku 'Eric?' w sekundach.")]
        public float CooldownEric { get; set; } = 5f;

        [Description("Czas odnowienia (cooldown) losowych nut w sekundach.")]
        public float CooldownMusic { get; set; } = 8f;
    }
}