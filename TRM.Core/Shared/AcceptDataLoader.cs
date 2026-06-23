
using System.Globalization;

namespace TRM.Core.Shared
{
    public class AcceptDataLoader
    {
        public List<AcceptClusterProfile> Load(string filePath)
        {
            var data = new List<AcceptClusterProfile>();

            foreach(var line in File.ReadLines(filePath))
            {
                var trimmed = line.Trim();

                if(string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                var parts = trimmed.Split(
                    new[] { ' ', '\t' },
                    StringSplitOptions.RemoveEmptyEntries);

                if(parts.Length < 15)
                    continue;

                try
                {
                    string name = parts[0];

                    double rin = double.Parse(parts[1], CultureInfo.InvariantCulture);
                    double rout = double.Parse(parts[2], CultureInfo.InvariantCulture);

                    double radius = 0.5 * (rin + rout);

                    double ne = double.Parse(parts[3], CultureInfo.InvariantCulture);
                    double temp = double.Parse(parts[12], CultureInfo.InvariantCulture);

                    double entropy = double.Parse(parts[5], CultureInfo.InvariantCulture);
                    double pressure = double.Parse(parts[8], CultureInfo.InvariantCulture);

                    double mass = double.Parse(parts[11], CultureInfo.InvariantCulture);

                    data.Add(new AcceptClusterProfile(
                        name,
                        radius,
                        ne,
                        temp,
                        pressure,
                        entropy,
                        mass
                    ));
                }
                catch
                {
                    continue;
                }
            }

            return data;
        }
    }
}
