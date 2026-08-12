using System;
using System.Diagnostics;

namespace ThorneTimer
{
    /// <summary>
    /// Lightweight, opt-in aggregator for log-match performance metrics
    /// (keyword-power-features.md Section 7.1). Accumulates per-chunk timing and
    /// counters across a sliding window and emits a SINGLE PERF summary line
    /// every <see cref="FlushIntervalSeconds"/> seconds, rather than logging
    /// per chunk (per-chunk logging would itself become the bottleneck).
    ///
    /// Phase 1 ("Baseline + harness"): this measures the CURRENT KeywordMatches
    /// path with no behavior change, establishing the before-number every later
    /// tier is compared against.
    ///
    /// Overhead is near-zero: when <see cref="ThorneLog.Enabled"/> is false the
    /// <see cref="Sample"/> path early-returns and no Stopwatch is read. The
    /// accumulators are plain fields guarded by a small lock shared with the
    /// caller's cadence; contention is negligible because samples are coarse
    /// (one per processed chunk, off the UI thread).
    /// </summary>
    internal sealed class MatchStats
    {
        // How often the rolling window is flushed to a PERF line.
        public static double FlushIntervalSeconds = 10.0;

        private readonly object _lock = new object();

        // Rolling window accumulators (reset on each flush).
        private long _chunks;
        private long _matchesFound;     // start/end keyword hits across all timers/categories
        private long _termEvaluations;  // individual KeywordMatches() invocations
        private double _totalMs;
        private double _maxMs;

        private readonly Stopwatch _windowClock = Stopwatch.StartNew();

        // Tier histogram (set once at load; informational in Phase 1). Counts the
        // SHAPE of existing keyword text without changing how it is matched.
        private int _tierLiteral;
        private int _tierWildcard;
        private int _tierRegex;

        /// <summary>
        /// Records one processed chunk. <paramref name="elapsedMs"/> is the wall
        /// time spent matching that chunk; <paramref name="matches"/> is how many
        /// keyword hits fired; <paramref name="evaluations"/> is how many
        /// individual keyword comparisons ran. No-op when logging is disabled, so
        /// the hot path pays nothing in production.
        /// </summary>
        public void Sample(double elapsedMs, long matches, long evaluations)
        {
            if (!ThorneLog.Enabled) return;

            bool flush = false;
            double snapTotal = 0, snapMax = 0;
            long snapChunks = 0, snapMatches = 0, snapEvals = 0;
            double windowSec = 0;

            lock (_lock)
            {
                _chunks++;
                _matchesFound += matches;
                _termEvaluations += evaluations;
                _totalMs += elapsedMs;
                if (elapsedMs > _maxMs) _maxMs = elapsedMs;

                if (_windowClock.Elapsed.TotalSeconds >= FlushIntervalSeconds && _chunks > 0)
                {
                    flush = true;
                    snapTotal = _totalMs;
                    snapMax = _maxMs;
                    snapChunks = _chunks;
                    snapMatches = _matchesFound;
                    snapEvals = _termEvaluations;
                    windowSec = _windowClock.Elapsed.TotalSeconds;

                    _chunks = 0;
                    _matchesFound = 0;
                    _termEvaluations = 0;
                    _totalMs = 0;
                    _maxMs = 0;
                    _windowClock.Restart();
                }
            }

            if (flush)
            {
                double avg = snapChunks > 0 ? snapTotal / snapChunks : 0;
                ThorneLog.Info(
                    $"PERF [match-window]: {snapChunks} chunk(s) in {windowSec:F1}s | " +
                    $"avg {avg:F3} ms, max {snapMax:F3} ms, total {snapTotal:F1} ms | " +
                    $"matches {snapMatches}, evals {snapEvals}");
            }
        }

        /// <summary>
        /// Records the tier histogram captured at timer/category load. Logged once
        /// at Info so a tome's fast-path coverage is visible (e.g.
        /// "literal=120 wildcard=8 regex=3"). Phase 1 classification is read-only:
        /// it inspects existing keyword text shape and does not alter matching.
        /// </summary>
        public void SetTierHistogram(int literal, int wildcard, int regex)
        {
            lock (_lock)
            {
                _tierLiteral = literal;
                _tierWildcard = wildcard;
                _tierRegex = regex;
            }

            if (ThorneLog.Enabled)
                ThorneLog.Info($"PERF [tier-histogram]: literal={literal} wildcard={wildcard} regex={regex}");
        }

        /// <summary>
        /// Emits a final summary of whatever is in the current window. Useful from
        /// the replay benchmark so the last partial window is not lost.
        /// </summary>
        public void FlushNow(string context = null)
        {
            if (!ThorneLog.Enabled) return;

            long snapChunks; double snapTotal, snapMax; long snapMatches, snapEvals; double windowSec;
            lock (_lock)
            {
                snapChunks = _chunks;
                snapTotal = _totalMs;
                snapMax = _maxMs;
                snapMatches = _matchesFound;
                snapEvals = _termEvaluations;
                windowSec = _windowClock.Elapsed.TotalSeconds;

                _chunks = 0;
                _matchesFound = 0;
                _termEvaluations = 0;
                _totalMs = 0;
                _maxMs = 0;
                _windowClock.Restart();
            }

            if (snapChunks == 0) return;
            double avg = snapTotal / snapChunks;
            string tag = string.IsNullOrEmpty(context) ? "match-final" : "match-final:" + context;
            ThorneLog.Info(
                $"PERF [{tag}]: {snapChunks} chunk(s) in {windowSec:F1}s | " +
                $"avg {avg:F3} ms, max {snapMax:F3} ms, total {snapTotal:F1} ms | " +
                $"matches {snapMatches}, evals {snapEvals}");
        }
    }
}