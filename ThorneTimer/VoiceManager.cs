using System;
using System.Data.SQLite;
using System.Speech.Synthesis;
using System.Windows.Forms;

namespace ThorneTimer
{
    /// <summary>
    /// Manages SAPI text-to-speech voice selection, rate, and volume.
    /// Owns the installed-voice enumeration that populates the voice
    /// combo box, persists selection changes to the <c>settings</c>
    /// table, and provides a test-speech helper.
    ///
    /// FormMain still owns the actual speech requests issued from timer
    /// events; this manager only handles the user-facing settings tab
    /// controls and exposes <see cref="ActiveVoice"/> / <see cref="Rate"/> /
    /// <see cref="Volume"/> for those callers.
    /// </summary>
    internal class VoiceManager
    {
        private readonly SQLiteConnection con;
        private readonly ComboBox voiceCombo;

        public VoiceManager(SQLiteConnection con, ComboBox voiceCombo)
        {
            this.con = con ?? throw new ArgumentNullException(nameof(con));
            this.voiceCombo = voiceCombo ?? throw new ArgumentNullException(nameof(voiceCombo));
        }

        /// <summary>Currently selected SAPI voice name (empty = system default).</summary>
        public string ActiveVoice { get; private set; } = "";

        /// <summary>Speech rate (-10 fastest .. 10 slowest, SAPI convention).</summary>
        public int Rate { get; private set; } = -2;

        /// <summary>Speech volume (0 .. 100).</summary>
        public int Volume { get; private set; } = 100;

        /// <summary>
        /// Loads persisted voice settings from the database.  Call this
        /// before <see cref="PopulateVoiceCombo"/> so the saved selection
        /// can be applied to the combo box.
        /// </summary>
        public void LoadFromDatabase()
        {
            ActiveVoice = Database.GetSetting(con, "ActiveVoice") ?? "";
            Volume = SafeParseInt(Database.GetSetting(con, "VoiceVolume"), 100);
            Rate = SafeParseInt(Database.GetSetting(con, "VoiceRate"), -2);
        }

        /// <summary>
        /// Enumerates installed SAPI voices (English only) into the voice
        /// combo box and selects the persisted ActiveVoice if available.
        /// Logs detailed diagnostic information for voice discovery issues.
        /// </summary>
        public void PopulateVoiceCombo()
        {
            ThorneLog.Info("VoiceManager.PopulateVoiceCombo: starting voice setup");
            voiceCombo.Items.Clear();

            try
            {
                using (var synthesizer = new SpeechSynthesizer())
                {
                    var installed = synthesizer.GetInstalledVoices();
                    ThorneLog.Info($"VoiceManager: enumerating {installed.Count} installed voice(s)");

                    int added = 0;
                    foreach (InstalledVoice voice in installed)
                    {
                        try
                        {
                            VoiceInfo info = voice.VoiceInfo;
                            string cultureName = info.Culture != null ? info.Culture.Name : "<null>";
                            string cultureTwo = info.Culture != null ? info.Culture.TwoLetterISOLanguageName : "<null>";
                            ThorneLog.Debug($"VoiceManager: found voice Name='{info.Name}', Id='{info.Id}', Description='{info.Description}', Culture='{cultureName}'");

                            bool isEnglish = info.Culture != null && info.Culture.TwoLetterISOLanguageName == "en";
                            if (isEnglish)
                            {
                                voiceCombo.Items.Add(info.Name);
                                added++;
                                ThorneLog.Info($"VoiceManager: added voice '{info.Name}' (English)");
                            }
                            else
                            {
                                ThorneLog.Debug($"VoiceManager: skipped voice '{info.Name}' (not English: {cultureTwo})");
                            }
                        }
                        catch (Exception exVoice)
                        {
                            ThorneLog.Warn($"VoiceManager: error processing a voice: {exVoice.Message}");
                        }
                    }

                    ThorneLog.Info($"VoiceManager: completed enumeration. Added {added} English voice(s) to combo box");
                }
            }
            catch (Exception ex)
            {
                ThorneLog.Error($"VoiceManager: failed to enumerate installed voices: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(ActiveVoice) && voiceCombo.Items.Contains(ActiveVoice))
            {
                voiceCombo.SelectedItem = ActiveVoice;
                ThorneLog.Info($"VoiceManager: selected saved voice '{ActiveVoice}'");
            }
            else
            {
                if (!string.IsNullOrEmpty(ActiveVoice))
                    ThorneLog.Warn($"VoiceManager: saved voice '{ActiveVoice}' not found in current list");
                voiceCombo.SelectedItem = ActiveVoice;
            }
        }

        /// <summary>
        /// Updates and persists the active voice selection.
        /// Returns true if the selection actually changed.
        /// </summary>
        public bool SetActiveVoice(string voice)
        {
            string newVoice = voice ?? "";
            if (newVoice == ActiveVoice) return false;

            ActiveVoice = newVoice;
            Database.SetSetting(con, "ActiveVoice", ActiveVoice);
            return true;
        }

        /// <summary>Updates and persists the speech volume.</summary>
        public void SetVolume(int volume)
        {
            Volume = volume;
            Database.SetSetting(con, "VoiceVolume", Volume);
        }

        /// <summary>Updates and persists the speech rate.</summary>
        public void SetRate(int rate)
        {
            Rate = rate;
            Database.SetSetting(con, "VoiceRate", Rate);
        }

        /// <summary>
        /// Speaks a short test phrase using the current voice/rate/volume
        /// settings.  Used by the "Test" button on the Settings tab.
        /// </summary>
        public void SpeakTest()
        {
            using (var synth = new SpeechSynthesizer())
            {
                if (!string.IsNullOrEmpty(ActiveVoice))
                {
                    synth.SelectVoice(ActiveVoice);
                }

                synth.SetOutputToDefaultAudioDevice();
                synth.Rate = Rate;
                synth.Volume = Volume;
                synth.Speak("Test");
            }
        }

        private static int SafeParseInt(string value, int defaultValue)
        {
            return int.TryParse(value, out int result) ? result : defaultValue;
        }
    }
}
