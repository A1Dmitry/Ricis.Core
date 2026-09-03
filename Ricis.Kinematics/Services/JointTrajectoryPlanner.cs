using Ricis.Kinematics.Domain;

namespace Ricis.Kinematics.Services;

/// <summary>
/// Time-scaled quintic joint-space planner. Each segment starts and ends with zero velocity and acceleration.
/// </summary>
public sealed class JointTrajectoryPlanner
{
    private readonly double _maxVelocityDegreesPerSecond;
    private readonly double _maxAccelerationDegreesPerSecondSquared;

    public JointTrajectoryPlanner(double maxVelocityDegreesPerSecond = 60, double maxAccelerationDegreesPerSecondSquared = 120)
    {
        if (maxVelocityDegreesPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(maxVelocityDegreesPerSecond));
        if (maxAccelerationDegreesPerSecondSquared <= 0) throw new ArgumentOutOfRangeException(nameof(maxAccelerationDegreesPerSecondSquared));
        _maxVelocityDegreesPerSecond = maxVelocityDegreesPerSecond;
        _maxAccelerationDegreesPerSecondSquared = maxAccelerationDegreesPerSecondSquared;
    }

    public JointAngles Interpolate(IReadOnlyList<JointAngles> waypoints, double progressPercentage)
    {
        if (waypoints is null || waypoints.Count < 2) throw new ArgumentException("At least two waypoints are required.", nameof(waypoints));
        var durations = new double[waypoints.Count - 1];
        var totalDuration = 0.0;
        for (var i = 0; i < durations.Length; i++)
        {
            durations[i] = SegmentDuration(waypoints[i], waypoints[i + 1]);
            totalDuration += durations[i];
        }

        var time = Math.Clamp(progressPercentage, 0, 100) / 100.0 * totalDuration;
        var segment = 0;
        while (segment < durations.Length - 1 && time > durations[segment])
        {
            time -= durations[segment++];
        }
        var local = durations[segment] <= double.Epsilon ? 1 : time / durations[segment];
        var blend = QuinticBlend(local);
        var from = waypoints[segment].ToDegreesArray();
        var to = waypoints[segment + 1].ToDegreesArray();
        var values = new double[from.Length];
        for (var i = 0; i < values.Length; i++) values[i] = from[i] + (to[i] - from[i]) * blend;
        return new JointAngles(values[0], values[1], values[2], values[3], values[4], values[5]);
    }

    public double SegmentDuration(JointAngles from, JointAngles to)
    {
        var maxDelta = from.ToDegreesArray().Zip(to.ToDegreesArray(), (a, b) => Math.Abs(b - a)).Max();
        var velocityBound = 1.875 * maxDelta / _maxVelocityDegreesPerSecond;
        var accelerationBound = Math.Sqrt(5.7735 * maxDelta / _maxAccelerationDegreesPerSecondSquared);
        return Math.Max(0.2, Math.Max(velocityBound, accelerationBound));
    }

    private static double QuinticBlend(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * t * (10 + t * (-15 + 6 * t));
    }
}
