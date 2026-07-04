#nullable enable

using Godot;

public partial class ToxicArea : Area2D
{
	[Export] public NodePath VisualPath { get; set; } = new NodePath();
	[Export] public float Radius { get; set; } = 26.0f;
	[Export] public float Duration { get; set; } = 6.0f;
	[Export] public float DamagePerTick { get; set; } = 4.0f;
	[Export] public float TickInterval { get; set; } = 0.5f;

	private CanvasItem? visual;
	private CollisionShape2D? collisionShape;
	private float tickTimer = 0.0f;

	public override void _Ready()
	{
		AddToGroup("toxic_areas");
		visual = GetNodeOrNull<CanvasItem>(VisualPath);
		collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		ApplyRadius();
		tickTimer = TickInterval;
	}

	public override void _Process(double delta)
	{
		Duration -= (float)delta;
		tickTimer -= (float)delta;

		if (visual != null)
		{
			Color modulate = visual.Modulate;
			modulate.A = Mathf.Clamp(Duration / 6.0f, 0.18f, 0.48f);
			visual.Modulate = modulate;
		}

		if (Duration <= 0.0f)
		{
			QueueFree();
		}
	}

	public bool CanTick()
	{
		if (tickTimer > 0.0f)
		{
			return false;
		}

		tickTimer = TickInterval;
		return true;
	}

	private void ApplyRadius()
	{
		if (collisionShape?.Shape is CircleShape2D circle)
		{
			circle.Radius = Radius;
		}

		if (visual is Node2D visualNode)
		{
			float scale = Radius / 32.0f;
			visualNode.Scale = new Vector2(scale, scale);
		}
	}
}
