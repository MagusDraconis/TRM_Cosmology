using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;

namespace TRM.CMD
{
    // ── Data Models ───────────────────────────────────────────────

    /// <summary>Per-channel analysis result.</summary>
    public class ChannelResult
    {
        public string Name { get; set; } = string.Empty;
        public double PeakFrequencyHz { get; set; }
        public double BandPower { get; set; }
        public double[] Phases { get; set; } = Array.Empty<double>();
    }

    /// <summary>Top-level EEG analysis summary.</summary>
    public class EegAnalysisResult
    {
        public double SamplingRateHz { get; set; }
        public double DurationSec { get; set; }
        public List<ChannelResult> Channels { get; set; } = new();
        public double MeanOmegaHz { get; set; }
        public double SigmaOmegaHz { get; set; }
        public double OmegaStarHz { get; set; }
        public double GlobalPlv { get; set; }
        public double CentroidError { get; set; }
        public bool IsSynchronized { get; set; }
        public string Interpretation { get; set; } = string.Empty;

        // ── Corrected (mode-based, noise-excluded) ──────────────

        public double OmegaModeHz { get; set; }
        public double ClusterConsistencyHz { get; set; }
        public double CentroidErrorCorrected { get; set; }
        public string InterpretationCorrected { get; set; } = string.Empty;
    }

    /// <summary>Synchronisation state label.</summary>
    public enum SyncState { Low, Mid, High }

    /// <summary>One window of sliding-window analysis.</summary>
    public class SlidingWindowPoint
    {
        public double TimeSec { get; set; }
        public double OmegaStarHz { get; set; }
        public double OmegaModeHz { get; set; }
        public double Plv { get; set; }
        public double ClusterSigmaHz { get; set; }
        public double SigmaOmegaHz { get; set; }
        public double Ratio { get; set; }           // PLV / σ_ω  (K/σ proxy)
        public SyncState State { get; set; }        // clustered state
        public int? FreqClusterId { get; set; }    // nearest Ω* peak cluster index
    }

    // ── EEG Analyser ──────────────────────────────────────────────

    /// <summary>
    /// Processes Muse-2 EEG CSV data and extracts TRM/TQM-relevant
    /// features: local oscillator frequencies ω_i, collective
    /// frequency Ω*, synchronisation metric (PLV), and coupling proxy.
    ///
    /// Implements FFT-based band-pass filtering, Hilbert phase
    /// extraction, and pairwise phase-locking value computation.
    /// </summary>
    public static class NeuralEEGAnalyzer
    {
        // ── File Paths ─────────────────────────────────────────────

        private static string EegDir => Path.GetFullPath(
            Path.Combine(LaserDataPath.InputDir, "..", "EEG"));

        public static string ExpPath =>
            Path.Combine(EegDir, "EEG_01_H_EXP_2024-10-31-12_25_25.csv");

        public static string ControlPath =>
            Path.Combine(EegDir, "EEG_01_H_Control_2024-11-15-13_01_49.csv");

        private static string OutputDir
        {
            get
            {
                string d = Path.Combine(EegDir, "Output");
                Directory.CreateDirectory(d);
                return d;
            }
        }

        private static string PlotsDir
        {
            get
            {
                string d = Path.Combine(EegDir, "Plots");
                Directory.CreateDirectory(d);
                return d;
            }
        }

        // ── CSV Loader ─────────────────────────────────────────────

        /// <summary>
        /// Loads a Muse-2 EEG CSV (timestamps, TP9, AF7, AF8, TP10, Marker0).
        /// Returns (time, channelData, markers).
        /// </summary>
        public static (double[] time, Dictionary<string, double[]> channels, double[] markers)
            LoadEeg(string path)
        {
            var lines = File.ReadAllLines(path);
            var header = lines[0].Split(',');

            int n = lines.Length - 1;
            var time = new double[n];
            var tp9 = new double[n];
            var af7 = new double[n];
            var af8 = new double[n];
            var tp10 = new double[n];
            var markers = new double[n];

            for (int i = 0; i < n; i++)
            {
                var parts = lines[i + 1].Split(',');
                time[i] = double.Parse(parts[0].Trim(), CultureInfo.InvariantCulture);
                tp9[i] = double.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
                af7[i] = double.Parse(parts[2].Trim(), CultureInfo.InvariantCulture);
                af8[i] = double.Parse(parts[3].Trim(), CultureInfo.InvariantCulture);
                tp10[i] = double.Parse(parts[4].Trim(), CultureInfo.InvariantCulture);
                markers[i] = parts.Length > 5 && double.TryParse(parts[5].Trim(),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out double m)
                    ? m : double.NaN;
            }

            var channels = new Dictionary<string, double[]>
            {
                ["TP9"] = tp9,
                ["AF7"] = af7,
                ["AF8"] = af8,
                ["TP10"] = tp10
            };

            return (time, channels, markers);
        }

        // ── FFT (radix-2, in-place) ────────────────────────────────

        /// <summary>
        /// Radix-2 Cooley–Tukey FFT.  n must be a power of 2.
        /// Returns frequency-domain Complex array.
        /// </summary>
        public static Complex[] Fft(double[] signal)
        {
            int n = signal.Length;
            // Zero-pad to next power of 2
            int n2 = 1;
            while (n2 < n) n2 <<= 1;

            var complex = new Complex[n2];
            for (int i = 0; i < n; i++)
                complex[i] = new Complex(signal[i], 0);

            FftInPlace(complex);
            return complex;
        }

