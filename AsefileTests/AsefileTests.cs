using Asefile;

namespace AsefileTests;

public class AsefileTests
{
    [Fact]
    public void AseHeaderReadsExpectedData()
    {
        AseFile file = new AseFile("asset/chica.aseprite");
        
        Assert.Equal(32, file.Width);
        Assert.Equal(32, file.Height);
        Assert.Equal(13, file.Frames.Count);
        Assert.Equal(32, file.ColorDepth);
        Assert.Equal(1, file.Header.PixelWidth);
        Assert.Equal(1, file.Header.PixelHeight);
        Assert.Equal(4, file.Header.GridXPos);
        Assert.Equal(0, file.Header.GridYPos);
        Assert.Equal(8, file.Header.GridWidth);
        Assert.Equal(8, file.Header.GridHeight);
    }
    [Fact]
    public void FrameDurationsExpectedValues()
    {
        AseFile file = new AseFile("asset/chica.aseprite");

        var frames = file.Frames; 
        Assert.Equal(100u, frames[0].FrameDuration);
        Assert.Equal(500u, frames[1].FrameDuration);
        Assert.Equal(200u, frames[2].FrameDuration);
        Assert.Equal(250u, frames[3].FrameDuration);
        Assert.Equal(100u, frames[4].FrameDuration);
    }
    [Fact]
    public void FrameTagsData()
    {
        AseFile file = new AseFile("asset/chica.aseprite");
        
        // The tags are in the first frame
        var tags = file.Frames[0].Tags;
        
        // There are 3 tags
        Assert.Equal(3, tags.Count);
        Assert.Equal("IdleDown", tags[0].TagName);
        Assert.Equal("WalkDown", tags[1].TagName);
        Assert.Equal("SpearDown", tags[2].TagName);
        // In the UI the frames are shown starting from 1, in the binary data it starts from 0
        Assert.Equal(1, tags[0].FromFrame); // from frame 2, to frame 6
        Assert.Equal(5, tags[0].ToFrame);
        Assert.Equal(0, tags[0].LoopDirection);
        Assert.Equal(0, tags[0].RepeatTimes);
        
        Assert.Equal(6, tags[1].FromFrame); // from frame 7, to frame 8
        Assert.Equal(7, tags[1].ToFrame);
        Assert.Equal(2, tags[1].LoopDirection);
        Assert.Equal(5, tags[1].RepeatTimes);
    }
    [Fact]
    public void LayerData()
    {
        AseFile file = new AseFile("asset/chica.aseprite");
        
        // Layers are in the first frame
        var layers = file.Frames[0].Layers;
        Assert.Equal(5, layers.Count);
        // The layers are ordered from the back-most to front-most, back-most being the layer at the bottom
        // of the layer view in the aseprite ui
        Assert.Equal("Ornament", layers[0].LayerName);
        Assert.Equal("Group 1", layers[1].LayerName);
        Assert.Equal("Layer 1", layers[2].LayerName);
        Assert.Equal("TestLayer", layers[3].LayerName);
        Assert.Equal("Base/Hair", layers[4].LayerName);
        // 75%, the percentage value is of 255
        Assert.Equal(75 * 255 / 100 /* 191 */, layers[3].Opacity);
        Assert.Equal(BlendMode.Lighten /* lighten */, layers[3].BlendMode);
    }
    
}