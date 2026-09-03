using System.Windows;
using System.Windows.Media.Media3D;
using Ricis.Kinematics.Domain;
using Ricis.Robotics3D.App.ViewModels;

namespace Ricis.Robotics3D.App;

/// <summary>
/// Presentation shell only. State and scene construction are owned by the ViewModel/application layer.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();

        if (DataContext is MainViewModel vm)
        {
            _vm = vm;
        }
        else
        {
            _vm = new MainViewModel();
            DataContext = _vm;
        }

        _vm.KinematicsUpdated += Build3DRobotArmGeometry;

        Build3DRobotArmGeometry();
    }

    /// <summary>
    /// Converts HelixToolkit 3.x Geometry.MeshGeometry3D to System.Windows.Media.Media3D.MeshGeometry3D.
    /// </summary>
    public static System.Windows.Media.Media3D.MeshGeometry3D ConvertToWpfMesh(HelixToolkit.Geometry.MeshGeometry3D mesh)
    {
        var wpfMesh = new System.Windows.Media.Media3D.MeshGeometry3D();
        if (mesh.Positions != null)
        {
            foreach (var p in mesh.Positions)
            {
                wpfMesh.Positions.Add(new Point3D(p.X, p.Y, p.Z));
            }
        }
        if (mesh.TriangleIndices != null)
        {
            foreach (var idx in mesh.TriangleIndices)
            {
                wpfMesh.TriangleIndices.Add(idx);
            }
        }
        if (mesh.Normals != null)
        {
            foreach (var n in mesh.Normals)
            {
                wpfMesh.Normals.Add(new Vector3D(n.X, n.Y, n.Z));
            }
        }
        return wpfMesh;
    }

    /// <summary>
    /// Renders 3D industrial arm links, boxes, and workpieces via UrdfRobotVisual3D.
    /// </summary>
    private void Build3DRobotArmGeometry()
    {
        //TODO Build3DRobotArmGeometry example at https://ricis-3-expansion.ai.studio/?node=6235182e70cd5dae93cdda3d4e9be016&lang=en&mode=verify&view=kinematic
        throw new NotImplementedException();
    }
}
