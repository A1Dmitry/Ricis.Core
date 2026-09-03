using System.Numerics;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using Ricis.Kinematics;
using Ricis.Kinematics.Domain;

namespace Ricis.Robotics3D.App;

/// <summary>
/// Builds a visual snapshot from the same DH model used by the kinematics solver.
/// </summary>
public sealed class RobotSceneBuilder
{
    private readonly Material _baseMaterial = MaterialHelper.CreateMaterial(Colors.DarkSlateGray);
    private readonly Material _jointMaterial = MaterialHelper.CreateMaterial(Colors.Silver);
    private readonly Material _linkMaterial = MaterialHelper.CreateMaterial(Colors.DarkOrange);
    private readonly Material _wristMaterial = MaterialHelper.CreateMaterial(Colors.DarkBlue);
    private readonly Material _gripperMaterial = MaterialHelper.CreateMaterial(Colors.Gold);
    private readonly Material _boxMaterial = MaterialHelper.CreateMaterial(Colors.SaddleBrown);

    public Model3DGroup Build(ManipulatorArm arm, JointAngles joints, IReadOnlyList<Workpiece> workpieces)
    {
        var group = new Model3DGroup();
        AddBox(group, new Vector3(0.4f, 0.3f, 0.05f), 0.3f, 0.3f, 0.15f, _boxMaterial);
        AddBox(group, new Vector3(0.4f, -0.3f, 0.05f), 0.3f, 0.3f, 0.15f, _boxMaterial);
        foreach (var piece in workpieces) AddWorkpiece(group, piece);

        var origins = ForwardKinematics.ComputeJointOrigins(arm.Links, joints.ToRadiansArray());
        AddCylinder(group, ToVector(origins[0]), ToVector(origins[1]), 0.14f, _baseMaterial);
        for (var i = 1; i < origins.Count; i++)
        {
            var from = ToVector(origins[i - 1]);
            var to = ToVector(origins[i]);
            AddCylinder(group, from, to, i < 4 ? 0.055f : 0.035f, i < 4 ? _linkMaterial : _wristMaterial);
            AddSphere(group, to, i < 4 ? 0.07f : 0.045f, _jointMaterial);
        }

        var tool = ToVector(origins[^1]) + new Vector3(0, 0, 0.05f);
        AddBox(group, tool, 0.06f, 0.06f, 0.04f, _gripperMaterial);
        AddBox(group, tool + new Vector3(0, 0.03f, 0), 0.04f, 0.015f, 0.06f, _gripperMaterial);
        AddBox(group, tool - new Vector3(0, 0.03f, 0), 0.04f, 0.015f, 0.06f, _gripperMaterial);
        return group;
    }

    private static void AddWorkpiece(Model3DGroup group, Workpiece piece)
    {
        var center = new Vector3((float)piece.Position.X, (float)piece.Position.Y, (float)piece.Position.Z);
        var material = MaterialHelper.CreateMaterial(piece.Shape switch
        {
            WorkpieceShape.Cube => Colors.Crimson,
            WorkpieceShape.Sphere => Colors.RoyalBlue,
            WorkpieceShape.Pyramid => Colors.Gold,
            _ => Colors.Gray
        });
        var mesh = new MeshBuilder(false, false);
        switch (piece.Shape)
        {
            case WorkpieceShape.Cube: mesh.AddBox(center, 0.04f, 0.04f, 0.04f); break;
            case WorkpieceShape.Sphere: mesh.AddSphere(center, 0.025f); break;
            case WorkpieceShape.Pyramid: mesh.AddCone(center, center + new Vector3(0, 0, 0.05f), 0.03f, true, 4); break;
        }
        group.Children.Add(new GeometryModel3D(ToWpfMesh(mesh.ToMesh()), material));
    }

    private static Vector3 ToVector((double X, double Y, double Z) point) => new((float)point.X, (float)point.Y, (float)point.Z);

    private static void AddBox(Model3DGroup group, Vector3 center, float x, float y, float z, Material material)
    {
        var mesh = new MeshBuilder(false, false);
        mesh.AddBox(center, x, y, z);
        group.Children.Add(new GeometryModel3D(ToWpfMesh(mesh.ToMesh()), material));
    }

    private static void AddSphere(Model3DGroup group, Vector3 center, float radius, Material material)
    {
        var mesh = new MeshBuilder(false, false);
        mesh.AddSphere(center, radius);
        group.Children.Add(new GeometryModel3D(ToWpfMesh(mesh.ToMesh()), material));
    }

    private static void AddCylinder(Model3DGroup group, Vector3 from, Vector3 to, float radius, Material material)
    {
        if (Vector3.DistanceSquared(from, to) < 1e-8f) return;
        var mesh = new MeshBuilder(false, false);
        mesh.AddCylinder(from, to, radius, 24);
        group.Children.Add(new GeometryModel3D(ToWpfMesh(mesh.ToMesh()), material));
    }

    private static System.Windows.Media.Media3D.MeshGeometry3D ToWpfMesh(HelixToolkit.Geometry.MeshGeometry3D mesh)
    {
        var result = new System.Windows.Media.Media3D.MeshGeometry3D();
        foreach (var position in mesh.Positions) result.Positions.Add(new Point3D(position.X, position.Y, position.Z));
        foreach (var index in mesh.TriangleIndices) result.TriangleIndices.Add(index);
        if (mesh.Normals != null) foreach (var normal in mesh.Normals) result.Normals.Add(new Vector3D(normal.X, normal.Y, normal.Z));
        return result;
    }
}
