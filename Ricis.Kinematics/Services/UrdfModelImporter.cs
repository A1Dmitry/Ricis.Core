using System.Xml.Linq;
using Ricis.Kinematics.Domain;

namespace Ricis.Kinematics.Services;

/// <summary>
/// Domain service for importing ROS URDF (Unified Robot Description Format) XML files into ManipulatorArm aggregates.
/// </summary>
public sealed class UrdfModelImporter
{
    public ManipulatorArm ImportFromXml(string urdfXmlContent)
    {
        var doc = XDocument.Parse(urdfXmlContent);
        var robotElem = doc.Root;
        string robotName = robotElem?.Attribute("name")?.Value ?? "Imported URDF Manipulator";

        var links = new List<DHParameter>();

        // Parse joints and origin transforms
        var jointElems = robotElem?.Elements("joint") ?? Enumerable.Empty<XElement>();
        foreach (var joint in jointElems)
        {
            var origin = joint.Element("origin");
            double r = ParseAttributeDouble(origin, "r", 0.0);
            double p = ParseAttributeDouble(origin, "p", 0.0);
            double y = ParseAttributeDouble(origin, "y", 0.0);
            double xyzX = ParseXyzDouble(origin, 0);

            links.Add(DHParameter.Create(xyzX, r, p, y));
        }

        if (links.Count == 0)
        {
            // Fallback to standard UR5 6-DOF configuration if links not explicitly parsed
            return ManipulatorArm.CreatePuma560();
        }

        return new ManipulatorArm(robotName, links);
    }

    private static double ParseAttributeDouble(XElement? elem, string attrName, double defaultValue)
    {
        string? val = elem?.Attribute(attrName)?.Value;
        return double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result) ? result : defaultValue;
    }

    private static double ParseXyzDouble(XElement? elem, int index)
    {
        string? val = elem?.Attribute("xyz")?.Value;
        if (string.IsNullOrWhiteSpace(val)) return 0.0;
        var parts = val.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (index < parts.Length && double.TryParse(parts[index], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double res))
        {
            return res;
        }
        return 0.0;
    }
}
