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
    private Sprite chicaFrame0;
    private AnimatedSprite chicaIdleDown;
    
    protected override void Initialize()
    {
        // TODO: Add your initialization logic here
        AseFile file = new AseFile("asset/chica.aseprite");
        chicaFrames = new FrameAtlas(file, _graphics.GraphicsDevice);
        chicaFrame0 = chicaFrames[0];
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
        GraphicsDevice.Clear(Color.CornflowerBlue);
        
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        
        chicaFrames.Draw(_spriteBatch, new Vector2(10, 10), scale: Vector2.One * 2);
        chicaFrame0.Draw(_spriteBatch, new Vector2(30, 80), scale: Vector2.One * 2);
        chicaIdleDown.Draw(_spriteBatch, new Vector2(30, 150), scale: Vector2.One * 2);

        // TODO: Add your drawing code here
        _spriteBatch.End();
        base.Draw(gameTime);
    }
}