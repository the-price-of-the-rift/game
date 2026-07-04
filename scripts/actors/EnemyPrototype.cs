#nullable enable

using Godot;

public partial class EnemyPrototype : CharacterBody2D
{
	[Signal] public delegate void EnemyKilledEventHandler(EnemyPrototype enemy);

	[Export] public NodePath VisualPath { get; set; } = new NodePath();
	[Export] public float MoveSpeed { get; set; } = 52.0f;
	[Export] public float AttackRange { get; set; } = 22.0f;
	[Export] public float AttackCooldown { get; set; } = 1.0f;
	[Export] public float ContactDamage { get; set; } = 7.0f;
	[Export] public float MaxHealth { get; set; } = 28.0f;

	public int RiftTier { get; private set; } = 1;
	public bool IsElite { get; private set; } = false;
	public bool IsDead => currentHealth <= 0.0f;

	private float currentHealth;
	private float attackTimer = 0.0f;
	private float poisonTimer = 0.0f;
	private float poisonTicksLeft = 0.0f;
	private CanvasItem? visual;
	private PlayerPrototype? player;

	public override void _Ready()
	{
		AddToGroup("enemies");
		currentHealth = MaxHealth;
		visual = GetNodeOrNull<CanvasItem>(VisualPath);
		UpdateColor();
	}

	public void Configure(PlayerPrototype target, int tier, bool elite)
	{
		player = target;
		RiftTier = tier;
		IsElite = elite;
		MoveSpeed = 52.0f + (tier - 1) * 5.0f + (elite ? 12.0f : 0.0f);
		ContactDamage = 7.0f + (tier - 1) * 2.0f + (elite ? 4.0f : 0.0f);
		MaxHealth = 28.0f + (tier - 1) * 12.0f + (elite ? 20.0f : 0.0f);
		AttackCooldown = Mathf.Max(0.45f, 1.0f - tier * 0.04f);
		currentHealth = MaxHealth;
		Scale = elite ? new Vector2(1.2f, 1.2f) : Vector2.One;
		UpdateColor();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsDead || player == null || !IsInstanceValid(player))
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		if (poisonTicksLeft > 0.0f)
		{
			poisonTimer -= (float)delta;
			if (poisonTimer <= 0.0f)
			{
				poisonTimer = 0.5f;
				poisonTicksLeft -= 1.0f;
				TakeDamage(3.0f, false);
			}
		}

		Vector2 toPlayer = player.GlobalPosition - GlobalPosition;
		float distance = toPlayer.Length();
		Velocity = distance > AttackRange ? toPlayer.Normalized() * MoveSpeed : Vector2.Zero;
		MoveAndSlide();
		Rotation = Velocity.Angle();

		attackTimer -= (float)delta;
		if (distance <= AttackRange + 2.0f && attackTimer <= 0.0f)
		{
			attackTimer = AttackCooldown;
			player.TakeDamage(ContactDamage);
		}
	}

	public void TakeDamage(float damage, bool applyPoison)
	{
		if (IsDead)
		{
			return;
		}

		currentHealth -= damage;
		if (applyPoison)
		{
			poisonTicksLeft = 4.0f;
			poisonTimer = 0.3f;
		}

		if (currentHealth <= 0.0f)
		{
			currentHealth = 0.0f;
			EmitSignal(SignalName.EnemyKilled, this);
			QueueFree();
		}
	}

	private void UpdateColor()
	{
		if (visual == null)
		{
			return;
		}

		visual.Modulate = IsElite ? new Color(0.8f, 0.22f, 0.18f) : new Color(0.9f, 0.4f, 0.28f);
	}
}
