using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Scp066;

int passed = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    passed++;
}

var defaults = new Config();
Check(defaults.Normalize().Count == 0, "Valid defaults must not be changed.");
var broken = new Config
{
    Volume = 100f, AttackRadius = float.NaN, Damage = float.PositiveInfinity,
    HumeShield = float.NegativeInfinity, HumeShieldRegenRate = -1,
    HumeShieldRegenDelay = float.NaN, MaxHealth = 0, SpawnChance = 999,
    AttackDuration = -22, CooldownAttack = -25f, CooldownMusic = float.NaN,
    CooldownEric = float.PositiveInfinity, RoleId = 0, SchematicName = " ",
    BeethovenKeybindId = -1, EricKeybindId = 0
};
Check(broken.Normalize().Count >= 16, "Every invalid field should be reported.");
Check(broken.Volume == 1f, "Existing volume:100 must be safe on upgrade.");
Check(broken.AttackRadius == 15f, "NaN must not disable the distance filter.");
Check(float.IsFinite(broken.Damage) && float.IsFinite(broken.HumeShield), "Non-finite values must not reach game stats.");
Check(broken.MaxHealth > 0 && broken.SpawnChance == 100 && broken.AttackDuration > 0, "Numeric limits must be enforced.");
Check(broken.CooldownAttack == 0 && broken.HumeShieldRegenRate == 0, "Negative timers/rates must not run backwards.");
Check(broken.Normalize().Count == 0, "Normalization must be idempotent.");
var disabled = new Config { Volume = 0, HumeShield = 0, HumeShieldRegenRate = 0, SpawnChance = 0, CooldownAttack = 0 };
Check(disabled.Normalize().Count == 0, "Explicitly disabled features must stay disabled.");

string temporary = Path.Combine(Path.GetTempPath(), "scp066-regression-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temporary);
var warnings = new List<string>();
try
{
    // A broken first clip must not prevent loading a later, valid clip.
    File.WriteAllText(Path.Combine(temporary, "Beethoven.ogg"), "not a Vorbis file");
    File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "mono48.ogg"), Path.Combine(temporary, "Eric1.ogg"));
    File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "stereo48.ogg"), Path.Combine(temporary, "Notes1.ogg"));
    File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "mono22.ogg"), Path.Combine(temporary, "Notes2.ogg"));
    using (var oversized = File.Create(Path.Combine(temporary, "Notes3.ogg"))) oversized.SetLength(32L * 1024 * 1024 + 1);

    var foreign = new AudioClipData("Eric1", 48000, 1, new float[480]);
    AudioClipStorage.AudioClips.Add("Eric1", foreign);
    using (var first = new AudioCatalog(warnings.Add))
    using (var second = new AudioCatalog(warnings.Add))
    {
        first.Load(temporary);
        second.Load(temporary);
        Check(first.GetAlias("Beethoven") == null, "Corrupt Vorbis must be rejected.");
        Check(first.GetAlias("Eric1") != null && first.HasClips, "A later valid clip must remain usable.");
        Check(first.GetAlias("Notes1") == null, "Stereo must be rejected.");
        Check(first.GetAlias("Notes2") == null, "Unsupported sample rate must be rejected.");
        Check(first.GetAlias("Notes3") == null, "Oversized files must be rejected before decoding.");
        Check(first.GetAvailable("Eric").SequenceEqual(new[] { "Eric1" }), "Random sounds must only select available clips.");
        string alias = first.GetAlias("Eric1");
        string secondAlias = second.GetAlias("Eric1");
        Check(alias != secondAlias && alias != "Eric1", "Separate owners must use separate aliases.");
        Check(ReferenceEquals(AudioClipStorage.AudioClips["Eric1"], foreign), "Loading must preserve foreign clips.");
        first.Dispose();
        Check(!AudioClipStorage.AudioClips.ContainsKey(alias), "Disposal must remove owned data.");
        Check(second.GetAlias("Eric1") == secondAlias, "Disposal must not affect another instance.");
        Check(ReferenceEquals(AudioClipStorage.AudioClips["Eric1"], foreign), "Disposal must preserve foreign clips.");
        // If a foreign consumer replaces an entry, do not destroy its replacement.
        AudioClipStorage.AudioClips[secondAlias] = foreign;
        Check(second.GetAlias("Eric1") == null, "Replaced data must not be played as our clip.");
        second.Dispose();
        Check(ReferenceEquals(AudioClipStorage.AudioClips[secondAlias], foreign), "Do not delete a foreign replacement.");
        AudioClipStorage.AudioClips.Remove(secondAlias);
    }
    Check(warnings.Any(x => x.Contains("Beethoven")), "Corrupt audio must produce a diagnostic.");
    using (var missing = new AudioCatalog(warnings.Add))
    {
        missing.Load(Path.Combine(temporary, "absent"));
        Check(!missing.HasClips, "Missing audio directory must be handled without throwing.");
    }
    AudioClipStorage.AudioClips.Remove("Eric1");

    AudioClipStorage.AudioClips.Add("probe", new AudioClipData("probe", 48000, 1, Enumerable.Repeat(0.02f, 960).ToArray()));
    var destroy = new List<int>();
    var mixed = AudioClipPlayback.MixPlaybacks(new[] { new AudioClipPlayback(1, "probe", broken.Volume) }, ref destroy);
    Check(mixed.All(x => Math.Abs(x - 0.02f) < 0.00001f), "Repaired legacy volume must not saturate the real audio mixer.");
    AudioClipStorage.AudioClips.Remove("probe");
}
finally { Directory.Delete(temporary, recursive: true); }

Console.WriteLine($"PASS: {passed} regression assertions (production config/catalog and real AudioPlayerApi/NVorbis).");