        private static void FftInPlace(Complex[] buffer)
        {
            int n = buffer.Length;
            // Bit-reversal permutation
            int j = 0;
            for (int i = 0; i < n; i++)
            {
                if (i < j)
                    (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
                int m = n >> 1;
                while (m >= 1 && (j & m) != 0) { j ^= m; m >>= 1; }
                j ^= m;
            }

            // Danielson–Lanczos
            for (int len = 2; len <= n; len <<= 1)
            {
                double angle = -2.0 * Math.PI / len;
                var wlen = new Complex(Math.Cos(angle), Math.Sin(angle));
                for (int i = 0; i < n; i += len)
                {
                    var w = Complex.One;
                    int half = len >> 1;
                    for (int k = 0; k < half; k++)
                    {
                        var u = buffer[i + k];
                        var v = buffer[i + k + half] * w;
                        buffer[i + k] = u + v;
                        buffer[i + k + half] = u - v;
                        w *= wlen;
                    }
                }
            }
        }

        /// <summary>Inverse FFT.</summary>
        public static double[] Ifft(Complex[] spectrum)
        {
            int n = spectrum.Length;

            // Conjugate → FFT → conjugate → scale
            for (int i = 0; i < n; i++)
                spectrum[i] = new Complex(spectrum[i].Real, -spectrum[i].Imaginary);

            FftInPlace(spectrum);

            var result = new double[n];
            for (int i = 0; i < n; i++)
                result[i] = spectrum[i].Real / n;

            return result;
        }

        // ── Band-Pass Filter (FFT-based) ───────────────────────────

        /// <summary>
        /// Applies a rectangular band-pass filter in the frequency domain
        /// by zeroing bins outside [lowHz, highHz], then inverse-FFTs.
        /// </summary>
        public static double[] BandPassFilter(double[] signal, double fs,
            double lowHz = 1.0, double highHz = 100.0)
        {
            int nOrig = signal.Length;
            var spectrum = Fft(signal);
            int n = spectrum.Length;

            for (int i = 0; i < n; i++)
            {
                double freq = i <= n / 2 ? i * fs / n : (n - i) * fs / n;
                if (freq < lowHz || freq > highHz)
                    spectrum[i] = Complex.Zero;
            }

            double[] filtered = Ifft(spectrum);
            // Trim back to original length
            var result = new double[nOrig];
            Array.Copy(filtered, result, nOrig);
            return result;
        }

        // ── Peak Frequency ─────────────────────────────────────────

        /// <summary>
        /// Finds the dominant frequency (peak in magnitude spectrum)
        /// within [lowHz, highHz].
        /// </summary>
        public static double FindPeakFrequency(double[] signal, double fs,
            double lowHz = 1.0, double highHz = 100.0)
        {
            var spectrum = Fft(signal);
            int n = spectrum.Length;
            double maxMag = 0;
            int maxIdx = 0;

            for (int i = 1; i <= n / 2; i++)
            {
                double freq = i * fs / n;
                if (freq < lowHz || freq > highHz) continue;

                double mag = spectrum[i].Magnitude;
                if (mag > maxMag)
                {
                    maxMag = mag;
                    maxIdx = i;
                }
            }

            return maxMag > 0 ? maxIdx * fs / n : double.NaN;
        }

        // ── Power-Weighted Frequency ────────────────────────────────

        /// <summary>
        /// Computes the power-weighted mean frequency for a signal,
        /// excluding the noise band [notchLowHz, notchHighHz] from the
        /// power computation.  Returns NaN if total power is zero.
        /// </summary>
        public static double ComputePowerWeightedFrequency(double[] signal, double fs,
            double lowHz = 1.0, double highHz = 4.0,
            double notchLowHz = 45.0, double notchHighHz = 60.0)
        {
            var spectrum = Fft(signal);
            int n = spectrum.Length;
            int half = n / 2;

            double num = 0, den = 0;
            for (int i = 1; i < half; i++)
            {
                double freq = i * fs / n;
                if (freq < lowHz || freq > highHz)
                    continue;
                if (freq >= notchLowHz && freq <= notchHighHz)
                    continue;

                double power = spectrum[i].Magnitude * spectrum[i].Magnitude;
                num += freq * power;
                den += power;
            }

            return den > 1e-30 ? num / den : double.NaN;
        }

        /// <summary>
        /// Returns the raw magnitude spectrum (frequencies + power)
        /// for a signal, trimmed to the Nyquist band.
        /// </summary>
        public static (double[] freqs, double[] power) ComputePowerSpectrum(
            double[] signal, double fs)
        {
            var spectrum = Fft(signal);
            int n = spectrum.Length;
            int half = n / 2;

            var freqs = new double[half];
            var power = new double[half];
            for (int i = 0; i < half; i++)
            {
                freqs[i] = i * fs / n;
                power[i] = spectrum[i].Magnitude * spectrum[i].Magnitude;
            }

            return (freqs, power);
        }

        // ── Hilbert Transform (FFT-based) ──────────────────────────

        /// <summary>
        /// Computes instantaneous phase via the analytic signal.
        /// H[s] = IFFT( H_hat(ω) · S(ω) ) where H_hat = 0 for ω&lt;0,
        /// 1 for ω=0, 2 for ω&gt;0.
        /// </summary>
        public static double[] HilbertPhase(double[] signal)
        {
            var spectrum = Fft(signal);
            int n = spectrum.Length;
            int half = n / 2;

            // Analytic signal: zero negative freqs, double positive freqs
            for (int i = 1; i < half; i++)
                spectrum[i] *= 2;
            for (int i = half + 1; i < n; i++)
                spectrum[i] = Complex.Zero;

            double[] analytic = Ifft(spectrum);

            var phase = new double[signal.Length];
            for (int i = 0; i < signal.Length; i++)
                phase[i] = Math.Atan2(analytic[i], signal[i]);

            return phase;
        }

        // ── Phase-Locking Value (PLV) ──────────────────────────────

        /// <summary>
        /// Computes the pairwise Phase-Locking Value:
        ///   PLV = |⟨ exp(i·(θ₁ − θ₂)) ⟩|
        /// </summary>
        public static double ComputePlv(double[] phase1, double[] phase2)
        {
            int n = Math.Min(phase1.Length, phase2.Length);
            var sum = Complex.Zero;

            for (int i = 0; i < n; i++)
            {
                double delta = phase1[i] - phase2[i];
                sum += new Complex(Math.Cos(delta), Math.Sin(delta));
            }

            return (sum / n).Magnitude;
        }

        /// <summary>
        /// Global PLV = mean of all unique pairwise PLVs.
        /// </summary>
        public static double ComputeGlobalPlv(Dictionary<string, double[]> phases)
        {
            var names = phases.Keys.ToArray();
            double sum = 0;
            int count = 0;

            for (int i = 0; i < names.Length; i++)
                for (int j = i + 1; j < names.Length; j++)
                {
                    sum += ComputePlv(phases[names[i]], phases[names[j]]);
                    count++;
                }

            return count > 0 ? sum / count : double.NaN;
        }

        // ── Statistics ─────────────────────────────────────────────

        public static double StdDev(IEnumerable<double> values)
        {
            var list = values.ToList();
            double mean = list.Average();
            double variance = list.Sum(v => (v - mean) * (v - mean)) / list.Count;
            return Math.Sqrt(variance);
        }

        // ── Full Pipeline ──────────────────────────────────────────

        /// <summary>
        /// Runs the complete EEG analysis pipeline on the experimental
        /// recording and returns a populated result object.
        /// </summary>
        public static EegAnalysisResult Analyze(string? filePath = null,
            double bandLow = 1.0, double bandHigh = 80.0)
        {
            filePath ??= ExpPath;
            var result = new EegAnalysisResult();

            // Step 1 — Load
            var (time, channels, _) = LoadEeg(filePath);

            // Step 2 — Sampling rate
            double dt = 0;
            for (int i = 1; i < time.Length; i++)
                dt += time[i] - time[i - 1];
            dt /= (time.Length - 1);
            double fs = 1.0 / dt;

            result.SamplingRateHz = fs;
            result.DurationSec = time[^1] - time[0];

            // Step 3-4 — Filter + FFT peak per channel
            var phases = new Dictionary<string, double[]>();
            var omegas = new List<double>();

            foreach (var kvp in channels)
            {
                string name = kvp.Key;
                double[] raw = kvp.Value;

                // Band-pass filter
                double[] filtered = BandPassFilter(raw, fs, bandLow, bandHigh);

                // Peak frequency
                double peakHz = FindPeakFrequency(filtered, fs, bandLow, bandHigh);
                omegas.Add(peakHz);

                // Hilbert phase
                double[] phase = HilbertPhase(filtered);
                phases[name] = phase;

                // Band power (crude: RMS of filtered signal)
                double rms = Math.Sqrt(filtered.Sum(v => v * v) / filtered.Length);

                result.Channels.Add(new ChannelResult
                {
                    Name = name,
                    PeakFrequencyHz = peakHz,
                    BandPower = rms,
                    Phases = phase
                });
            }

            // Step 5 — Mean + sigma
            result.MeanOmegaHz = omegas.Average();
            result.SigmaOmegaHz = StdDev(omegas);

            // Step 6-7 — PLV
            result.GlobalPlv = ComputeGlobalPlv(phases);

            // Step 8 — Ω* (option B: FFT of averaged signal)
            int minLen = channels.Values.Min(c => c.Length);
            var avgSignal = new double[minLen];
            int nCh = channels.Count;
            foreach (var c in channels.Values)
                for (int i = 0; i < minLen; i++)
                    avgSignal[i] += c[i] / nCh;
            result.OmegaStarHz = FindPeakFrequency(avgSignal, fs, bandLow, bandHigh);

            // Step 10 — TRM tests
            result.CentroidError = Math.Abs(result.OmegaStarHz - result.MeanOmegaHz);
            result.IsSynchronized = result.GlobalPlv > 0.5;

            // Interpretation
            result.Interpretation = BuildInterpretation(result);

            // ── Corrected analysis (delta band + power-weighted) ──
            RunCorrectedAnalysis(result, channels, fs);

            return result;
        }

        /// <summary>
        /// Runs the mode-based corrected comparison:
        /// Ω* (global FFT peak) vs Omega_mode (median of per-channel
        /// dominant peaks).  Populates the Corrected fields.
        /// </summary>
        private static void RunCorrectedAnalysis(
            EegAnalysisResult result,
            Dictionary<string, double[]> channels,
            double fs)
        {
            // Build the set of dominant peak frequencies per channel
            var dominantOmegas = result.Channels
                .Select(ch => ch.PeakFrequencyHz)
                .Where(f => !double.IsNaN(f))
                .OrderBy(f => f)
                .ToList();

            if (dominantOmegas.Count == 0)
            {
                result.OmegaModeHz = double.NaN;
                result.ClusterConsistencyHz = double.NaN;
                result.CentroidErrorCorrected = double.NaN;
                result.InterpretationCorrected = "No valid channel peaks.";
                return;
            }

            // Median as the robust mode
            int mid = dominantOmegas.Count / 2;
            result.OmegaModeHz = dominantOmegas.Count % 2 == 1
                ? dominantOmegas[mid]
                : (dominantOmegas[mid - 1] + dominantOmegas[mid]) / 2.0;

            // Cluster consistency = std of dominant peaks
            result.ClusterConsistencyHz = StdDev(dominantOmegas);

            // New centroid test: |Ω* − Omega_mode|
            result.CentroidErrorCorrected =
                Math.Abs(result.OmegaStarHz - result.OmegaModeHz);

            // Corrected interpretation
            result.InterpretationCorrected = BuildCorrectedInterpretation(result);
        }

        // ── Interpretation ─────────────────────────────────────────

        private static string BuildInterpretation(EegAnalysisResult r)
        {
            var parts = new List<string>();
            var ci = CultureInfo.InvariantCulture;

            // C1 — Centroid
            double relError = r.MeanOmegaHz > 1e-6
                ? r.CentroidError / r.MeanOmegaHz : r.CentroidError;
            if (relError < 0.15)
                parts.Add(string.Format(ci,
                    "C1 CENTROID: error = {0:F1}% — Ω* tracks mean(ω_i) closely. PASS.",
                    relError * 100));
            else
                parts.Add(string.Format(ci,
                    "C1 CENTROID: error = {0:F1}% — Ω* deviates from mean(ω_i). WEAK.",
                    relError * 100));

            // C2 — Synchrony
            if (r.GlobalPlv > 0.7)
                parts.Add(string.Format(ci,
                    "C2 SYNCHRONY: PLV = {0:F3} — strong phase locking. PASS.", r.GlobalPlv));
            else if (r.GlobalPlv > 0.5)
                parts.Add(string.Format(ci,
                    "C2 SYNCHRONY: PLV = {0:F3} — moderate synchronisation. PASS.", r.GlobalPlv));
            else
                parts.Add(string.Format(ci,
                    "C2 SYNCHRONY: PLV = {0:F3} — weak synchronisation. WEAK.", r.GlobalPlv));

            // Overall
            if (relError < 0.15 && r.GlobalPlv > 0.5)
                parts.Add("OVERALL: Neural data supports TRM/TQM centroid + synchrony predictions.");
            else
                parts.Add("OVERALL: Partial support. Try analysing the control recording for comparison.");

            return string.Join("  ", parts);
        }

        /// <summary>Interpretation for the mode-based corrected analysis.</summary>
        private static string BuildCorrectedInterpretation(EegAnalysisResult r)
        {
            var parts = new List<string>();
            var ci = CultureInfo.InvariantCulture;

            double relError = r.OmegaModeHz > 1e-6
                ? r.CentroidErrorCorrected / r.OmegaModeHz
                : r.CentroidErrorCorrected;

            bool pass = relError < 0.15 && r.ClusterConsistencyHz < 2.0;

            if (pass)
                parts.Add(string.Format(ci,
                    "C1c MODE CENTROID: error = {0:F1}%, cluster σ = {1:F2} Hz — Ω* ≈ channel-peak mode. STRONG PASS.",
                    relError * 100, r.ClusterConsistencyHz));
            else if (relError < 0.15)
                parts.Add(string.Format(ci,
                    "C1c MODE CENTROID: error = {0:F1}% (PASS), but cluster σ = {1:F2} Hz (wide). PARTIAL.",
                    relError * 100, r.ClusterConsistencyHz));
            else if (r.ClusterConsistencyHz < 2.0)
                parts.Add(string.Format(ci,
                    "C1c MODE CENTROID: error = {0:F1}% (WEAK), but cluster σ = {1:F2} Hz (tight). PARTIAL.",
                    relError * 100, r.ClusterConsistencyHz));
            else
                parts.Add(string.Format(ci,
                    "C1c MODE CENTROID: error = {0:F1}%, cluster σ = {1:F2} Hz — both weak. WEAK.",
                    relError * 100, r.ClusterConsistencyHz));

            if (pass && r.GlobalPlv > 0.5)
                parts.Add("OVERALL CORRECTED: Mode centroid + tight cluster + synchrony all pass.");
            else if (pass)
                parts.Add("OVERALL CORRECTED: Mode centroid + tight cluster pass. Synchrony weak (resting-state).");
            else
                parts.Add("OVERALL CORRECTED: Partial support. One or both mode-centroid conditions fail.");

            return string.Join("  ", parts);
        }

        // ── Console Output ─────────────────────────────────────────

        public static void PrintSummary(EegAnalysisResult r)
        {
            var ci = CultureInfo.InvariantCulture;

            Console.WriteLine("  ╔══════════════════════════════════════════╗");
            Console.WriteLine("  ║  EEG ANALYSIS — TRM/TQM VALIDATION       ║");
            Console.WriteLine("  ╚══════════════════════════════════════════╝");
            Console.WriteLine();

            Console.WriteLine(string.Format(ci,
                "  Sampling rate  : {0:F1} Hz", r.SamplingRateHz));
            Console.WriteLine(string.Format(ci,
                "  Duration       : {0:F1} s", r.DurationSec));
            Console.WriteLine();

            Console.WriteLine("  ── CHANNEL FREQUENCIES (ω_i) ──");
            foreach (var ch in r.Channels)
                Console.WriteLine(string.Format(ci,
                    "    {0,-6} : peak = {1,7:F2} Hz   (RMS = {2:F2} µV)",
                    ch.Name, ch.PeakFrequencyHz, ch.BandPower));
            Console.WriteLine();

            Console.WriteLine(string.Format(ci,
                "  mean ω_i  : {0:F2} Hz", r.MeanOmegaHz));
            Console.WriteLine(string.Format(ci,
                "  sigma ω_i : {0:F3} Hz", r.SigmaOmegaHz));
            Console.WriteLine();

            Console.WriteLine(string.Format(ci,
                "  Ω* (collective) : {0:F2} Hz", r.OmegaStarHz));
            Console.WriteLine(string.Format(ci,
                "  Global PLV      : {0:F4}", r.GlobalPlv));
            Console.WriteLine();

            Console.WriteLine("  ── TRM/TQM TESTS ──");
            Console.WriteLine(string.Format(ci,
                "  C1 centroid error  : {0:F2} Hz  ({1:F1}%)",
                r.CentroidError,
                r.MeanOmegaHz > 1e-6 ? r.CentroidError / r.MeanOmegaHz * 100 : 0));
            Console.WriteLine(string.Format(ci,
                "  C2 synchronised    : {0}  (PLV > 0.5)", r.IsSynchronized));
            Console.WriteLine();

            // ── Corrected analysis section ──
            Console.WriteLine("  ── CORRECTED COMPARISON (mode-based) ──");
            Console.WriteLine(string.Format(ci,
                "  Ω* (global peak)  : {0:F2} Hz", r.OmegaStarHz));
            Console.WriteLine(string.Format(ci,
                "  Ω_mode (median)   : {0:F2} Hz", r.OmegaModeHz));
            Console.WriteLine(string.Format(ci,
                "  cluster σ          : {0:F3} Hz", r.ClusterConsistencyHz));
            Console.WriteLine(string.Format(ci,
                "  C1c mode error    : {0:F3} Hz  ({1:F1}%)",
                r.CentroidErrorCorrected,
                r.OmegaModeHz > 1e-6
                    ? r.CentroidErrorCorrected / r.OmegaModeHz * 100 : 0));
            Console.WriteLine();

            Console.WriteLine("  ── INTERPRETATION ──");
            Console.WriteLine("  " + r.Interpretation);
            Console.WriteLine();
            Console.WriteLine("  ── CORRECTED INTERPRETATION ──");
            Console.WriteLine("  " + r.InterpretationCorrected);
        }

        // ── CSV Output ─────────────────────────────────────────────

        public static string SaveSummaryCsv(EegAnalysisResult r)
        {
            string path = Path.Combine(OutputDir, "eeg_summary.csv");
            var ci = CultureInfo.InvariantCulture;

            using var writer = new StreamWriter(path);
            writer.WriteLine("channel,omega_hz");
            foreach (var ch in r.Channels)
                writer.WriteLine(string.Format(ci, "{0},{1:F2}",
                    ch.Name, ch.PeakFrequencyHz));

            writer.WriteLine(string.Format(ci, "mean_omega,{0:F2},", r.MeanOmegaHz));
            writer.WriteLine(string.Format(ci, "omega_mode,{0:F2},", r.OmegaModeHz));
            writer.WriteLine(string.Format(ci, "sigma_omega,{0:F3},", r.SigmaOmegaHz));
            writer.WriteLine(string.Format(ci, "cluster_consistency,{0:F3},", r.ClusterConsistencyHz));
            writer.WriteLine(string.Format(ci, "Omega_star,{0:F2},", r.OmegaStarHz));
            writer.WriteLine(string.Format(ci, "global_plv,{0:F4},", r.GlobalPlv));
            writer.WriteLine(string.Format(ci, "centroid_error,{0:F2},", r.CentroidError));
            writer.WriteLine(string.Format(ci, "centroid_error_mode,{0:F3},", r.CentroidErrorCorrected));
            writer.WriteLine(string.Format(ci, "is_synchronized,{0},", r.IsSynchronized));

            Console.WriteLine("  Summary CSV saved: " + path);
            return path;
        }

        // ── Plotting ───────────────────────────────────────────────

        /// <summary>
        /// Plots FFT magnitude spectra for all channels on one canvas.
        /// </summary>
        public static string PlotSpectra(EegAnalysisResult r, string filePath,
            double fs, double bandLow = 1.0, double bandHigh = 80.0)
        {
            var (_, channels, _) = LoadEeg(filePath);
            var plt = new ScottPlot.Plot();

            string[] colors = { "#1f77b4", "#ff7f0e", "#2ca02c", "#d62728" };
            int ci = 0;

            foreach (var kvp in channels)
            {
                double[] signal = kvp.Value;
                var spectrum = Fft(signal);
                int n = spectrum.Length;
                int half = n / 2;

                var freqs = new double[half];
                var mags = new double[half];
                for (int i = 1; i < half; i++)
                {
                    freqs[i] = i * fs / n;
                    mags[i] = spectrum[i].Magnitude;
                }

                var sc = plt.Add.Scatter(freqs, mags);
                sc.LegendText = kvp.Key;
                sc.LineWidth = 1f;
                sc.Color = ScottPlot.Color.FromHex(colors[ci % colors.Length]);
                ci++;
            }

            plt.Title("EEG FFT Magnitude Spectra");
            plt.XLabel("Frequency (Hz)");
            plt.YLabel("Magnitude");
            plt.Axes.SetLimits(0f, (float)Math.Min(bandHigh * 1.2, 120f), 0f, float.NaN);
            plt.ShowLegend();

            string pngPath = Path.Combine(PlotsDir, "spectra.png");
            plt.SavePng(pngPath, 1200, 800);
            Console.WriteLine("  Spectra plot saved: " + pngPath);
            return pngPath;
        }

        /// <summary>
        /// Plots phase differences over time for one channel pair.
        /// </summary>
        public static string PlotPhaseDiff(EegAnalysisResult r,
            string ch1 = "AF7", string ch2 = "AF8",
            int maxPoints = 2000)
        {
            var c1 = r.Channels.FirstOrDefault(c => c.Name == ch1);
            var c2 = r.Channels.FirstOrDefault(c => c.Name == ch2);
            if (c1 == null || c2 == null) return string.Empty;

            int n = Math.Min(c1.Phases.Length, c2.Phases.Length);
            int step = Math.Max(1, n / maxPoints);

            var xs = new List<double>();
            var ys = new List<double>();
            for (int i = 0; i < n; i += step)
            {
                xs.Add(i / r.SamplingRateHz);
                double diff = c1.Phases[i] - c2.Phases[i];
                // Wrap to [-π, π]
                diff = ((diff + Math.PI) % (2 * Math.PI)) - Math.PI;
                ys.Add(diff);
            }

            var plt = new ScottPlot.Plot();
            var sc = plt.Add.Scatter(xs.ToArray(), ys.ToArray());
            sc.LineWidth = 0.5f;
            sc.MarkerSize = 0f;

            plt.Title(string.Format("Phase Difference: {0} − {1}", ch1, ch2));
            plt.XLabel("Time (s)");
            plt.YLabel("Δθ (rad)");
            plt.Axes.SetLimits(0f, (float)(n / r.SamplingRateHz), -3.2f, 3.2f);

            string pngPath = Path.Combine(PlotsDir, "phase_diff.png");
            plt.SavePng(pngPath, 1200, 600);
            Console.WriteLine("  Phase-diff plot saved: " + pngPath);
            return pngPath;
        }

        /// <summary>
        /// Plots a 4×4 PLV matrix heatmap.
        /// </summary>
        public static string PlotPlvMatrix(EegAnalysisResult r)
        {
            var names = r.Channels.Select(c => c.Name).ToArray();
            int nCh = names.Length;

            var plt = new ScottPlot.Plot();
            var xs = new List<double>();
            var ys = new List<double>();
            var cols = new List<ScottPlot.Color>();
            for (int i = 0; i < nCh; i++)
            {
                for (int j = 0; j < nCh; j++)
                {
                    double plv;
                    if (i == j)
                        plv = 1.0;
                    else
                    {
                        var pi = r.Channels[i].Phases;
                        var pj = r.Channels[j].Phases;
                        plv = ComputePlv(pi, pj);
                    }

                    xs.Add(j);
                    ys.Add(nCh - 1 - i);   // flip Y so top row = channel 0

                    // Interpolate blue (low) → red (high)
                    byte rCol = (byte)(plv * 255);
                    byte bCol = (byte)((1 - plv) * 255);
                    cols.Add(new ScottPlot.Color(rCol, 0, bCol));
                }
            }

            // One scatter per cell for per-point colour
            for (int k = 0; k < xs.Count; k++)
            {
                var pt = plt.Add.Scatter(new[] { xs[k] }, new[] { ys[k] });
                pt.MarkerSize = 40f;
                pt.LineWidth = 0f;
                pt.Color = cols[k];
            }

            // Channel labels
            double[] tickPositions = Enumerable.Range(0, nCh).Select(i => (double)i).ToArray();
            // ScottPlot 5 tick labels via Axes
            plt.Axes.SetLimits(-0.8f, (float)(nCh - 0.2f), -0.8f, (float)(nCh - 0.2f));

            plt.Title("PLV Matrix (pairwise phase locking)");

            string pngPath = Path.Combine(PlotsDir, "plv_matrix.png");
            plt.SavePng(pngPath, 800, 800);
            Console.WriteLine("  PLV matrix plot saved: " + pngPath);
            return pngPath;
        }

        // ── Sliding-Window Transition Detection ────────────────────

        /// <summary>
        /// Runs sliding-window analysis on the experimental recording.
        /// Windows: 5 s, step: 1 s.  Per window: Ω*, Ω_mode, PLV.
        /// </summary>
        public static List<SlidingWindowPoint> SlidingWindowAnalyze(
            string? filePath = null,
            double windowSec = 5.0,
            double stepSec = 1.0,
            double bandLow = 1.0,
            double bandHigh = 80.0)
        {
            filePath ??= ExpPath;
            var (time, channels, _) = LoadEeg(filePath);

            // Sampling rate
            double dt = (time[^1] - time[0]) / (time.Length - 1);
            double fs = 1.0 / dt;

            int windowSamples = (int)(windowSec * fs);
            int stepSamples = (int)(stepSec * fs);
            int totalSamples = time.Length;

            var windowPoints = new List<SlidingWindowPoint>();
            var names = channels.Keys.ToArray();

            for (int start = 0; start + windowSamples <= totalSamples; start += stepSamples)
            {
                double tCenter = time[start + windowSamples / 2];

                // Extract window per channel
                var windowData = new Dictionary<string, double[]>();
                foreach (var name in names)
                {
                    var segment = new double[windowSamples];
                    Array.Copy(channels[name], start, segment, 0, windowSamples);

                    // Band-pass filter
                    segment = BandPassFilter(segment, fs, bandLow, bandHigh);
                    windowData[name] = segment;
                }

                // Per-channel peak frequencies
                var peaks = new List<double>();
                var phases = new Dictionary<string, double[]>();
                foreach (var name in names)
                {
                    double peak = FindPeakFrequency(windowData[name], fs, bandLow, bandHigh);
                    peaks.Add(peak);
                    phases[name] = HilbertPhase(windowData[name]);
                }

                // Ω* from averaged signal in window
                int minLen = windowData.Values.Min(c => c.Length);
                var avgSignal = new double[minLen];
                foreach (var seg in windowData.Values)
                    for (int i = 0; i < minLen; i++)
                        avgSignal[i] += seg[i] / names.Length;
                double omegaStar = FindPeakFrequency(avgSignal, fs, bandLow, bandHigh);

                // Ω_mode = median of channel peaks
                var sorted = peaks.Where(p => !double.IsNaN(p)).OrderBy(p => p).ToList();
                double omegaMode = sorted.Count > 0
                    ? (sorted.Count % 2 == 1
                        ? sorted[sorted.Count / 2]
                        : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2.0)
                    : double.NaN;

                // PLV
                double plv = ComputeGlobalPlv(phases);

                // Cluster sigma = σ_ω
                double clusterSigma = StdDev(sorted);
                double sigmaOmega = clusterSigma;
                double ratio = sigmaOmega > 1e-6 ? plv / sigmaOmega : double.NaN;

                windowPoints.Add(new SlidingWindowPoint
                {
                    TimeSec = tCenter,
                    OmegaStarHz = omegaStar,
                    OmegaModeHz = omegaMode,
                    Plv = plv,
                    ClusterSigmaHz = clusterSigma,
                    SigmaOmegaHz = sigmaOmega,
                    Ratio = ratio
                });
            }

            return windowPoints;
        }

        /// <summary>
        /// Detects transitions where PLV rises rapidly.
        /// Returns (index, startTime, endTime, deltaPLV) for each candidate.
        /// </summary>
        public static List<(int index, double time, double deltaPlv)> DetectTransitions(
            List<SlidingWindowPoint> windows,
            double plvRiseThreshold = 0.05)
        {
            var transitions = new List<(int, double, double)>();

            for (int i = 1; i < windows.Count; i++)
            {
                double delta = windows[i].Plv - windows[i - 1].Plv;
                if (delta > plvRiseThreshold)
                    transitions.Add((i, windows[i].TimeSec, delta));
            }

            return transitions;
        }

        /// <summary>
        /// Saves the sliding-window time series as CSV.
        /// </summary>
        public static string SaveSlidingWindowCsv(
            List<SlidingWindowPoint> windows)
        {
            string path = Path.Combine(OutputDir, "sliding_window.csv");
            var ci = CultureInfo.InvariantCulture;

            using var writer = new StreamWriter(path);
            writer.WriteLine("time_s,Omega_star_Hz,Omega_mode_Hz,PLV,sigma_omega_Hz,ratio");
            foreach (var w in windows)
                writer.WriteLine(string.Format(ci, "{0:F2},{1:F2},{2:F2},{3:F4},{4:F3},{5:F4}",
                    w.TimeSec, w.OmegaStarHz, w.OmegaModeHz, w.Plv, w.SigmaOmegaHz, w.Ratio));

            Console.WriteLine("  Sliding-window CSV saved: " + path);
            return path;
        }

        /// <summary>
        /// Plots PLV and Ω* vs time on a dual-axis plot.
        /// </summary>
        public static string PlotSlidingWindow(
            List<SlidingWindowPoint> windows,
            List<(int index, double time, double deltaPlv)>? transitions = null)
        {
            var plt = new ScottPlot.Plot();

            var times = windows.Select(w => w.TimeSec).ToArray();
            var plvs = windows.Select(w => w.Plv).ToArray();
            var omegas = windows.Select(w => w.OmegaStarHz).ToArray();

            // PLV on left axis
            var plvScatter = plt.Add.Scatter(times, plvs);
            plvScatter.LegendText = "PLV";
            plvScatter.Color = ScottPlot.Color.FromHex("#1f77b4");
            plvScatter.LineWidth = 1.5f;
            plvScatter.MarkerSize = 0f;
            plt.YLabel("PLV");

            // Ω* on right axis
            var omegaAxis = plt.Axes.AddRightAxis();
            var omegaScatter = plt.Add.Scatter(times, omegas);
            omegaScatter.LegendText = "Ω* (Hz)";
            omegaScatter.Color = ScottPlot.Color.FromHex("#d62728");
            omegaScatter.LineWidth = 1f;
            omegaScatter.MarkerSize = 0f;
            omegaScatter.Axes.YAxis = omegaAxis;
            omegaAxis.LabelText = "Ω* (Hz)";

            // Mark transitions
            if (transitions != null)
            {
                foreach (var (_, t, _) in transitions)
                {
                    var hl = plt.Add.VerticalLine(t);
                    hl.Color = ScottPlot.Color.FromHex("#ff7f0e");
                    hl.LineWidth = 1f;
                    hl.LinePattern = ScottPlot.LinePattern.Dashed;
                }
            }

            plt.Title("Sliding-Window EEG: PLV & Ω* vs Time");
            plt.XLabel("Time (s)");
            plt.ShowLegend();

            string pngPath = Path.Combine(PlotsDir, "sliding_window.png");
            plt.SavePng(pngPath, 1400, 700);
            Console.WriteLine("  Sliding-window plot saved: " + pngPath);
            return pngPath;
        }

        /// <summary>
        /// Analyses PLV vs K/σ ratio to detect Kuramoto-like threshold behaviour.
        /// Returns (thresholdRatio, correlationR).
        /// </summary>
        public static (double thresholdRatio, double correlationR)
            AnalyzeThreshold(List<SlidingWindowPoint> windows)
        {
            var valid = windows
                .Where(w => !double.IsNaN(w.Ratio) && !double.IsInfinity(w.Ratio))
                .ToList();

            if (valid.Count < 10)
                return (double.NaN, double.NaN);

            // Pearson correlation: PLV vs ratio
            int n = valid.Count;
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0, sumY2 = 0;
            foreach (var w in valid)
            {
                double x = w.Ratio;
                double y = w.Plv;
                sumX += x; sumY += y;
                sumXY += x * y;
                sumX2 += x * x; sumY2 += y * y;
            }
            double corr = (n * sumXY - sumX * sumY) /
                Math.Sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));

