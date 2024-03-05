using Asefile;
using AsefileMG;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AsefileMGExample;

public class AsefileMGExample : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    public AsefileMGExample()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    private FrameAtlas chicaFrames;
    private AseSprite chicaFrame0;
    private AseSprite chicaFrame9;
    private AseSprite chicaFrame10;
    private AseAnimatedSprite chicaIdleDown;
    
    protected override void Initialize()
    {
        // TODO: Add your initialization logic here
        AseFile file = new AseFile("asset/chica.aseprite");
        chicaFrames = new FrameAtlas(file, _graphics.GraphicsDevice);
        chicaFrame0 = chicaFrames[0];
        chicaFrame9 = chicaFrames[9];
        chicaFrame10 = chicaFrames[10];
        chicaIdleDown = chicaFrames.GetAnimation("IdleDown");
        chicaIdleDown.Play();
        base.Initialize();
    }
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // TODO: use this.Content to load your game content here
    }
    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here
        chicaIdleDown.Update(gameTime);

        base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Gray);
        
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);//, blendState: BlendState.AlphaBlend);
        
        chicaFrames.Draw(_spriteBatch, new Vector2(10, 10), scale: Vector2.One * 2);
        chicaFrame0.Draw(_spriteBatch, new Vector2(30, 80), scale: Vector2.One * 2);
        chicaIdleDown.Draw(_spriteBatch, new Vector2(78, 80), scale: Vector2.One * 2);
        chicaFrame9.Draw(_spriteBatch, new Vector2(112, 80), scale: Vector2.One * 6);
        chicaFrame10.Draw(_spriteBatch, new Vector2(220, 80), scale: Vector2.One * 6);

        _spriteBatch.End();
        base.Draw(gameTime);
    }
}