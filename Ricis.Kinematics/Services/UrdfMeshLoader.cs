namespace Ricis.Kinematics.Services;

/// <summary>
/// Domain service for loading 3D Wavefront .OBJ mesh geometry definitions for manipulator link CAD visuals.
/// </summary>
public sealed class UrdfMeshLoader
{
    public sealed record ObjMeshData(List<float[]> Vertices, List<int[]> Faces);

    public ObjMeshData LoadObjContent(string objFileContent)
    {
        var vertices = new List<float[]>();
        var faces = new List<int[]>();

        using var reader = new StringReader(objFileContent);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.StartsWith("v "))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4 &&
                    float.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(parts[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float y) &&
                    float.TryParse(parts[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float z))
                {
                    vertices.Add([x, y, z]);
                }
            }
            else if (line.StartsWith("f "))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var faceIndices = new List<int>();
                for (int i = 1; i < parts.Length; i++)
                {
                    var vertIdxStr = parts[i].Split('/')[0];
                    if (int.TryParse(vertIdxStr, out int idx))
                    {
                        faceIndices.Add(idx > 0 ? idx - 1 : 0);
                    }
                }
                if (faceIndices.Count >= 3)
                {
                    faces.Add(faceIndices.ToArray());
                }
            }
        }

        return new ObjMeshData(vertices, faces);
    }
}
