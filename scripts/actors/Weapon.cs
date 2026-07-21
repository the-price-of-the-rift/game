#nullable enable

using Godot;

public partial class Weapon : Node2D
{
	[Export] public NodePath WarriorVisualPath { get; set; } = new NodePath();
	[Export] public NodePath RangerVisualPath { get; set; } = new NodePath();
	[Export] public NodePath ScoutVisualPath { get; set; } = new NodePath();
	// Each branch's single dominant shape (the blade / bow arc / dagger polygon) - the
	// part that gets reshaped per equipped weapon. Everything else in the visual (guard,
	// bowstring, handle) stays fixed, so a weapon swap only needs to touch one polygon.
	[Export] public NodePath WarriorBladePath { get; set; } = new NodePath();
	[Export] public NodePath RangerBowArcPath { get; set; } = new NodePath();
	[Export] public NodePath ScoutDaggerPath { get; set; } = new NodePath();
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
	private Polygon2D? warriorBlade;
	private Polygon2D? rangerBowArc;
	private Polygon2D? scoutDagger;
	private float swingTimer = 0.0f;
	private float swingAngleRadians = 0.0f;
	private float flashTimer = 0.0f;

	public override void _Ready()
	{
		warriorVisual = GetNodeOrNull<Node2D>(WarriorVisualPath);
		rangerVisual = GetNodeOrNull<Node2D>(RangerVisualPath);
		scoutVisual = GetNodeOrNull<Node2D>(ScoutVisualPath);
		warriorBlade = GetNodeOrNull<Polygon2D>(WarriorBladePath);
		rangerBowArc = GetNodeOrNull<Polygon2D>(RangerBowArcPath);
		scoutDagger = GetNodeOrNull<Polygon2D>(ScoutDaggerPath);
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

	// Recolors the equipped weapon's idle tint (a blacksmith purchase) and refreshes the
	// visual immediately instead of waiting for the next _Process tick.
	public void SetIdleColor(Color color)
	{
		IdleColor = color;
		UpdateVisualPose();
	}

	// Reshapes the given branch's dominant polygon to a purchased weapon's silhouette
	// (e.g. a slim sword blade vs. a flared axe head). No-op if the shape is empty, so
	// callers can pass WeaponDefinition.ShapePoints without checking branch support.
	public void SetShape(BuildBranch forBranch, Vector2[] points)
	{
		if (points.Length == 0)
		{
			return;
		}

		Polygon2D? target = forBranch switch
		{
			BuildBranch.Warrior => warriorBlade,
			BuildBranch.Ranger => rangerBowArc,
			BuildBranch.Scout => scoutDagger,
			_ => null,
		};

		if (target != null)
		{
			target.Polygon = points;
		}
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
