using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ThorneTimer
{
    /// <summary>
    /// Controller owning the timer-maintenance domain logic for the Timers tab,
    /// following the established Controller pattern (see
    /// <see cref="CategoriesController"/>, <see cref="StylesController"/>,
    /// <see cref="ViewsController"/>, <see cref="CharactersController"/>).
    ///
    /// Responsibilities owned here:
    ///
    ///  * Timer construction (Add / Duplicate / Chain) returning
    ///    <see cref="Timers.GridData"/> seeds.
    ///  * Dependent-chain naming (Roman-numeral suffixing).
    ///  * Duration validation, including shorthand auto-format
    ///    (e.g. "9:25" -> "00:09:25").
    ///  * Grid maintenance: row creation, save, delete, and per-row save on
    ///    <c>RowValidating</c> — driving <see cref="TimersRepository"/> CRUD and
    ///    <see cref="TimerRuntime"/> registration.
    ///
    /// Cross-cutting side effects that remain owned by <c>FormMain</c> (compact
    /// view, the <c>_allTimers</c> master list / filter versioning, row tinting,
    /// and grid repaint) are injected as delegate hooks rather than referenced
    /// directly, so the controller stays free of the form's filtering and
    /// view-state plumbing.
    /// </summary>
    internal class TimersController
    {
        // Canonical "empty" duration used when seeding a new timer.
        public const string NoTime = "00:00:00";

        // Default seconds inserted between links when extending a dependent
        // timer chain (mirrors the Spawn-series convention of a 5s stagger).
        public const long DefaultChainDelaySeconds = 5;

        private readonly TimersRepository repository;
        private readonly TimerRuntime runtime;
        private DataGridView grid;

        // -----------------------------------------------------------------
        // Cross-cutting hooks supplied by FormMain.  These cover side effects
        // that still belong to the form's view/filter state and are injected
        // so the controller doesn't reach back into FormMain internals.
        // -----------------------------------------------------------------

        /// <summary>Switches the grid out of compact view before a new row is
        /// added so every field is editable.</summary>
        public Action EnsureFullView;

        /// <summary>Adds a freshly created timer to the <c>_allTimers</c> master
        /// list and bumps the filter version so it survives a filter rebuild.</summary>
        public Action<Timers.GridData> TrackTimer;

        /// <summary>Removes a deleted timer from the <c>_allTimers</c> master
        /// list and bumps the filter version.</summary>
        public Action<long> UntrackTimer;

        /// <summary>Applies the runtime row tint for a given row/state.</summary>
        public Action<DataGridViewRow, TimerState> ApplyRowColor;

        /// <summary>Repaints the timer grid after a structural change.</summary>
        public Action RepaintGrid;

        /// <summary>The grid this controller drives (null until Initialize).</summary>
        public DataGridView Grid => grid;

        /// <summary>
        /// Creates the controller.  <paramref name="runtime"/> is used to
        /// register, deregister, stop, and recolor timers as rows are created
        /// and deleted; <paramref name="repository"/> exposes the connection
        /// for the static <see cref="TimersRepository"/> CRUD helpers.
        /// </summary>
        public TimersController(TimersRepository repository, TimerRuntime runtime)
        {
            this.repository = repository;
            this.runtime = runtime;
        }

        // -----------------------------------------------------------------
        // Lifecycle / grid wiring
        // -----------------------------------------------------------------

        /// <summary>
        /// Binds the controller to <paramref name="grid"/> and wires the
        /// per-row save validation.  Column setup and the initial data load
        /// remain in <c>FormMain.SetupTimerGrid</c> for now (they are entangled
        /// with the filter/master-list plumbing); this only owns the
        /// <c>RowValidating</c> save trigger.
        /// </summary>
        public void Initialize(DataGridView grid)
        {
            this.grid = grid;
            UnwireEvents();
            grid.RowValidating += Grid_RowValidating;
        }

        public void Dispose()
        {
            UnwireEvents();
        }

        private void UnwireEvents()
        {
            if (grid == null) return;
            grid.RowValidating -= Grid_RowValidating;
        }

        private void Grid_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            DataGridViewRow row = grid.Rows[e.RowIndex];

            // Row 0 is intentionally excluded to preserve the original
            // FormMain behavior (the first row was never auto-saved here).
            if (ValidateRow(row) && row.Index != 0)
            {
                SaveAll();
            }
        }

        // -----------------------------------------------------------------
        // Grid maintenance: validate / save / create / delete
        // -----------------------------------------------------------------

        /// <summary>
        /// Validates a row's Duration cell (auto-formatting loose input in
        /// place) and reports whether the row is persistable.
        /// </summary>
        public bool ValidateRow(DataGridViewRow row)
        {
            if (row == null || grid == null) return false;
            DataGridViewCell durationCell = row.Cells[grid.Columns["Duration"].Index];
            return ValidateAndNormalizeDurationCell(durationCell);
        }

        /// <summary>
        /// Persists every timer row, then syncs runtime fields from the grid
        /// and repaints.  Mirrors the former <c>FormMain.SaveDataTimers</c>.
        /// </summary>
        public void SaveAll()
        {
            if (grid == null) return;

            for (int r = 0; r < grid.Rows.Count; r++)
            {
                DataGridViewRow row = grid.Rows[r];
                grid.EndEdit();
                TimersRepository.SaveTimer(repository.Con, grid, row);
            }

            runtime.SyncTimerFieldsFromGrid(grid);
            RepaintGrid?.Invoke();
        }

        /// <summary>
        /// Creates a new timer grid row from <paramref name="gd"/> (whose ID
        /// must be -1), persists it to obtain a real DB ID, registers it with
        /// the runtime, applies its row tint, and moves the caret to the Name
        /// cell.  When <paramref name="beginEdit"/> is true the Name cell also
        /// enters edit mode (Add / Duplicate); Chain passes false so repeated
        /// clicks rapidly extend a series.  Shared by Add, Duplicate, and Chain.
        /// </summary>
        public void CreateRow(Timers.GridData gd, bool beginEdit = true)
        {
            if (grid == null) return;

            // Switch to full view if compact so the user can edit all fields.
            EnsureFullView?.Invoke();

            // Add to the existing data source — preserves sort/filter.
            var data = (SortableBindingList<Timers.GridData>)grid.DataSource;
            data.Add(gd);

            // Also add to the master list so the new timer survives any
            // subsequent filter rebuild.  For Add the defaults
            // (ActiveYn=1, ClassID=0) are filter-safe; for Duplicate/Chain the
            // cloned attributes match the (already visible) source row, so the
            // new row stays visible under the current filter.
            TrackTimer?.Invoke(gd);

            // Find the new row (may not be last if a sort is active).
            int newRowIndex = -1;
            for (int r = 0; r < grid.Rows.Count; r++)
            {
                if (Convert.ToInt64(grid.Rows[r].Cells[grid.Columns["ID"].Index].Value) == -1)
                {
                    newRowIndex = r;
                    break;
                }
            }
            if (newRowIndex < 0) newRowIndex = grid.Rows.Count - 1;

            // Save immediately to get a real DB ID.
            DataGridViewRow newRow = grid.Rows[newRowIndex];
            grid.EndEdit();
            TimersRepository.SaveTimer(repository.Con, grid, newRow);

            // Register in the runtime with the real ID.
            runtime.AddTimerState(gd);

            // Apply row color and navigate for editing.
            var ts = runtime.GetState(gd.ID);
            if (ts != null)
                ApplyRowColor?.Invoke(newRow, ts);

            grid.CurrentCell = newRow.Cells[grid.Columns["Name"].Index];
            if (beginEdit)
                grid.BeginEdit(true);
        }

        // -----------------------------------------------------------------
        // Toolbar / context-menu entry points
        // -----------------------------------------------------------------

        /// <summary>Adds a new default timer when the current row is valid.</summary>
        public void AddTimer()
        {
            if (grid == null) return;

            DataGridViewRow row = grid.CurrentRow;
            if (row == null || ValidateRow(row))
            {
                CreateRow(CreateDefaultTimer());
            }
        }

        /// <summary>
        /// Duplicates the selected timer as a standalone copy, opening the Name
        /// cell for editing.  No-ops when the selection is missing or invalid.
        /// </summary>
        public void DuplicateCurrent()
        {
            if (grid == null) return;

            DataGridViewRow row = grid.CurrentRow;
            if (row == null)
                return;

            // Don't clone a row that has an invalid duration; surface the same
            // validation the user would hit on Add.
            if (!ValidateRow(row))
                return;

            if (!(row.DataBoundItem is Timers.GridData source))
                return;

            // The controller produces a full clone (ID=-1, " (copy)" suffix);
            // the Name cell is opened on edit so the user can type over it.
            CreateRow(CreateDuplicate(source));
        }

        /// <summary>
        /// Creates the next link in a dependent timer chain from the selected
        /// timer.  Because the new row becomes the selection, repeated clicks
        /// extend the series: Spawn 20 -> Spawn 20 II -> Spawn 20 III.
        /// </summary>
        public void ChainCurrent()
        {
            if (grid == null) return;

            DataGridViewRow row = grid.CurrentRow;
            if (row == null)
                return;

            if (!ValidateRow(row))
                return;

            if (!(row.DataBoundItem is Timers.GridData source))
                return;

            if (!TryCreateChainLink(source, out Timers.GridData gd))
                return;

            // Don't enter edit mode: the generated name is already correct, so
            // the user can simply click Chain again to add the next link.
            CreateRow(gd, beginEdit: false);
        }

        /// <summary>
        /// Deletes the selected timer after confirmation: stops it, removes its
        /// runtime state, deletes the DB row, drops it from the visible and
        /// master lists, and repaints.
        /// </summary>
        public void DeleteCurrent()
        {
            if (grid?.CurrentCell == null) return;

            if (MessageBox.Show(
                    "Are you sure you want to delete this timer?",
                    "Delete Timer",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1) != DialogResult.Yes)
                return;

            int rowIndex = grid.CurrentCell.RowIndex;
            long timerID = Convert.ToInt64(grid.Rows[rowIndex].Cells[grid.Columns["ID"].Index].Value);

            // Stop this specific timer if running, via TimerRuntime.
            runtime.StopTimer(timerID);

            TimersRepository.DeleteTimer(repository.Con, Convert.ToString(timerID));

            // Remove from existing data source — preserves sort/filter.
            runtime.RemoveTimerState(timerID);
            var data = (SortableBindingList<Timers.GridData>)grid.DataSource;
            var item = data.FirstOrDefault(g => g.ID == timerID);
            if (item != null)
                data.Remove(item);

            // Mirror the deletion in the master list so a later filter rebuild
            // doesn't resurrect the row.
            UntrackTimer?.Invoke(timerID);

            RepaintGrid?.Invoke();
        }

        // -----------------------------------------------------------------
        // Timer construction
        // -----------------------------------------------------------------

        /// <summary>
        /// Builds the seed for a brand-new timer with grid-safe defaults
        /// (ActiveYn=1, ClassID=0) so the row stays visible under the active
        /// filter.  ID is -1 until persisted.
        /// </summary>
        public Timers.GridData CreateDefaultTimer()
        {
            return new Timers.GridData
            {
                ID = -1,
                ActiveYn = 1,
                Style = "Normal",
                Scope = "World",
                DependsOnTimer = "",
                DependsOnDelay = 0,
                ClassID = 0,
                Duration = NoTime
            };
        }

        /// <summary>
        /// Builds a full field-by-field clone of <paramref name="source"/> as
        /// a standalone timer.  ID is reset to -1 and the name is suffixed with
        /// " (copy)" so duplicates are distinguishable in the grid.  Depends-on
        /// settings are copied verbatim (a duplicate, not a chain link).
        /// </summary>
        public Timers.GridData CreateDuplicate(Timers.GridData source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return new Timers.GridData
            {
                ID = -1,
                Name = string.IsNullOrEmpty(source.Name) ? source.Name : source.Name + " (copy)",
                CategoryID = source.CategoryID,
                StartKeyword = source.StartKeyword,
                EndKeyword = source.EndKeyword,
                WAVFile = source.WAVFile,
                Speech = source.Speech,
                Duration = string.IsNullOrEmpty(source.Duration) ? NoTime : source.Duration,
                ActiveYn = source.ActiveYn,
                CaseYn = source.CaseYn,
                EndlessYn = source.EndlessYn,
                Style = string.IsNullOrEmpty(source.Style) ? "Normal" : source.Style,
                Scope = string.IsNullOrEmpty(source.Scope) ? "World" : source.Scope,
                DependsOnTimer = source.DependsOnTimer ?? "",
                DependsOnDelay = source.DependsOnDelay,
                ClassID = source.ClassID
            };
        }

        /// <summary>
        /// Builds the next link in a dependent timer chain from
        /// <paramref name="source"/>.  The new timer copies the source's
        /// keyword/duration/style/etc., appends the next Roman numeral to the
        /// Name (and Speech), points its DependsOnTimer at the source, and
        /// applies <see cref="DefaultChainDelaySeconds"/>.  If the source has
        /// no usable name the method returns false and <paramref name="chained"/>
        /// is null.
        /// </summary>
        public bool TryCreateChainLink(Timers.GridData source, out Timers.GridData chained)
        {
            chained = null;
            if (source == null || string.IsNullOrWhiteSpace(source.Name))
                return false;

            // Work out the base name and the next numeral.  If the source
            // already ends in a Roman numeral (II, III, ...) we continue from
            // it; otherwise it is treated as link I and the new one is II.
            SplitChainName(source.Name, out string baseName, out int currentNumber);
            string nextNumeral = ToRomanNumeral(currentNumber + 1);

            chained = new Timers.GridData
            {
                ID = -1,
                Name = baseName + " " + nextNumeral,
                CategoryID = source.CategoryID,
                StartKeyword = source.StartKeyword,
                EndKeyword = source.EndKeyword,
                WAVFile = source.WAVFile,
                Speech = BuildChainSpeech(source.Speech, baseName, nextNumeral),
                Duration = string.IsNullOrEmpty(source.Duration) ? NoTime : source.Duration,
                ActiveYn = source.ActiveYn,
                CaseYn = source.CaseYn,
                EndlessYn = source.EndlessYn,
                Style = string.IsNullOrEmpty(source.Style) ? "Normal" : source.Style,
                Scope = string.IsNullOrEmpty(source.Scope) ? "World" : source.Scope,
                DependsOnTimer = source.Name,
                DependsOnDelay = DefaultChainDelaySeconds,
                ClassID = source.ClassID
            };
            return true;
        }

        // -----------------------------------------------------------------
        // Duration validation + auto-format
        // -----------------------------------------------------------------

        /// <summary>
        /// Validates the duration cell and, when the value is valid but
        /// loosely formatted (e.g. "9:25"), rewrites it to canonical
        /// "HH:MM:SS" / "DD HH:MM:SS" form in place.  Sets the cell's
        /// <see cref="DataGridViewCell.ErrorText"/> to a helpful message when
        /// the value cannot be parsed.  Returns true when the cell holds (or
        /// now holds) a valid duration.
        /// </summary>
        public bool ValidateAndNormalizeDurationCell(DataGridViewCell durationCell)
        {
            if (durationCell == null) return false;

            string input = Convert.ToString(durationCell.Value) ?? "";

            if (TryNormalizeDuration(input, out string normalized))
            {
                // Auto-format loosely-typed input in place so the grid always
                // shows canonical durations after editing.
                if (!string.Equals(normalized, input, StringComparison.Ordinal))
                    durationCell.Value = normalized;

                durationCell.ErrorText = "";
                return true;
            }

            durationCell.ErrorText =
                "Invalid Duration. Use 'HH:MM:SS' or 'DD HH:MM:SS' (or 'DDd HH:MM:SS'). " +
                "Shorthand like '9:25' is accepted; '9.25' is not.";
            return false;
        }

        /// <summary>
        /// Attempts to normalize a duration string to canonical
        /// "HH:MM:SS" (optionally "DD HH:MM:SS") form.  Accepts colon-delimited
        /// shorthand without leading zeros and with fewer than three segments:
        ///   "25"        -> "00:00:25"
        ///   "9:25"      -> "00:09:25"
        ///   "1:2:3"     -> "01:02:03"
        ///   "2 9:25"    -> "2 00:09:25"
        /// Rejects anything that is not purely integer segments separated by
        /// colons (so "9.25" fails).  Returns false when the input cannot be
        /// interpreted as a duration.
        /// </summary>
        public bool TryNormalizeDuration(string input, out string normalized)
        {
            normalized = null;
            if (input == null) return false;

            string text = input.Trim();
            if (text.Length == 0) return false;

            // Optional day prefix: "DD HH:MM:SS" or "DDd HH:MM:SS".
            string dayToken = null;
            string timePart = text;

            int spaceIdx = text.IndexOf(' ');
            if (spaceIdx > 0)
            {
                string dayPart = text.Substring(0, spaceIdx).TrimEnd('d', 'D');
                if (!TryParseSegment(dayPart, out int days))
                    return false;

                dayToken = days.ToString(CultureInfo.InvariantCulture);
                timePart = text.Substring(spaceIdx + 1).Trim();
                if (timePart.Length == 0)
                    return false;
            }

            string[] parts = timePart.Split(':');
            if (parts.Length < 1 || parts.Length > 3)
                return false;

            int[] values = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!TryParseSegment(parts[i], out values[i]))
                    return false;
            }

            // Map segments from the right: seconds, then minutes, then hours.
            int hours = 0, minutes = 0, seconds = 0;
            switch (parts.Length)
            {
                case 1:
                    seconds = values[0];
                    break;
                case 2:
                    minutes = values[0];
                    seconds = values[1];
                    break;
                default:
                    hours = values[0];
                    minutes = values[1];
                    seconds = values[2];
                    break;
            }

            string time = string.Format(
                CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);

            normalized = dayToken != null ? dayToken + " " + time : time;
            return true;
        }

        /// <summary>
        /// Parses a single duration segment as a non-negative integer.  Uses
        /// <see cref="NumberStyles.None"/> so signs, decimals and whitespace
        /// are rejected (keeping "9.25" out).
        /// </summary>
        private static bool TryParseSegment(string segment, out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(segment)) return false;

            return int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out value)
                   && value >= 0;
        }

        // -----------------------------------------------------------------
        // Dependent-chain naming helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Splits a chain timer name into its base name and current link
        /// number.  A trailing Roman numeral (space-delimited) is parsed into
        /// <paramref name="number"/>; if none is present the name is the base
        /// and the link number is 1.
        /// </summary>
        private static void SplitChainName(string name, out string baseName, out int number)
        {
            baseName = name.Trim();
            number = 1;

            int lastSpace = baseName.LastIndexOf(' ');
            if (lastSpace > 0 && lastSpace < baseName.Length - 1)
            {
                string tail = baseName.Substring(lastSpace + 1);
                int parsed = FromRomanNumeral(tail);
                if (parsed > 0)
                {
                    number = parsed;
                    baseName = baseName.Substring(0, lastSpace).TrimEnd();
                }
            }
        }

        /// <summary>
        /// Builds the Speech text for a new chain link.  Any trailing Roman
        /// numeral on the source Speech is stripped first so successive chain
        /// steps don't accumulate "II III" suffixes.
        /// </summary>
        private static string BuildChainSpeech(string sourceSpeech, string baseName, string nextNumeral)
        {
            if (string.IsNullOrWhiteSpace(sourceSpeech))
                return baseName + " " + nextNumeral;

            string trimmed = sourceSpeech.Trim();
            int lastSpace = trimmed.LastIndexOf(' ');
            if (lastSpace > 0 && lastSpace < trimmed.Length - 1 &&
                FromRomanNumeral(trimmed.Substring(lastSpace + 1)) > 0)
            {
                trimmed = trimmed.Substring(0, lastSpace).TrimEnd();
            }

            return trimmed + " " + nextNumeral;
        }

        /// <summary>
        /// Converts a positive integer to its Roman numeral form.  Chain links
        /// never realistically exceed a couple dozen.
        /// </summary>
        private static string ToRomanNumeral(int number)
        {
            if (number < 1)
                return string.Empty;

            int[] values = { 10, 9, 5, 4, 1 };
            string[] symbols = { "X", "IX", "V", "IV", "I" };

            var sb = new StringBuilder();
            for (int i = 0; i < values.Length && number > 0; i++)
            {
                while (number >= values[i])
                {
                    sb.Append(symbols[i]);
                    number -= values[i];
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parses a Roman numeral back to an integer.  Returns 0 if the token
        /// is not a well-formed Roman numeral, which lets callers distinguish a
        /// numeral suffix from an ordinary word.  Round-trip validation rejects
        /// malformed numerals like "IIII" or "VV".
        /// </summary>
        private static int FromRomanNumeral(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return 0;

            token = token.Trim().ToUpperInvariant();

            int total = 0;
            int prev = 0;
            foreach (char c in token)
            {
                int value;
                switch (c)
                {
                    case 'I': value = 1; break;
                    case 'V': value = 5; break;
                    case 'X': value = 10; break;
                    default: return 0; // not a recognized numeral
                }

                if (value > prev && prev != 0)
                    total += value - 2 * prev; // subtractive (e.g. IV, IX)
                else
                    total += value;

                prev = value;
            }

            return ToRomanNumeral(total) == token ? total : 0;
        }
    }
}
