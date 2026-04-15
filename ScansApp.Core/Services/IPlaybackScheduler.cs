using System;

namespace ScansApp.Services;

public interface IPlaybackScheduler
{
    void Start(TimeSpan interval, Action tick);

    void Stop();
}
