using Godot;
using System;

public partial class Player : CharacterBody2D
{
	private Sprite2D sprite;
	private float moveSpeed = 75.0f;
	private double walkTimer = 0.0f;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		sprite = GetNode<Sprite2D>("Sprite2D");
		GD.Print("Player loaded");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = Vector2.Zero;

		direction.X = Input.GetActionStrength("right") - Input.GetActionStrength("left");
		direction.Y = Input.GetActionStrength("down") - Input.GetActionStrength("up");
		Velocity = direction * moveSpeed;

		MoveAndSlide();

		if (Velocity.Length() <= 0) {
			sprite.Frame = 0;
			walkTimer = 0.0f;

			return;
		}

		walkTimer = walkTimer + delta + 1.0f;

		if (walkTimer >= 12.0f) {
			walkTimer = walkTimer - 12.0f;
			sprite.Frame = (sprite.Frame + 1) % sprite.Hframes;
		}
	}
}
