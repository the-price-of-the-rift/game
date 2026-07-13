#nullable enable

using Godot;

public partial class Weapon : Node2D
{
	[Export] public NodePath WarriorVisualPath { get; set; } = new NodePath();
	[Export] public NodePath RangerVisualPath { get; set; } = new NodePath();
	[Export] public NodePath ScoutVisualPath { get; set; } = new NodePath();
	[Export] public float LightSwingAngleDegrees { get; set; } = 28.0f;
	[Export] public float HeavySwingAngleDegrees { get; set; } = 46.0f;
	[Export] public float SwingSpeed { get; set; } = 4.5f;
	[Export] public float HitFlashDuration { get; set; } = 0.12f;
	[Export] public Color HitFlashColor { get; set; } = new Color(1.0f, 0.92f, 0.52f);
	[Export] public Color IdleColor { get; set; } = Colors.White;

	private BuildBranch branch = BuildBranch.None;
	private Node2D? warriorVisual;
	private Node2D? rangerVisual;
	private Node2D? scoutVisual;
	private Node2D? activeVisual;
	private float swingTimer = 0.0f;
	private float swingAngleRadians = 0.0f;
	private float flashTimer = 0.0f;

	public override void _Ready()
	{
		warriorVisual = GetNodeOrNull<Node2D>(WarriorVisualPath);
		rangerVisual = GetNodeOrNull<Node2D>(RangerVisualPath);
		scoutVisual = GetNodeOrNull<Node2D>(ScoutVisualPath);
		ApplyBranchVisual();
	}

	public override void _Process(double delta)
	{
		swingTimer = Mathf.Max(0.0f, swingTimer - (float)delta * SwingSpeed);
		flashTimer = Mathf.Max(0.0f, flashTimer - (float)delta);
		UpdateVisualPose();
	}

	public void AimAt(Vector2 target, int nextBranch)
	{
		branch = (BuildBranch)nextBranch;
		ApplyBranchVisual();
		LookAt(target);
	}

	public void Swing(bool heavy = false)
	{
		swingTimer = 1.0f;
		swingAngleRadians = Mathf.DegToRad(heavy ? HeavySwingAngleDegrees : LightSwingAngleDegrees);
		flashTimer = 0.0f;
	}

	public void FlashHit()
	{
		flashTimer = HitFlashDuration;
		UpdateVisualPose();
	}

	private void ApplyBranchVisual()
	{
		SetVisible(warriorVisual, branch == BuildBranch.Warrior || branch == BuildBranch.None);
		SetVisible(rangerVisual, branch == BuildBranch.Ranger);
		SetVisible(scoutVisual, branch == BuildBranch.Scout);

		activeVisual = branch switch
		{
			BuildBranch.Ranger => rangerVisual,
			BuildBranch.Scout => scoutVisual,
			_ => warriorVisual,
		};

		UpdateVisualPose();
	}

	private void UpdateVisualPose()
	{
		if (activeVisual == null)
		{
			return;
		}

		float swingOffset = Mathf.Sin((1.0f - swingTimer) * Mathf.Pi) * swingAngleRadians;
		activeVisual.Rotation = swingOffset;
		activeVisual.Scale = flashTimer > 0.0f ? new Vector2(1.08f, 1.08f) : Vector2.One;
		activeVisual.Modulate = flashTimer > 0.0f ? HitFlashColor : IdleColor;
	}

	private static void SetVisible(Node2D? node, bool visible)
	{
		if (node != null)
		{
			node.Visible = visible;
		}
	}
}
