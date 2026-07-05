using System;
using System.IO;

namespace TRM.CMD
{
    /// <summary>
    /// Centralised path resolution for Laser-array analysis modules.
    /// Input data lives in Data\Laser\.
    /// All generated output goes to Data\Laser\Results\{timestamp}\
    /// so nothing is ever overwritten.
    /// </summary>
    public static class LaserDataPath
    {
        private static string? _inputDir;
        private static string? _resultsDir;

        /// <summary>Data\Laser — where input CSVs, PNGs, and the PDF live.</summary>
        public static string InputDir
        {
            get
            {
                if (_inputDir == null)
                    _inputDir = ResolveLaserDir();
                return _inputDir;
            }
        }

        /// <summary>Data\Laser\Results\2026-07-04_204900\ — timestamped, created on first access.</summary>
        public static string ResultsDir
        {
            get
            {
                if (_resultsDir == null)
                {
                    string ts = DateTime.Now.ToString("yyyy-MM-dd_HHmmss",
                        System.Globalization.CultureInfo.InvariantCulture);
                    _resultsDir = Path.Combine(InputDir, "Results", ts);
                    Directory.CreateDirectory(_resultsDir);
                }
                return _resultsDir;
            }
        }

        /// <summary>Timestamped plots subdirectory.</summary>
        public static string PlotsDir
        {
            get
            {
                string dir = Path.Combine(ResultsDir, "Plots");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        /// <summary>Generate a unique file path in ResultsDir with the given name.
        /// If a file already exists, appends (1), (2), etc.</summary>
        public static string UniqueResultPath(string fileName)
        {
            string basePath = Path.Combine(ResultsDir, fileName);
            if (!File.Exists(basePath))
                return basePath;

            string name = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            int counter = 1;
            string candidate;
            do
            {
                candidate = Path.Combine(ResultsDir,
                    string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{0} ({1}){2}", name, counter, ext));
                counter++;
            } while (File.Exists(candidate));

            return candidate;
        }

        /// <summary>Generate a unique file path in PlotsDir.</summary>
        public static string UniquePlotPath(string fileName)
        {
            string basePath = Path.Combine(PlotsDir, fileName);
            if (!File.Exists(basePath))
                return basePath;

            string name = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            int counter = 1;
            string candidate;
            do
            {
                candidate = Path.Combine(PlotsDir,
                    string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{0} ({1}){2}", name, counter, ext));
                counter++;
            } while (File.Exists(candidate));

            return candidate;
        }

        // ── Internal resolution ────────────────────────────────────

        private static string ResolveLaserDir()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "TRM_Cosmology.slnx")))
                dir = dir.Parent;
            return dir != null
                ? Path.Combine(dir.FullName, "Data", "Laser")
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Laser");
        }
    }
}
