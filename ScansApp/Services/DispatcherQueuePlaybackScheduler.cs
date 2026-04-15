using System;
using Microsoft.UI.Dispatching;

namespace ScansApp.Services;

public sealed class DispatcherQueuePlaybackScheduler : IPlaybackScheduler
{
    private readonly DispatcherQueueTimer timer;
    private Action? tick;

    public DispatcherQueuePlaybackScheduler(DispatcherQueue dispatcherQueue)
    {
        timer = dispatcherQueue.CreateTimer();
        timer.Tick += OnTick;
    }

    public void Start(TimeSpan interval, Action tick)
    {
        this.tick = tick ?? throw new ArgumentNullException(nameof(tick));
        timer.Interval = interval;

        if (!timer.IsRunning)
        {
            timer.Start();
        }
    }

    public void Stop()
    {
        if (timer.IsRunning)
        {
            timer.Stop();
        }

        tick = null;
    }

    private void OnTick(DispatcherQueueTimer sender, object args)
    {
        tick?.Invoke();
    }
}
