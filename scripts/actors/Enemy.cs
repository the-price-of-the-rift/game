#nullable enable

using Godot;

public partial class Enemy : CharacterBody2D
{
	[Signal] public delegate void EnemyKilledEventHandler(Enemy enemy);

	[Export] public NodePath VisualPath { get; set; } = new NodePath();
	[Export] public float MoveSpeed { get; set; } = 52.0f;
	[Export] public float AttackRange { get; set; } = 22.0f;
	[Export] public float AttackCooldown { get; set; } = 1.0f;
	[Export] public float ContactDamage { get; set; } = 7.0f;
	[Export] public float MaxHealth { get; set; } = 28.0f;

	public int RiftTier { get; private set; } = 1;
	public bool IsElite { get; private set; } = false;
	public bool IsDead => currentHealth <= 0.0f;
	public bool IsScoutDebuffed => scoutDebuffTimer > 0.0f;
	public bool IsStunned => stunTimer > 0.0f;

	private float currentHealth;
	private float stunTimer = 0.0f;
	private float baseMoveSpeed;
	private float baseAttackCooldown;
	private float attackTimer = 0.0f;
	private float rangedAttackTimer = 0.0f;
	private float poisonTimer = 0.0f;
	private float poisonTicksLeft = 0.0f;
	private float scoutDebuffTimer = 0.0f;
	private float scoutPoisonTimer = 0.0f;
	private float scoutPoisonTickDamage = 1.0f;
	private float scoutPoisonTickInterval = 4.0f;
	private float scoutSlowMultiplier = 0.6f;
	private CanvasItem? visual;
	private Player? player;
	private Tween? hitTween;

	public override void _Ready()
	{
		AddToGroup("enemies");
		currentHealth = MaxHealth;
		visual = GetNodeOrNull<CanvasItem>(VisualPath);
		UpdateColor();
	}

	public void Configure(Player target, int tier, bool elite)
	{
		player = target;
		RiftTier = tier;
		IsElite = elite;
		MoveSpeed = 52.0f + (tier - 1) * 5.0f + (elite ? 12.0f : 0.0f);
		ContactDamage = 7.0f + (tier - 1) * 2.0f + (elite ? 4.0f : 0.0f);
		MaxHealth = 28.0f + (tier - 1) * 12.0f + (elite ? 20.0f : 0.0f);
		AttackCooldown = Mathf.Max(0.45f, 1.0f - tier * 0.04f);
		baseMoveSpeed = MoveSpeed;
		baseAttackCooldown = AttackCooldown;
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

		if (scoutDebuffTimer > 0.0f)
		{
			scoutDebuffTimer -= (float)delta;
			scoutPoisonTimer -= (float)delta;
			if (scoutPoisonTimer <= 0.0f)
			{
				scoutPoisonTimer = scoutPoisonTickInterval;
				TakeDamage(scoutPoisonTickDamage, false);
			}
		}

		if (stunTimer > 0.0f)
		{
			stunTimer -= (float)delta;
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		Vector2 toPlayer = player.GlobalPosition - GlobalPosition;
		float distance = toPlayer.Length();
		Velocity = distance > AttackRange ? toPlayer.Normalized() * GetCurrentMoveSpeed() : Vector2.Zero;
		MoveAndSlide();
		Rotation = Velocity.Angle();

		attackTimer -= (float)delta;
		rangedAttackTimer -= (float)delta;
		if (distance <= AttackRange + 2.0f && attackTimer <= 0.0f)
		{
			attackTimer = GetCurrentAttackCooldown();
			player.TakeDamage(ContactDamage);
		}
	}

	public void ApplyScoutDebuff(float slowMultiplier, float poisonDamage, float poisonInterval, float duration)
	{
		if (IsDead)
		{
			return;
		}

		scoutSlowMultiplier = slowMultiplier;
		scoutPoisonTickDamage = poisonDamage;
		scoutPoisonTickInterval = poisonInterval;
		scoutDebuffTimer = Mathf.Max(scoutDebuffTimer, duration);
		scoutPoisonTimer = Mathf.Min(scoutPoisonTimer <= 0.0f ? poisonInterval : scoutPoisonTimer, poisonInterval);
	}

	// The Axe's on-hit effect: freezes movement and attacks for `duration` seconds.
	// Stacks by taking the longer of the current and new duration, not adding them.
	public void ApplyStun(float duration)
	{
		if (IsDead)
		{
			return;
		}

		stunTimer = Mathf.Max(stunTimer, duration);
	}

	public bool CanUseRangedAttack()
	{
		return !IsDead && rangedAttackTimer <= 0.0f;
	}

	public void TriggerRangedAttack(float baseCooldown)
	{
		rangedAttackTimer = baseCooldown * (IsScoutDebuffed ? 2.0f : 1.0f);
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
			hitTween?.Kill();
			EmitSignal(SignalName.EnemyKilled, this);
			QueueFree();
			return;
		}

		PlayHitFlash();
	}

	private float GetCurrentMoveSpeed()
	{
		return IsScoutDebuffed ? baseMoveSpeed * scoutSlowMultiplier : baseMoveSpeed;
	}

	private float GetCurrentAttackCooldown()
	{
		return IsScoutDebuffed ? baseAttackCooldown * 2.0f : baseAttackCooldown;
	}

	private void UpdateColor()
	{
		if (visual == null)
		{
			return;
		}

		visual.Modulate = IsElite ? new Color(0.8f, 0.22f, 0.18f) : new Color(0.9f, 0.4f, 0.28f);
	}

	private void PlayHitFlash()
	{
		if (visual == null)
		{
			return;
		}

		hitTween?.Kill();
		Color baseColor = IsElite ? new Color(0.8f, 0.22f, 0.18f) : new Color(0.9f, 0.4f, 0.28f);
		Color flashColor = new Color(0.3f, 0.72f, 1.0f);
		visual.Modulate = baseColor;
		hitTween = CreateTween();
		hitTween.TweenProperty(visual, "modulate", flashColor, 0.18f);
		hitTween.TweenProperty(visual, "modulate", baseColor, 0.62f);
	}
}
