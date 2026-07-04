#nullable enable

using Godot;
using System.Collections.Generic;

public partial class Projectile : Area2D
{
	[Export] public NodePath VisualPath { get; set; } = new NodePath();
	[Export] public float Speed { get; set; } = 240.0f;
	[Export] public float Damage { get; set; } = 8.0f;
	[Export] public float Lifetime { get; set; } = 2.0f;
	[Export] public bool FromPlayer { get; set; } = true;
	[Export] public bool CanPierce { get; set; } = false;
	[Export] public bool AppliesPoison { get; set; } = false;
	[Export] public Color Tint { get; set; } = new Color(0.95f, 0.8f, 0.2f);

	public Vector2 Direction { get; set; } = Vector2.Right;

	private readonly HashSet<Node2D> hitTargets = new();
	private CanvasItem? visual;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		AreaEntered += OnAreaEntered;
		visual = GetNodeOrNull<CanvasItem>(VisualPath);
		if (visual != null)
		{
			visual.Modulate = Tint;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		GlobalPosition += Direction.Normalized() * Speed * (float)delta;
		Lifetime -= (float)delta;
		Rotation = Direction.Angle();

		if (Lifetime <= 0.0f)
		{
			QueueFree();
		}
	}

	private void OnBodyEntered(Node body)
	{
		if (body == null)
		{
			return;
		}

		if (body is StaticBody2D)
		{
			QueueFree();
			return;
		}

		if (FromPlayer && body is EnemyPrototype enemy)
		{
			HitEnemy(enemy);
			return;
		}

		if (!FromPlayer && body is PlayerPrototype player)
		{
			player.TakeDamage(Damage);
			QueueFree();
		}
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area.GetParent() is StaticBody2D)
		{
			QueueFree();
		}
	}

	private void HitEnemy(EnemyPrototype enemy)
	{
		if (hitTargets.Contains(enemy))
		{
			return;
		}

		hitTargets.Add(enemy);
		enemy.TakeDamage(Damage, AppliesPoison);

		if (!CanPierce)
		{
			QueueFree();
		}
	}
}
