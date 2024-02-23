namespace Asefile.Common;

/// <summary>
/// AseAnimationDirection encapsulates the logic of stepping an animation to the next frame
/// </summary>
public abstract class AsePlayMode
{
    public int Frame { get; protected set; }

    public int TotalFrames { get; protected set; }
    public AsePlayMode(int maxFrames, int startingFrame = 0)
    {
        Frame = startingFrame;
        TotalFrames = maxFrames;
    }
    public abstract void Update();
}

/// <summary>
/// When an animation uses this animationdirection the frames will progress forward normally
/// </summary>
public class AseForwardMode : AsePlayMode
{
    public AseForwardMode(int maxFrames, int startingFrame = 0)
        : base(maxFrames, startingFrame)
    {
    }
    public override void Update()
    {
        Frame++;
        if (Frame >= TotalFrames)
            Frame = 0;
    }
}

/// <summary>
/// When an animation uses this animation direction the frames will progress backwards
/// </summary>
public class AseReverseMode : AsePlayMode
{
    public AseReverseMode(int maxFrames, int startingFrame = 0)
        : base(maxFrames, startingFrame)
    {
    }

    public override void Update()
    {
        Frame--;
        if (Frame < 0)
            Frame = TotalFrames - 1;
    }
}

/// <summary>
/// When using Ping-Pong mode an animation will progress to the end and then back to the beginning
/// </summary>
public class AsePingPongMode : AsePlayMode
{
    public AsePingPongMode(int maxFrames, int startingFrame = 0, bool reverse = false)
        : base(maxFrames, startingFrame)
    {
        _playingForward = !reverse;
    }
    
    private bool _playingForward = true;
    public override void Update()
    {
        if (_playingForward)
        {
            Frame++;
            if (Frame >= TotalFrames)
            {
                _playingForward = false;
                Frame--; // Ensure we don't overshoot
            }
        }
        else
        {
            Frame--;
            if (Frame < 0)
            {
                _playingForward = true;
                Frame++; // Ensure we're in bounds
            }
        }
    }
}