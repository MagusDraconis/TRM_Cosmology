using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TRM.CMD
{
    // ── Data Structures ──────────────────────────────────────────

    /// <summary>
    /// A single digitised point from a paper curve,
    /// storing both pixel coordinates and calibrated data coordinates.
    /// </summary>
    public class DigitizedPoint
    {
        public double PixelX { get; set; }
        public double PixelY { get; set; }
        public double DataX { get; set; }
        public double DataY { get; set; }
    }

    // ── Digitizer Form ───────────────────────────────────────────

    /// <summary>
    /// WinForms window that displays a paper figure and lets the user
    /// click along a curve to digitise it.
    /// </summary>
    public class DigitizerForm : Form
    {
        public List<DigitizedPoint> Points = new();

        private readonly PictureBox _pictureBox;
        private readonly Bitmap _image;
        private readonly Label _statusLabel;

        public DigitizerForm(string imagePath)
        {
            _image = new Bitmap(imagePath);

            Text = "Paper Curve Digitiser — Click along curve, close window when done";
            Width = Math.Min(_image.Width + 40, 1400);
            Height = Math.Min(_image.Height + 80, 900);
            StartPosition = FormStartPosition.CenterScreen;

            _pictureBox = new PictureBox
            {
                Image = _image,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            _pictureBox.MouseClick += OnMouseClick;

            _statusLabel = new Label
            {
                Text = "Points: 0 | Click along the curve. Close window when finished.",
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0)
            };

            Controls.Add(_pictureBox);
            Controls.Add(_statusLabel);
        }

        private void OnMouseClick(object? sender, MouseEventArgs e)
        {
            // Convert screen coordinates to picture-box-relative coordinates
            // accounting for the zoom / letterboxing of SizeMode.Zoom
            float imageAspect = (float)_image.Width / _image.Height;
            float controlAspect = (float)_pictureBox.Width / _pictureBox.Height;

            int imgDisplayWidth, imgDisplayHeight;
            int offsetX = 0, offsetY = 0;

            if (controlAspect > imageAspect)
            {
                // Control is wider — image constrained by height
                imgDisplayHeight = _pictureBox.Height;
                imgDisplayWidth = (int)(imgDisplayHeight * imageAspect);
                offsetX = (_pictureBox.Width - imgDisplayWidth) / 2;
            }
            else
            {
                // Control is taller — image constrained by width
                imgDisplayWidth = _pictureBox.Width;
                imgDisplayHeight = (int)(imgDisplayWidth / imageAspect);
                offsetY = (_pictureBox.Height - imgDisplayHeight) / 2;
            }

            double px = (e.X - offsetX) / (double)imgDisplayWidth * _image.Width;
            double py = (e.Y - offsetY) / (double)imgDisplayHeight * _image.Height;

            // Clamp to image bounds
            px = Math.Clamp(px, 0, _image.Width - 1);
            py = Math.Clamp(py, 0, _image.Height - 1);

            Points.Add(new DigitizedPoint
            {
                PixelX = px,
                PixelY = py
            });

            _statusLabel.Text = string.Format(CultureInfo.InvariantCulture,
                "Points: {0} | Last click: ({1:F0}, {2:F0}) | Close window when finished.",
                Points.Count, px, py);

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "  Click #{0}: pixel ({1:F0}, {2:F0})", Points.Count, px, py));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // ── Digitiser Engine ─────────────────────────────────────────

    /// <summary>
    /// Digitises curves from scientific paper images by letting the user
    /// click along the curve, then calibrating pixel coordinates to data
    /// coordinates using axis bounds.
    /// </summary>
    public static class PaperCurveDigitizer
    {
        /// <summary>
        /// Converts pixel coordinates to data coordinates using linear
        /// mapping from the image bounds to the specified axis range.
        /// PixelY = 0 is the TOP of the image (data Y max).
        /// </summary>
        public static void Calibrate(
            List<DigitizedPoint> points,
            double xMin, double xMax,
            double yMin, double yMax,
            int imageWidth, int imageHeight)
        {
            double xRange = xMax - xMin;
            double yRange = yMax - yMin;

            foreach (var p in points)
            {
                double xNorm = p.PixelX / imageWidth;
                double yNorm = 1.0 - (p.PixelY / (double)imageHeight); // flip Y

                p.DataX = xMin + xNorm * xRange;
                p.DataY = yMin + yNorm * yRange;
            }
        }

        /// <summary>
        /// Saves digitised points as CSV with header "x,y".
        /// </summary>
        public static void SaveCsv(List<DigitizedPoint> points, string path)
        {
            var inv = CultureInfo.InvariantCulture;

            using var writer = new StreamWriter(path);

            writer.WriteLine("x,y");

            foreach (var p in points)
                writer.WriteLine(string.Format(inv, "{0},{1}", p.DataX, p.DataY));

            Console.WriteLine(string.Format(inv,
                "  Saved {0} points to {1}", points.Count, path));
        }

        /// <summary>
        /// Plots digitised points and saves as PNG.
        /// </summary>
        public static void Plot(List<DigitizedPoint> points, string title = "Digitised Curve")
        {
            var plt = new ScottPlot.Plot();
            var xs = points.Select(p => p.DataX).ToArray();
            var ys = points.Select(p => p.DataY).ToArray();

            var scatter = plt.Add.Scatter(xs, ys);
            scatter.MarkerSize = 6;
            scatter.LineWidth = 1;

            plt.Title(title);
            plt.XLabel("Omega_RMS / K");
            plt.YLabel("SyncMetric (IPR)");

            string pngPath = LaserDataPath.UniquePlotPath(
                title.Replace(' ', '_') + ".png");
            plt.SavePng(pngPath, 800, 600);

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "  Plot saved: {0}", pngPath));

            // Try to open the image
            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(pngPath) { UseShellExecute = true });
            }
            catch
            {
                // Opening the image is best-effort
            }
        }

        // ── Interactive Pipeline ──────────────────────────────────

        /// <summary>
        /// Runs the full interactive digitisation pipeline.
        ///   1. User picks an image file
        ///   2. Clicks along the curve
        ///   3. Closes the window
        ///   4. Points are calibrated and saved as CSV
        ///   5. Plot is shown
        /// </summary>
        public static List<DigitizedPoint> RunInteractive(
            string imagePath,
            double xMin, double xMax,
            double yMin, double yMax,
            string? outputCsvPath = null)
        {
            if (!File.Exists(imagePath))
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "  ERROR: image not found: {0}", imagePath));
                return new List<DigitizedPoint>();
            }

            Console.WriteLine("  Opening image for digitisation...");
            Console.WriteLine("  Click along the curve. Close the window when done.");
            Console.WriteLine();

            // Run the WinForms message loop
            var form = new DigitizerForm(imagePath);
            Application.Run(form);

            var points = form.Points;

            Console.WriteLine();
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "  Collected {0} points.", points.Count));

            if (points.Count == 0)
            {
                Console.WriteLine("  No points collected. Aborting.");
                return points;
            }

            // Get image dimensions for calibration
            using var bmp = new Bitmap(imagePath);
            int w = bmp.Width;
            int h = bmp.Height;

            Calibrate(points, xMin, xMax, yMin, yMax, w, h);

            // Save CSV
            string csvPath = outputCsvPath ?? Path.ChangeExtension(imagePath, ".csv");
            SaveCsv(points, csvPath);

            // Plot
            Plot(points, Path.GetFileNameWithoutExtension(imagePath));

            return points;
        }

        /// <summary>
        /// Non-interactive batch mode: loads pre-clicked pixel points
        /// from a text file, calibrates, and saves CSV.
        /// </summary>
        public static List<DigitizedPoint> RunBatch(
            string pixelFile,
            double xMin, double xMax,
            double yMin, double yMax,
            int imageWidth, int imageHeight,
            string outputCsvPath)
        {
            var inv = CultureInfo.InvariantCulture;

            var points = new List<DigitizedPoint>();
            foreach (var line in File.ReadLines(pixelFile))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                    continue;

                var parts = trimmed.Split(',', ' ', '\t');
                if (parts.Length >= 2 &&
                    double.TryParse(parts[0], NumberStyles.Float, inv, out double px) &&
                    double.TryParse(parts[1], NumberStyles.Float, inv, out double py))
                {
                    points.Add(new DigitizedPoint { PixelX = px, PixelY = py });
                }
            }

            Console.WriteLine(string.Format(inv,
                "  Loaded {0} pixel points from {1}", points.Count, pixelFile));

            if (points.Count == 0) return points;

            Calibrate(points, xMin, xMax, yMin, yMax, imageWidth, imageHeight);
            SaveCsv(points, outputCsvPath);

            return points;
        }
    }
}
