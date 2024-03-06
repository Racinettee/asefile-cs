using Godot;
using System;
using System.Diagnostics;
using System.IO;
using Asefile;
using Asefile.Godot;

public partial class Node2D : Godot.Node2D
{
	private AnimatedSprite2D sprite;
	
	public override void _Ready()
	{
		Trace.WriteLine(Directory.GetCurrentDirectory());
		AseFile file = new AseFile("Chica.aseprite");
		var frames = SpriteFrameUtils.LoadFrames(file, "WalkDown", "IdleDown", "SpearDown");
		frames.SetAnimationSpeed("WalkDown", 1.0);
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		sprite.SpriteFrames = frames;
		sprite.Animation = new StringName("WalkDown");
		sprite.Play();
		ResourceSaver.Save(frames, "res://chicaFrames.tres");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