            // Find ratio where PLV crosses 0.2–0.3 band
            double thresholdRatio = double.NaN;
            var sorted = valid.OrderBy(w => w.Ratio).ToList();
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i].Plv >= 0.2 && sorted[i - 1].Plv < 0.2)
                {
                    double t = (0.2 - sorted[i - 1].Plv) / (sorted[i].Plv - sorted[i - 1].Plv);
                    thresholdRatio = sorted[i - 1].Ratio + t * (sorted[i].Ratio - sorted[i - 1].Ratio);
                    break;
                }
            }

            return (thresholdRatio, corr);
        }

        /// <summary>
        /// Plots PLV vs K/σ ratio with a fitted trend to visualise threshold behaviour.
        /// </summary>
        public static string PlotThresholdCurve(List<SlidingWindowPoint> windows)
        {
            var valid = windows
                .Where(w => !double.IsNaN(w.Ratio) && !double.IsInfinity(w.Ratio))
                .ToList();

            var plt = new ScottPlot.Plot();

            var ratios = valid.Select(w => w.Ratio).ToArray();
            var plvs = valid.Select(w => w.Plv).ToArray();

            var sc = plt.Add.Scatter(ratios, plvs);
            sc.Color = ScottPlot.Color.FromHex("#1f77b4");
            sc.LineWidth = 0f;
            sc.MarkerSize = 3f;

            // Horizontal lines at PLV = 0.2, 0.3
            var h02 = plt.Add.HorizontalLine(0.2);
            h02.Color = ScottPlot.Color.FromHex("#888888");
            h02.LineWidth = 1f;
            h02.LinePattern = ScottPlot.LinePattern.Dotted;

            var h03 = plt.Add.HorizontalLine(0.3);
            h03.Color = ScottPlot.Color.FromHex("#888888");
            h03.LineWidth = 1f;
            h03.LinePattern = ScottPlot.LinePattern.Dotted;

            plt.Title("EEG Threshold Analysis: PLV vs K/σ");
            plt.XLabel("PLV / σ_ω  (K/σ proxy)");
            plt.YLabel("PLV");

            string pngPath = Path.Combine(PlotsDir, "threshold_curve.png");
            plt.SavePng(pngPath, 1200, 800);
            Console.WriteLine("  Threshold curve plot saved: " + pngPath);
            return pngPath;
        }

        // ── State Clustering ───────────────────────────────────────

        /// <summary>
        /// Threshold-based clustering into LOW / MID / HIGH sync states.
        /// Thresholds are derived from the data distribution (mean ± 1σ).
        /// </summary>
        public static void ClusterStates(List<SlidingWindowPoint> windows)
        {
            if (windows.Count == 0) return;

            double meanPlv = windows.Average(w => w.Plv);
            double stdPlv = StdDev(windows.Select(w => w.Plv));

            double lowHigh = meanPlv - 0.7 * stdPlv;   // ~0.12 for this data
            double highLow = meanPlv + 0.7 * stdPlv;   // ~0.16 for this data

            foreach (var w in windows)
            {
                if (w.Plv < lowHigh)
                    w.State = SyncState.Low;
                else if (w.Plv >= highLow)
                    w.State = SyncState.High;
                else
                    w.State = SyncState.Mid;
            }
        }

        /// <summary>
        /// Analyses state transitions: counts, dwell times, and
        /// dominant transition direction.
        /// </summary>
        public static (int lowCount, int midCount, int highCount,
            int transitions, List<string> sequence)
            AnalyzeStateTransitions(List<SlidingWindowPoint> windows)
        {
            int low = 0, mid = 0, high = 0, trans = 0;
            var seq = new List<string>();

            SyncState prev = windows.Count > 0 ? windows[0].State : SyncState.Low;
            foreach (var w in windows)
            {
                switch (w.State)
                {
                    case SyncState.Low: low++; break;
                    case SyncState.Mid: mid++; break;
                    case SyncState.High: high++; break;
                }

                if (w.State != prev)
                {
                    trans++;
                    seq.Add(string.Format(CultureInfo.InvariantCulture,
                        "{0} → {1}  @ t = {2:F1} s",
                        prev, w.State, w.TimeSec));
                    prev = w.State;
                }
            }

            return (low, mid, high, trans, seq);
        }

        /// <summary>
        /// Saves per-window state assignments as CSV.
        /// </summary>
        public static string SaveStateCsv(List<SlidingWindowPoint> windows)
        {
            string path = Path.Combine(OutputDir, "eeg_states.csv");
            var ci = CultureInfo.InvariantCulture;

            using var writer = new StreamWriter(path);
            writer.WriteLine("time_s,state,plv,omega_star_hz,omega_mode_hz,sigma_omega_hz,ratio");
            foreach (var w in windows)
                writer.WriteLine(string.Format(ci, "{0:F2},{1},{2:F4},{3:F2},{4:F2},{5:F3},{6:F4}",
                    w.TimeSec, w.State, w.Plv, w.OmegaStarHz, w.OmegaModeHz,
                    w.SigmaOmegaHz, w.Ratio));

            Console.WriteLine("  State CSV saved: " + path);
            return path;
        }

        /// <summary>
        /// Plots state timeline as colour-coded blocks over time.
        /// </summary>
        public static string PlotStateTimeline(List<SlidingWindowPoint> windows)
        {
            var plt = new ScottPlot.Plot();

            // Colour-code each state as a coloured band
            var times = windows.Select(w => w.TimeSec).ToArray();
            var stateVals = windows.Select(w =>
                w.State == SyncState.Low ? 0.0 :
                w.State == SyncState.Mid ? 1.0 : 2.0).ToArray();

            // Scatter with colour
            var lowXs = new List<double>();
            var lowYs = new List<double>();
            var midXs = new List<double>();
            var midYs = new List<double>();
            var highXs = new List<double>();
            var highYs = new List<double>();

            for (int i = 0; i < times.Length; i++)
            {
                switch (windows[i].State)
                {
                    case SyncState.Low:
                        lowXs.Add(times[i]); lowYs.Add(0.0); break;
                    case SyncState.Mid:
                        midXs.Add(times[i]); midYs.Add(1.0); break;
                    case SyncState.High:
                        highXs.Add(times[i]); highYs.Add(2.0); break;
                }
            }

            var scLow = plt.Add.Scatter(lowXs.ToArray(), lowYs.ToArray());
            scLow.Color = ScottPlot.Color.FromHex("#1f77b4");
            scLow.LegendText = "LOW";
            scLow.MarkerSize = 4f; scLow.LineWidth = 0f;

            var scMid = plt.Add.Scatter(midXs.ToArray(), midYs.ToArray());
            scMid.Color = ScottPlot.Color.FromHex("#ff7f0e");
            scMid.LegendText = "MID";
            scMid.MarkerSize = 4f; scMid.LineWidth = 0f;

            var scHigh = plt.Add.Scatter(highXs.ToArray(), highYs.ToArray());
            scHigh.Color = ScottPlot.Color.FromHex("#d62728");
            scHigh.LegendText = "HIGH";
            scHigh.MarkerSize = 4f; scHigh.LineWidth = 0f;

            // Custom tick labels for y-axis
            plt.Axes.SetLimits(
                (float)times.Min() - 1f, (float)times.Max() + 1f,
                -0.5f, 2.5f);

            plt.Title("EEG Synchronisation States");
            plt.XLabel("Time (s)");
            plt.YLabel("State");
            plt.ShowLegend();

            string pngPath = Path.Combine(PlotsDir, "state_timeline.png");
            plt.SavePng(pngPath, 1400, 500);
            Console.WriteLine("  State timeline plot saved: " + pngPath);
            return pngPath;
        }

        /// <summary>
        /// Groups windows by sync state and computes per-state Ω* statistics.
        /// </summary>
        public static void AnalyzeStateFrequencyRelation(
            List<SlidingWindowPoint> windows)
        {
            var groups = windows.GroupBy(w => w.State)
                .OrderBy(g => g.Key)
                .ToList();

            var ci = CultureInfo.InvariantCulture;

            Console.WriteLine(string.Format(ci,
                "  {0,-6} {1,5} {2,10} {3,10} {4,10}",
                "State", "Count", "mean Ω*", "std Ω*", "mean PLV"));
            Console.WriteLine("  " + new string('-', 46));

            foreach (var g in groups)
            {
                var list = g.ToList();
                double meanO = list.Average(w => w.OmegaStarHz);
                double stdO = StdDev(list.Select(w => w.OmegaStarHz));
                double meanP = list.Average(w => w.Plv);

                Console.WriteLine(string.Format(ci,
                    "  {0,-6} {1,5} {2,10:F2} {3,10:F2} {4,10:F4}",
                    g.Key.ToString(), list.Count, meanO, stdO, meanP));
            }
        }

        /// <summary>
        /// Scatter plot: Ω* vs PLV, coloured by sync state.
        /// </summary>
        public static string PlotStateFrequencyScatter(
            List<SlidingWindowPoint> windows)
        {
            var plt = new ScottPlot.Plot();

            var lowGroup = windows.Where(w => w.State == SyncState.Low).ToList();
            var midGroup = windows.Where(w => w.State == SyncState.Mid).ToList();
            var highGroup = windows.Where(w => w.State == SyncState.High).ToList();

            void AddGroup(List<SlidingWindowPoint> group, string hex, string label)
            {
                if (group.Count == 0) return;
                var sc = plt.Add.Scatter(
                    group.Select(w => w.Plv).ToArray(),
                    group.Select(w => w.OmegaStarHz).ToArray());
                sc.Color = ScottPlot.Color.FromHex(hex);
                sc.LegendText = string.Format(CultureInfo.InvariantCulture,
                    "{0} (n={1})", label, group.Count);
                sc.MarkerSize = 4f;
                sc.LineWidth = 0f;
            }

            AddGroup(lowGroup, "#1f77b4", "LOW");
            AddGroup(midGroup, "#ff7f0e", "MID");
            AddGroup(highGroup, "#d62728", "HIGH");

            plt.Title("Ω* vs PLV by Synchronisation State");
            plt.XLabel("PLV");
            plt.YLabel("Ω* (Hz)");
            plt.ShowLegend();

            string pngPath = Path.Combine(PlotsDir, "state_frequency_scatter.png");
            plt.SavePng(pngPath, 1200, 800);
            Console.WriteLine("  State–frequency scatter saved: " + pngPath);
            return pngPath;
        }

        // ── Frequency Band (Bridge-Band) Detection ─────────────────

        /// <summary>
        /// Builds a histogram of Ω* values from sliding windows,
        /// detects peaks, and reports candidate frequency bands.
        /// </summary>
        public static (int peakCount, List<double> peakFreqs, List<double> peakWidths)
            AnalyzeFrequencyBands(List<SlidingWindowPoint> windows,
            double binWidthHz = 0.1, double minHz = 0.5, double maxHz = 5.0)
        {
            var omegas = windows.Select(w => w.OmegaStarHz)
                .Where(o => o >= minHz && o <= maxHz)
                .ToList();

            if (omegas.Count < 10)
                return (0, new List<double>(), new List<double>());

            int binCount = (int)((maxHz - minHz) / binWidthHz) + 1;
            var bins = new int[binCount];
            foreach (double o in omegas)
            {
                int idx = (int)((o - minHz) / binWidthHz);
                if (idx >= 0 && idx < binCount)
                    bins[idx]++;
            }

            // Detect peaks: bin higher than both neighbours
            var peakFreqs = new List<double>();
            var peakWidths = new List<double>();
            int threshold = Math.Max(1, omegas.Count / 50);  // at least 2% of data

            for (int i = 1; i < binCount - 1; i++)
            {
                if (bins[i] >= threshold && bins[i] > bins[i - 1] && bins[i] > bins[i + 1])
                {
                    double center = minHz + (i + 0.5) * binWidthHz;

                    // Width at half-max
                    int halfMax = bins[i] / 2;
                    int left = i;
                    while (left > 0 && bins[left] > halfMax) left--;
                    int right = i;
                    while (right < binCount - 1 && bins[right] > halfMax) right++;
                    double width = (right - left) * binWidthHz;

                    peakFreqs.Add(center);
                    peakWidths.Add(width);
                }
            }

            var ci = CultureInfo.InvariantCulture;
            Console.WriteLine(string.Format(ci,
                "  Bin width: {0:F2} Hz   Range: {1:F1}–{2:F1} Hz   Samples: {3}",
                binWidthHz, minHz, maxHz, omegas.Count));
            Console.WriteLine(string.Format(ci,
                "  Peaks found: {0}", peakFreqs.Count));

            for (int i = 0; i < peakFreqs.Count; i++)
                Console.WriteLine(string.Format(ci,
                    "    #{0}:  {1:F2} Hz   (width ≈ {2:F2} Hz)",
                    i + 1, peakFreqs[i], peakWidths[i]));

            return (peakFreqs.Count, peakFreqs, peakWidths);
        }

        /// <summary>
        /// Plots histogram of Ω* with detected peak markers.
        /// </summary>
        public static string PlotOmegaHistogram(
            List<SlidingWindowPoint> windows,
            List<double> peakFreqs,
            double binWidthHz = 0.1, double minHz = 0.5, double maxHz = 5.0)
        {
            var omegas = windows.Select(w => w.OmegaStarHz)
                .Where(o => o >= minHz && o <= maxHz)
                .ToList();

            var plt = new ScottPlot.Plot();

            // Build histogram bars manually
            int binCount = (int)((maxHz - minHz) / binWidthHz) + 1;
            var barValues = new double[binCount];
            var barPositions = new double[binCount];
            foreach (double o in omegas)
            {
                int idx = (int)((o - minHz) / binWidthHz);
                if (idx >= 0 && idx < binCount)
                    barValues[idx]++;
            }
            for (int i = 0; i < binCount; i++)
                barPositions[i] = minHz + (i + 0.5) * binWidthHz;

            var bars = plt.Add.Bars(barPositions, barValues);
            bars.Color = ScottPlot.Color.FromHex("#1f77b4");

            // Mark peaks
            foreach (double pf in peakFreqs)
            {
                var vl = plt.Add.VerticalLine(pf);
                vl.Color = ScottPlot.Color.FromHex("#d62728");
                vl.LineWidth = 2f;
                vl.LinePattern = ScottPlot.LinePattern.Dashed;
            }

            plt.Title("Ω* Histogram — Collective Frequency Distribution");
            plt.XLabel("Ω* (Hz)");
            plt.YLabel("Count");

            string pngPath = Path.Combine(PlotsDir, "omega_histogram.png");
            plt.SavePng(pngPath, 1200, 600);
            Console.WriteLine("  Ω* histogram saved: " + pngPath);
            return pngPath;
        }

        // ── Dwell-Time Analysis ───────────────────────────────────

        /// <summary>
        /// Assigns each window to nearest detected Ω* peak, computes
        /// dwell times (attractor stability), and builds transition matrix.
        /// </summary>
        public static void AnalyzeDwellTime(
            List<SlidingWindowPoint> windows,
            List<double> peakFreqs,
            double stepSec = 1.0)
        {
            var ci = CultureInfo.InvariantCulture;

            if (peakFreqs.Count == 0)
            {
                Console.WriteLine("  No peaks to assign — skipping dwell-time analysis.");
                return;
            }

            // Step 1 — Assign each window to nearest peak
            for (int i = 0; i < windows.Count; i++)
            {
                double omega = windows[i].OmegaStarHz;
                int bestIdx = 0;
                double bestDist = double.MaxValue;
                for (int p = 0; p < peakFreqs.Count; p++)
                {
                    double d = Math.Abs(omega - peakFreqs[p]);
                    if (d < bestDist) { bestDist = d; bestIdx = p; }
                }
                windows[i].FreqClusterId = bestIdx;
            }

            // Step 2 — Dwell time per cluster
            var dwells = new List<double>[peakFreqs.Count];
            var visits = new int[peakFreqs.Count];
            for (int p = 0; p < peakFreqs.Count; p++)
                dwells[p] = new List<double>();

            int? currentCluster = null;
            int consecutive = 0;

            for (int i = 0; i < windows.Count; i++)
            {
                if (windows[i].FreqClusterId == currentCluster)
                {
                    consecutive++;
                }
                else
                {
                    if (currentCluster.HasValue && consecutive > 0)
                    {
                        dwells[currentCluster.Value].Add(consecutive * stepSec);
                        visits[currentCluster.Value]++;
                    }
                    currentCluster = windows[i].FreqClusterId;
                    consecutive = 1;
                }
            }
            // Flush last run
            if (currentCluster.HasValue && consecutive > 0)
            {
                dwells[currentCluster.Value].Add(consecutive * stepSec);
                visits[currentCluster.Value]++;
            }

            // Step 3 — Transition matrix
            var transitions = new Dictionary<(int from, int to), int>();
            for (int i = 1; i < windows.Count; i++)
            {
                int? from = windows[i - 1].FreqClusterId;
                int? to = windows[i].FreqClusterId;
                if (from.HasValue && to.HasValue && from != to)
                {
                    var key = (from.Value, to.Value);
                    transitions.TryGetValue(key, out int count);
                    transitions[key] = count + 1;
                }
            }

            // Step 4 — Output table
            Console.WriteLine("  Cluster  Center (Hz)   Visits   Mean Dwell (s)   Max Dwell (s)");
            Console.WriteLine("  -------  -----------   ------   --------------   --------------");
            for (int p = 0; p < peakFreqs.Count; p++)
            {
                double meanDwell = dwells[p].Count > 0 ? dwells[p].Average() : 0;
                double maxDwell = dwells[p].Count > 0 ? dwells[p].Max() : 0;
                Console.WriteLine(string.Format(ci,
                    "    #{0,-4}   {1,6:F2}       {2,4}        {3,5:F1}            {4,5:F1}",
                    p + 1, peakFreqs[p], visits[p], meanDwell, maxDwell));
            }

            Console.WriteLine();

            // Transition matrix (top 10)
            var topTrans = transitions.OrderByDescending(kv => kv.Value).Take(10).ToList();
            Console.WriteLine("  Top transitions:");
            foreach (var kv in topTrans)
                Console.WriteLine(string.Format(ci,
                    "    #{0} → #{1}  : {2}", kv.Key.from + 1, kv.Key.to + 1, kv.Value));
            Console.WriteLine();

            // Step 5 — Interpretation
            double maxMeanDwell = dwells.Where(d => d.Count > 0).Select(d => d.Average()).DefaultIfEmpty(0).Max();
            if (maxMeanDwell > 30)
                Console.WriteLine("  INTERPRETATION: Long dwell — stable Ω* attractors.");
            else if (maxMeanDwell > 10)
                Console.WriteLine("  INTERPRETATION: Moderate dwell — metastable frequency attractors.");
            else
                Console.WriteLine("  INTERPRETATION: Short dwell — transient frequency states.");
        }

        /// <summary>
        /// Plots cluster assignment vs time to visualise attractor stability.
        /// </summary>
        public static string PlotClusterTimeline(
            List<SlidingWindowPoint> windows,
            List<double> peakFreqs)
        {
            var ci = CultureInfo.InvariantCulture;
            var plt = new ScottPlot.Plot();

            double[] times = windows.Select(w => w.TimeSec).ToArray();
            double[] clusterIds = windows.Select(w => (double)(w.FreqClusterId ?? -1)).ToArray();

            var sp = plt.Add.Scatter(times, clusterIds);
            sp.MarkerSize = 3f;
            sp.LineWidth = 0.5f;
            sp.Color = ScottPlot.Color.FromHex("#1f77b4");

            plt.YLabel("Cluster ID (Ω* peak)");
            plt.XLabel("Time (s)");
            plt.Title("Ω* Cluster Timeline — Attractor Stability");

            // Set Y ticks to peak labels
            var axes = plt.Axes;
            var yTicks = new ScottPlot.TickGenerators.NumericManual();
            for (int p = 0; p < peakFreqs.Count; p++)
                yTicks.AddMajor(p, string.Format(ci, "P{0} ({1:F1} Hz)", p + 1, peakFreqs[p]));
            axes.Left.TickGenerator = yTicks;

            string pngPath = Path.Combine(PlotsDir, "cluster_timeline.png");
            plt.SavePng(pngPath, 1200, 600);
            Console.WriteLine("  Cluster timeline saved: " + pngPath);
            return pngPath;
        }

        /// <summary>
        /// Runs sliding-window analysis, prints summary, and generates outputs.
        /// </summary>
        public static void RunSlidingWindow(string? filePath = null,
            string? label = null)
        {
            filePath ??= ExpPath;
            label ??= Path.GetFileNameWithoutExtension(filePath);

            Console.WriteLine("  ── SLIDING-WINDOW: " + label + " ──");
            Console.WriteLine("  Window: 5 s   Step: 1 s");
            Console.WriteLine();

            var windows = SlidingWindowAnalyze(filePath);

            if (windows.Count == 0)
            {
                Console.WriteLine("  No windows computed.");
                return;
            }

            // Summary statistics
            double meanPlv = windows.Average(w => w.Plv);
            double stdPlv = NeuralEEGAnalyzer.StdDev(windows.Select(w => w.Plv));
            double meanOmega = windows.Average(w => w.OmegaStarHz);
            double stdOmega = NeuralEEGAnalyzer.StdDev(windows.Select(w => w.OmegaStarHz));

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "  Windows      : {0}", windows.Count));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "  Mean PLV     : {0:F4}  (σ = {1:F4})", meanPlv, stdPlv));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "  Mean Ω*      : {0:F2} Hz  (σ = {1:F2} Hz)", meanOmega, stdOmega));

            // Detect transitions
            var transitions = DetectTransitions(windows);
            if (transitions.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "  Transitions detected: {0}  (PLV rise > 0.05)", transitions.Count));
                foreach (var (_, t, delta) in transitions.Take(5))
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "    t = {0,7:F1} s   ΔPLV = +{1:F3}", t, delta));
                if (transitions.Count > 5)
                    Console.WriteLine("    ...");
                Console.WriteLine();
                Console.WriteLine("  INTERPRETATION: Dynamic synchronization transitions present.");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("  No transitions detected (PLV rise > 0.05).");
                Console.WriteLine("  INTERPRETATION: Stationary regime — PLV is stable across time.");
            }

            Console.WriteLine();

            // Save CSV
            SaveSlidingWindowCsv(windows);

            // Plot
            try
            {
                PlotSlidingWindow(windows, transitions);
            }
            catch (Exception ex)
            {
                Console.WriteLine("  Sliding-window plot warning: " + ex.Message);
            }

            // Threshold analysis
            Console.WriteLine("  ── EEG THRESHOLD ANALYSIS ──");
            var (thresholdRatio, corr) = AnalyzeThreshold(windows);

            if (!double.IsNaN(thresholdRatio))
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "  Threshold ratio    : {0:F3}  (PLV crosses 0.2)", thresholdRatio));
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "  Correlation (PLV vs ratio) : r = {0:F3}", corr));

                if (corr > 0.5 && thresholdRatio > 0.01)
                    Console.WriteLine("  INTERPRETATION: Clear Kuramoto-like threshold — PLV rises with K/σ.");
                else if (corr > 0.25)
                    Console.WriteLine("  INTERPRETATION: Soft transition / partially noise-dominated.");
                else
                    Console.WriteLine("  INTERPRETATION: Weak or absent threshold — noise-dominated regime.");
            }
            else
            {
                Console.WriteLine("  Insufficient data for threshold analysis.");
            }

            try
            {
                PlotThresholdCurve(windows);
            }
            catch (Exception ex)
            {
                Console.WriteLine("  Threshold plot warning: " + ex.Message);
            }

            // State clustering
            Console.WriteLine("  ── STATE CLUSTERING ──");
            ClusterStates(windows);

            var (low, mid, high, trans, seq) = AnalyzeStateTransitions(windows);

            int total = low + mid + high;
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "  LOW  : {0,4}  ({1:F1}%)", low, 100.0 * low / total));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "  MID  : {0,4}  ({1:F1}%)", mid, 100.0 * mid / total));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "  HIGH : {0,4}  ({1:F1}%)", high, 100.0 * high / total));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "  State transitions : {0}", trans));

            if (trans > 0)
            {
                Console.WriteLine("  Recent transitions:");
                foreach (var s in seq.TakeLast(5))
                    Console.WriteLine("    " + s);
            }

            if (trans > total * 0.02)
                Console.WriteLine("  INTERPRETATION: Dynamic switching between TRM attractors.");
            else if (high > total * 0.3 || low > total * 0.3)
                Console.WriteLine("  INTERPRETATION: EEG operates in metastable synchronisation states.");
            else
                Console.WriteLine("  INTERPRETATION: Broad distribution — no single dominant state.");

            SaveStateCsv(windows);

            try
            {
                PlotStateTimeline(windows);
            }
            catch (Exception ex)
            {
                Console.WriteLine("  State timeline plot warning: " + ex.Message);
            }

            // State–frequency relation
            Console.WriteLine("  ── STATE × FREQUENCY ──");
            AnalyzeStateFrequencyRelation(windows);

            // Check whether Ω* shifts with state
            var lowOmegas = windows.Where(w => w.State == SyncState.Low)
                .Select(w => w.OmegaStarHz).ToList();
            var highOmegas = windows.Where(w => w.State == SyncState.High)
                .Select(w => w.OmegaStarHz).ToList();
            if (lowOmegas.Count > 0 && highOmegas.Count > 0)
            {
                double delta = Math.Abs(lowOmegas.Average() - highOmegas.Average());
                if (delta > 0.3)
                    Console.WriteLine("  INTERPRETATION: Ω* shifts with sync state — different attractors, different frequencies.");
                else
                    Console.WriteLine("  INTERPRETATION: Ω* is stable across sync states — states differ in synchrony, not frequency.");
            }

            Console.WriteLine();

            try
            {
                PlotStateFrequencyScatter(windows);
            }
            catch (Exception ex)
            {
                Console.WriteLine("  State–frequency scatter warning: " + ex.Message);
            }

            // Frequency band (bridge-band) detection
            Console.WriteLine("  ── FREQUENCY BANDS (bridge-band candidates) ──");
            var (peakCount, peakFreqs, peakWidths) = AnalyzeFrequencyBands(windows);

            if (peakCount == 1)
                Console.WriteLine("  INTERPRETATION: Single frequency attractor — Ω* has one preferred band.");
            else if (peakCount > 1)
                Console.WriteLine("  INTERPRETATION: Multiple frequency attractors — neural 'bridge bands' detected.");
            else
                Console.WriteLine("  INTERPRETATION: No clear preferred frequency — flat or noisy distribution.");

            Console.WriteLine();

            try
            {
                PlotOmegaHistogram(windows, peakFreqs);
            }
            catch (Exception ex)
            {
                Console.WriteLine("  Ω* histogram warning: " + ex.Message);
            }

            // Dwell-time / attractor stability analysis
            Console.WriteLine("  ── DWELL-TIME (attractor stability) ──");
            AnalyzeDwellTime(windows, peakFreqs);

            try
            {
                PlotClusterTimeline(windows, peakFreqs);
            }
            catch (Exception ex)
            {
                Console.WriteLine("  Cluster timeline warning: " + ex.Message);
            }

            Console.WriteLine();
        }

        // ── Convenience Runner ─────────────────────────────────────

        /// <summary>
        /// Runs the full pipeline (analysis, CSV, plots) and prints
        /// everything to the console. Call from Program.cs menu.
        /// </summary>
        public static void RunAndPrintAll()
        {
            Console.WriteLine("  EEG data dir: " + EegDir);
            Console.WriteLine();

            if (!File.Exists(ExpPath))
            {
                Console.WriteLine("  ERROR: Experimental EEG file not found.");
                Console.WriteLine("    " + ExpPath);
                return;
            }

            double bandLow = 1.0;
            double bandHigh = 80.0;

            // Analyse experimental recording
            Console.WriteLine("  Analysing: " + Path.GetFileName(ExpPath));
            Console.WriteLine();

            var result = Analyze(ExpPath, bandLow, bandHigh);
            PrintSummary(result);

            // Save CSV
            Console.WriteLine();
            SaveSummaryCsv(result);

            // Plots
            Console.WriteLine();
            Console.WriteLine("  Generating plots...");
            try
            {
                PlotSpectra(result, ExpPath, result.SamplingRateHz, bandLow, bandHigh);
                PlotPhaseDiff(result, "AF7", "AF8");
                PlotPlvMatrix(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("  Plot warning: " + ex.Message);
            }

            // Sliding-window analysis
            Console.WriteLine();
            try
            {
                RunSlidingWindow(ExpPath, "EXPERIMENTAL");
            }
            catch (Exception ex)
            {
                Console.WriteLine("  Sliding-window warning: " + ex.Message);
            }

            // Also analyse control if present
            if (File.Exists(ControlPath))
            {
                Console.WriteLine();
                Console.WriteLine("  ── CONTROL RECORDING ──");
                Console.WriteLine("  " + Path.GetFileName(ControlPath));
                Console.WriteLine();

                var ctrlResult = Analyze(ControlPath, bandLow, bandHigh);
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "  Ω* = {0:F2} Hz   PLV = {1:F4}   mean ω_i = {2:F2} Hz",
                    ctrlResult.OmegaStarHz, ctrlResult.GlobalPlv, ctrlResult.MeanOmegaHz));
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "  Centroid error = {0:F2} Hz   Synchronised = {1}",
                    ctrlResult.CentroidError, ctrlResult.IsSynchronized));
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "  Mode: Ω* = {0:F2} Hz   Ω_mode = {1:F2} Hz   cluster σ = {2:F3} Hz   err = {3:F3} Hz",
                    ctrlResult.OmegaStarHz,
                    ctrlResult.OmegaModeHz,
                    ctrlResult.ClusterConsistencyHz,
                    ctrlResult.CentroidErrorCorrected));

                // Sliding-window for control
                try
                {
                    RunSlidingWindow(ControlPath, "CONTROL");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  Sliding-window warning: " + ex.Message);
                }
            }
        }
    }
}
