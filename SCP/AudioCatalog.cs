using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NVorbis;

namespace Scp066
{
    // One catalog per plugin instance; individual players never own shared clip data.
    internal sealed class AudioCatalog : IDisposable
    {
        private readonly string _prefix = "SCP066." + Guid.NewGuid().ToString("N") + ".";
        private readonly Dictionary<string, AudioClipData> _owned = new Dictionary<string, AudioClipData>();
        private readonly Action<string> _warn;
        private const long MaxFileBytes = 32L * 1024 * 1024;
        private const long MaxSamples = 48000L * 300;

        internal AudioCatalog(Action<string> warn) { _warn = warn; }
        internal bool HasClips => _owned.Count > 0;

        internal void Load(string directory)
        {
            if (!Directory.Exists(directory))
            {
                _warn($"Brak folderu audio: {directory}. Atak działa bez dźwięku.");
                return;
            }
            foreach (string name in new[] { "Beethoven", "Eric1", "Eric2", "Eric3", "Notes1", "Notes2", "Notes3", "Notes4", "Notes5", "Notes6" })
                LoadOne(directory, name);
        }

        private void LoadOne(string directory, string name)
        {
            string path = Path.Combine(directory, name + ".ogg");
            string alias = _prefix + name;
            try
            {
                if (!File.Exists(path)) { _warn($"Brak pliku audio: {path}"); return; }
                if (new FileInfo(path).Length > MaxFileBytes) { _warn($"Za duży plik audio (limit 32 MiB): {path}"); return; }
                using (var reader = new VorbisReader(path))
                {
                    if (reader.Channels != 1 || reader.SampleRate != 48000 || reader.TotalSamples <= 0 || reader.TotalSamples > MaxSamples)
                    {
                        _warn($"Nieprawidłowe audio: {path}. Wymagane mono, 48 kHz, długość 0-300 s.");
                        return;
                    }
                }
                if (!AudioClipStorage.LoadClip(path, alias)) { _warn($"Nie udało się załadować: {path}"); return; }
                _owned.Add(name, AudioClipStorage.AudioClips[alias]);
            }
            catch (Exception e) { _warn($"Błąd klipu {path}: {e.Message}"); }
        }

        internal string GetAlias(string name)
        {
            if (!_owned.TryGetValue(name, out var clip)) return null;
            string alias = _prefix + name;
            return AudioClipStorage.AudioClips.TryGetValue(alias, out var current) && ReferenceEquals(current, clip) ? alias : null;
        }

        internal string[] GetAvailable(string prefix) => _owned.Keys
            .Where(x => x.StartsWith(prefix, StringComparison.Ordinal) && GetAlias(x) != null).ToArray();

        public void Dispose()
        {
            foreach (var entry in _owned)
            {
                string alias = _prefix + entry.Key;
                if (AudioClipStorage.AudioClips.TryGetValue(alias, out var current) && ReferenceEquals(current, entry.Value))
                    AudioClipStorage.DestroyClip(alias);
            }
            _owned.Clear();
        }
    }
}
