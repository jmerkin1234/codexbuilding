using System.Security.Cryptography;
using System.Text;

namespace CodexBuilding.Billiards.Core.Simulation;

public static class SimulationFingerprintBuilder
{
    public static string Build(SimulationReplayTrace trace)
    {
        var builder = new StringBuilder(4096);

        AppendInt(builder, trace.Completed ? 1 : 0);
        builder.Append('|');
        AppendInt(builder, trace.MaxSteps);
        builder.Append('|');
        AppendResolvedCueStrike(builder, trace.ResolvedCueStrike);

        foreach (var frame in trace.Frames)
        {
            builder.Append("|F:");
            AppendInt(builder, frame.StepIndex);
            builder.Append(':');
            AppendInt(builder, (int)frame.Phase);
            builder.Append(':');
            AppendFloat(builder, frame.SimulationTimeSeconds);

            foreach (var ball in frame.Balls.OrderBy(ball => ball.BallNumber))
            {
                builder.Append("|B:");
                AppendInt(builder, ball.BallNumber);
                builder.Append(':');
                AppendInt(builder, (int)ball.Kind);
                builder.Append(':');
                AppendInt(builder, ball.IsPocketed ? 1 : 0);
                builder.Append(':');
                AppendFloat(builder, ball.Position.X);
                builder.Append(':');
                AppendFloat(builder, ball.Position.Y);
                builder.Append(':');
                AppendFloat(builder, ball.Velocity.X);
                builder.Append(':');
                AppendFloat(builder, ball.Velocity.Y);
                builder.Append(':');
                AppendFloat(builder, ball.Spin.SideSpinRps);
                builder.Append(':');
                AppendFloat(builder, ball.Spin.ForwardSpinRps);
                builder.Append(':');
                AppendFloat(builder, ball.Spin.VerticalSpinRps);
            }

            foreach (var shotEvent in frame.Events.OrderBy(evt => evt.EventType).ThenBy(evt => evt.BallNumber).ThenBy(evt => evt.Detail, StringComparer.Ordinal))
            {
                builder.Append("|E:");
                AppendInt(builder, (int)shotEvent.EventType);
                builder.Append(':');
                AppendInt(builder, shotEvent.BallNumber ?? -1);
                builder.Append(':');
                builder.Append(shotEvent.Detail);
            }
        }

        return builder.ToString();
    }

    public static string BuildSha256(SimulationReplayTrace trace)
    {
        var fingerprint = Build(trace);
        var bytes = Encoding.UTF8.GetBytes(fingerprint);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(bytes);
        var builder = new StringBuilder(hashBytes.Length * 2);

        foreach (var hashByte in hashBytes)
        {
            builder.Append(hashByte.ToString("x2"));
        }

        return builder.ToString();
    }

    private static void AppendResolvedCueStrike(StringBuilder builder, ResolvedCueStrike cueStrike)
    {
        builder.Append("S:");
        AppendFloat(builder, cueStrike.AimDirection.X);
        builder.Append(':');
        AppendFloat(builder, cueStrike.AimDirection.Y);
        builder.Append(':');
        AppendFloat(builder, cueStrike.StrikeSpeedMetersPerSecond);
        builder.Append(':');
        AppendFloat(builder, cueStrike.TipOffsetNormalized.X);
        builder.Append(':');
        AppendFloat(builder, cueStrike.TipOffsetNormalized.Y);
        builder.Append(':');
        AppendFloat(builder, cueStrike.InitialVelocity.X);
        builder.Append(':');
        AppendFloat(builder, cueStrike.InitialVelocity.Y);
        builder.Append(':');
        AppendFloat(builder, cueStrike.InitialSpin.SideSpinRps);
        builder.Append(':');
        AppendFloat(builder, cueStrike.InitialSpin.ForwardSpinRps);
        builder.Append(':');
        AppendFloat(builder, cueStrike.InitialSpin.VerticalSpinRps);
    }

    private static void AppendFloat(StringBuilder builder, float value)
    {
        builder.Append(BitConverter.SingleToInt32Bits(value).ToString("X8"));
    }

    private static void AppendInt(StringBuilder builder, int value)
    {
        builder.Append(value);
    }
}
