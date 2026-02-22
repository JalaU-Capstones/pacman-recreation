using System;
using System.Diagnostics;

namespace PacmanGame.Helpers;

/// <summary>
/// Lightweight FPS counter for UI overlays.
/// Counts "frames" based on calls to <see cref="OnFrame"/> (typically once per render/update tick).
/// </summary>
public sealed class FpsCounter
{
    private readonly TimeSpan _publishInterval;
    private int _frames;
    private long _lastTimestamp;

    public FpsCounter(TimeSpan? publishInterval = null)
    {
        _publishInterval = publishInterval ?? TimeSpan.FromMilliseconds(500);
        _lastTimestamp = Stopwatch.GetTimestamp();
    }

    public int? OnFrame()
    {
        _frames++;
        var now = Stopwatch.GetTimestamp();
        var elapsedSeconds = (now - _lastTimestamp) / (double)Stopwatch.Frequency;
        if (elapsedSeconds < _publishInterval.TotalSeconds)
        {
            return null;
        }

        var fps = (int)Math.Round(_frames / Math.Max(0.0001, elapsedSeconds));
        _frames = 0;
        _lastTimestamp = now;
        return fps;
    }
}
