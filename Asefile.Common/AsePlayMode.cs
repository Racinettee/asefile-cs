namespace Asefile.Common;

/// <summary>
/// AseAnimationDirection encapsulates the logic of stepping an animation to the next frame
/// </summary>
public abstract class AsePlayMode
{
    public int Frame { get; set; }

    public int TotalFrames { get; protected set; }
    public AsePlayMode(int maxFrames, int startingFrame = 0)
    {
        Frame = startingFrame;
        TotalFrames = maxFrames;
    }
    public abstract void Update();

    public static AsePlayMode FromEnum(AnimationDirection animMode, int maxFrames, int startingFrame = 0)
    {
        return animMode switch
        {
            AnimationDirection.Forward => new AseForwardMode(maxFrames, startingFrame),
            AnimationDirection.Reverse => new AseReverseMode(maxFrames, startingFrame),
            AnimationDirection.PingPong => new AsePingPongMode(maxFrames, startingFrame, false),
            AnimationDirection.PingPongReverse => new AsePingPongMode(maxFrames, startingFrame, true),
        };
    }
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