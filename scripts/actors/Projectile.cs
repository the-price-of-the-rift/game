#nullable enable

using Godot;
using System.Collections.Generic;

public partial class Projectile : Area2D
{
	[Export] public NodePath VisualPath { get; set; } = new NodePath();
	[Export] public NodePath ShapeCastPath { get; set; } = new NodePath();
	[Export] public float Speed { get; set; } = 240.0f;
	[Export] public float Damage { get; set; } = 8.0f;
	[Export] public float Lifetime { get; set; } = 2.0f;
	[Export] public bool FromPlayer { get; set; } = true;
	[Export] public bool CanPierce { get; set; } = false;
	[Export] public bool AppliesPoison { get; set; } = false;
	[Export] public bool AppliesScoutDebuff { get; set; } = false;
	[Export] public float ScoutSlowMultiplier { get; set; } = 0.6f;
	[Export] public float ScoutPoisonTickDamage { get; set; } = 1.0f;
	[Export] public float ScoutPoisonTickInterval { get; set; } = 4.0f;
	[Export] public float ScoutDebuffDuration { get; set; } = 8.0f;
	[Export] public Color Tint { get; set; } = new Color(0.95f, 0.8f, 0.2f);

	public Vector2 Direction { get; set; } = Vector2.Right;

	private readonly HashSet<Node2D> hitTargets = new();
	private CanvasItem? visual;
	private ShapeCast2D? shapeCast;

	public override void _Ready()
	{
		ConfigureCollisionMask();
		BodyEntered += OnBodyEntered;
		AreaEntered += OnAreaEntered;
		visual = GetNodeOrNull<CanvasItem>(VisualPath);
		shapeCast = GetNodeOrNull<ShapeCast2D>(ShapeCastPath);
		ConfigureShapeCastMask();
		if (visual != null)
		{
			visual.Modulate = Tint;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 motion = Direction.Normalized() * Speed * (float)delta;
		CheckShapeCastHits(motion);
		if (!IsInstanceValid(this))
		{
			return;
		}

		GlobalPosition += motion;
		Lifetime -= (float)delta;
		Rotation = Direction.Angle();
		CheckOverlapHits();

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

		if (FromPlayer && body is Enemy enemy)
		{
			HitEnemy(enemy);
			return;
		}

		if (!FromPlayer && body is Player player)
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

	private void HitEnemy(Enemy enemy)
	{
		if (hitTargets.Contains(enemy))
		{
			return;
		}

		hitTargets.Add(enemy);
		if (AppliesScoutDebuff)
		{
			enemy.ApplyScoutDebuff(ScoutSlowMultiplier, ScoutPoisonTickDamage, ScoutPoisonTickInterval, ScoutDebuffDuration);
		}
		enemy.TakeDamage(Damage, AppliesPoison);

		if (!CanPierce)
		{
			QueueFree();
		}
	}

	private void ConfigureCollisionMask()
	{
		SetCollisionMaskValue(1, true);
		SetCollisionMaskValue(2, !FromPlayer);
		SetCollisionMaskValue(3, FromPlayer);
		SetCollisionMaskValue(4, false);
	}

	private void CheckOverlapHits()
	{
		if (FromPlayer)
		{
			foreach (Node2D body in GetOverlappingBodies())
			{
				if (body is Enemy enemy)
				{
					HitEnemy(enemy);
					if (!CanPierce || !IsInstanceValid(this))
					{
						return;
					}
				}
			}
			return;
		}

		foreach (Node2D body in GetOverlappingBodies())
		{
			if (body is Player player)
			{
				player.TakeDamage(Damage);
				QueueFree();
				return;
			}
		}
	}

	private void CheckShapeCastHits(Vector2 motion)
	{
		if (shapeCast == null)
		{
			return;
		}

		shapeCast.TargetPosition = motion;
		shapeCast.ForceShapecastUpdate();

		for (int index = 0; index < shapeCast.GetCollisionCount(); index++)
		{
			GodotObject collider = shapeCast.GetCollider(index);
			if (collider is StaticBody2D)
			{
				QueueFree();
				return;
			}

			if (FromPlayer && collider is Enemy enemy)
			{
				HitEnemy(enemy);
				if (!CanPierce || !IsInstanceValid(this))
				{
					return;
				}
			}

			if (!FromPlayer && collider is Player player)
			{
				player.TakeDamage(Damage);
				QueueFree();
				return;
			}
		}
	}

	private void ConfigureShapeCastMask()
	{
		if (shapeCast == null)
		{
			return;
		}

		shapeCast.SetCollisionMaskValue(1, true);
		shapeCast.SetCollisionMaskValue(2, !FromPlayer);
		shapeCast.SetCollisionMaskValue(3, FromPlayer);
		shapeCast.SetCollisionMaskValue(4, false);
	}
}
