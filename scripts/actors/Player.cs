#nullable enable

using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
	[Signal] public delegate void StatsChangedEventHandler();
	[Signal] public delegate void AbilityUnlockedEventHandler(string abilityId);
	[Signal] public delegate void PlayerDiedEventHandler();

	[Export] public NodePath SpritePath { get; set; } = new NodePath();
	[Export] public NodePath WeaponPath { get; set; } = new NodePath();
	[Export] public NodePath ProjectileOriginPath { get; set; } = new NodePath();
	[Export] public PackedScene ProjectileScene { get; set; } = null!;

	public BuildBranch ChosenBranch { get; private set; } = BuildBranch.None;
	public int Level { get; private set; } = 1;
	public int Xp { get; private set; } = 0;
	public int Reputation { get; private set; } = 12;
	public int MagicStones { get; private set; } = 3;
	public float CurrentHealth => currentHealth;
	public float MaxHealth => maxHealth;
	public float CurrentShield => currentShield;
	public float MaxShield => maxShield;
	public bool InputLocked { get; set; } = false;
	public string LastTreeMessage { get; private set; } = "Open the tree with B and pick a branch.";

	private const float BaseMeleeDamage = 12.0f;
	private const float BaseRangedDamage = 10.0f;
	private const float RangerBaseHealth = 50.0f;
	private const float WarriorHealthBonus = 20.0f;
	private const float ScoutHealthPenalty = 10.0f;
	private const float RangerBaseMoveSpeed = 108.0f;
	private const float WarriorMovePenalty = 18.0f;
	private const float ScoutMoveBonus = 22.0f;
	private const float BaseCritChance = 0.08f;
	private const float BaseCritMultiplier = 1.6f;
	private const float ShieldRechargeDelay = 3.0f;
	private const float ShieldRechargeRate = 4.0f;
	private const float DashDuration = 0.18f;
	private const float DashDistance = 148.0f;
	private const float DashHitRadius = 24.0f;

	private readonly HashSet<string> unlockedAbilities = new();
	private readonly Dictionary<string, float> cooldowns = new();
	private readonly HashSet<Enemy> dashHitEnemies = new();

	private Sprite2D? sprite;
	private Weapon? weapon;
	private Node2D? projectileOrigin;
	private Node? projectileContainer;
	private double walkTimer = 0.0f;
	private float currentHealth = 50.0f;
	private float maxHealth = 50.0f;
	private float currentShield = 0.0f;
	private float maxShield = 0.0f;
	private float shieldRechargeTimer = 0.0f;
	private float attackTimer = 0.0f;
	private float quickFireTimer = 0.0f;
	private float criticalFireTimer = 0.0f;
	private float enemyCrashTimer = 0.0f;
	private float invulnerabilityTimer = 0.0f;
	private float dashTimer = 0.0f;
	private float dashDamage = 0.0f;
	private Vector2 dashDirection = Vector2.Zero;

	public override void _Ready()
	{
		AddToGroup("player");
		sprite = GetNodeOrNull<Sprite2D>(SpritePath);
		weapon = GetNodeOrNull<Weapon>(WeaponPath);
		projectileOrigin = GetNodeOrNull<Node2D>(ProjectileOriginPath);
		projectileContainer = GetParent();
		currentHealth = maxHealth;
		RefreshStats();
	}

	public override void _PhysicsProcess(double delta)
	{
		TickTimers((float)delta);
		UpdateMovement(delta);
		UpdateAiming();

		if (!InputLocked && currentHealth > 0.0f)
		{
			HandleAttack();
			HandleActiveAbilities();
		}

		RechargeShield((float)delta);
		EmitSignal(SignalName.StatsChanged);
	}

	public void SetProjectileContainer(Node container)
	{
		projectileContainer = container;
	}

	public bool HasAbility(string abilityId)
	{
		return unlockedAbilities.Contains(abilityId);
	}

	public IReadOnlyCollection<string> GetUnlockedAbilities()
	{
		return unlockedAbilities;
	}

	public bool CanUnlock(string abilityId)
	{
		if (!AbilityDefinitions.All.TryGetValue(abilityId, out AbilityDefinition? definition) || unlockedAbilities.Contains(abilityId))
		{
			return false;
		}

		if (MagicStones < definition.Cost || Level < definition.RequiredLevel || Reputation < definition.RequiredReputation)
		{
			return false;
		}

		if (definition.Branch != BuildBranch.None && ChosenBranch != BuildBranch.None && definition.Branch != ChosenBranch)
		{
			return false;
		}

		foreach (string prerequisite in definition.Prerequisites)
		{
			if (!unlockedAbilities.Contains(prerequisite))
			{
				return false;
			}
		}

		if (definition.Id == "strikes_of_justice" && !HasAnyAbility("kings_blessing", "kings_courtesy", "kings_pardon"))
		{
			return false;
		}

		if (definition.Id == "royal_order" && !HasAnyAbility("strikes_of_judgement", "arrows_of_vengeance", "payment_time"))
		{
			return false;
		}

		return true;
	}

	public bool TryUnlock(string abilityId)
	{
		if (!AbilityDefinitions.All.TryGetValue(abilityId, out AbilityDefinition? definition))
		{
			LastTreeMessage = "Unknown ability.";
			return false;
		}

		if (unlockedAbilities.Contains(abilityId))
		{
			LastTreeMessage = definition.DisplayName + " is already unlocked.";
			return false;
		}

		if (definition.Branch != BuildBranch.None && ChosenBranch != BuildBranch.None && definition.Branch != ChosenBranch)
		{
			LastTreeMessage = "You already committed to the " + AbilityDefinitions.GetBranchName(ChosenBranch) + " build.";
			return false;
		}

		foreach (string prerequisite in definition.Prerequisites)
		{
			if (!unlockedAbilities.Contains(prerequisite))
			{
				LastTreeMessage = "Locked: requires " + AbilityDefinitions.All[prerequisite].DisplayName + ".";
				return false;
			}
		}

		if (Level < definition.RequiredLevel)
		{
			LastTreeMessage = "Locked until level " + definition.RequiredLevel + ".";
			return false;
		}

		if (Reputation < definition.RequiredReputation)
		{
			LastTreeMessage = "Need " + definition.RequiredReputation + " reputation to learn this village skill.";
			return false;
		}

		if (MagicStones < definition.Cost)
		{
			LastTreeMessage = "Need " + definition.Cost + " magic stone(s).";
			return false;
		}

		MagicStones -= definition.Cost;
		unlockedAbilities.Add(abilityId);
		if (ChosenBranch == BuildBranch.None && definition.Branch != BuildBranch.None)
		{
			ChosenBranch = definition.Branch;
		}

		RefreshStats();
		LastTreeMessage = "Unlocked " + definition.DisplayName + ".";
		EmitSignal(SignalName.AbilityUnlocked, abilityId);
		EmitSignal(SignalName.StatsChanged);
		return true;
	}

	// NPC-taught skill: grants an active for free (no magic stones, no level/reputation gate),
	// but still respects the branch commit and prerequisites so teaching stays coherent.
	public bool FreeUnlock(string abilityId)
	{
		if (!AbilityDefinitions.All.TryGetValue(abilityId, out AbilityDefinition? definition))
		{
			LastTreeMessage = "Unknown ability.";
			return false;
		}

		if (unlockedAbilities.Contains(abilityId))
		{
			LastTreeMessage = definition.DisplayName + " is already learned.";
			return false;
		}

		if (definition.Branch != BuildBranch.None && ChosenBranch != BuildBranch.None && definition.Branch != ChosenBranch)
		{
			LastTreeMessage = "You already committed to the " + AbilityDefinitions.GetBranchName(ChosenBranch) + " build.";
			return false;
		}

		foreach (string prerequisite in definition.Prerequisites)
		{
			if (!unlockedAbilities.Contains(prerequisite))
			{
				LastTreeMessage = "You are not ready to learn " + definition.DisplayName + " yet.";
				return false;
			}
		}

		unlockedAbilities.Add(abilityId);
		if (ChosenBranch == BuildBranch.None && definition.Branch != BuildBranch.None)
		{
			ChosenBranch = definition.Branch;
		}

		RefreshStats();
		LastTreeMessage = "Learned " + definition.DisplayName + ".";
		EmitSignal(SignalName.AbilityUnlocked, abilityId);
		EmitSignal(SignalName.StatsChanged);
		return true;
	}

	public void GainXp(int amount)
	{
		Xp += amount;
		bool leveled = false;

		if (Level < 2 && Xp >= 25)
		{
			Level = 2;
			Reputation += 15;
			maxHealth += 5.0f;
			currentHealth += 5.0f;
			leveled = true;
		}

		if (Level < 3 && Xp >= 60)
		{
			Level = 3;
			Reputation += 10;
			maxHealth += 10.0f;
			currentHealth += 10.0f;
			leveled = true;
		}

		if (leveled)
		{
			RefreshStats();
			LastTreeMessage = "Reached level " + Level + ". New passive gate unlocked.";
		}

		EmitSignal(SignalName.StatsChanged);
	}

	public void GainMagicStones(int amount)
	{
		MagicStones += amount;
		EmitSignal(SignalName.StatsChanged);
	}

	public void GainReputation(int amount)
	{
		Reputation += amount;
		LastTreeMessage = "Village reputation is now " + Reputation + ".";
		EmitSignal(SignalName.StatsChanged);
	}

	public void SetLastTreeMessage(string message)
	{
		LastTreeMessage = message;
		EmitSignal(SignalName.StatsChanged);
	}

	public void HealFull()
	{
		currentHealth = maxHealth;
		currentShield = maxShield;
		EmitSignal(SignalName.StatsChanged);
	}

	public void TakeDamage(float amount)
	{
		if (currentHealth <= 0.0f || invulnerabilityTimer > 0.0f)
		{
			return;
		}

		if (HasAbility("kings_pardon") && GD.Randf() <= 0.10f)
		{
			LastTreeMessage = "King's Pardon ignored the hit.";
			return;
		}

		shieldRechargeTimer = ShieldRechargeDelay;
		float remaining = amount;

		if (currentShield > 0.0f)
		{
			float absorbed = Mathf.Min(currentShield, remaining);
			currentShield -= absorbed;
			remaining -= absorbed;
		}

		if (remaining > 0.0f)
		{
			currentHealth -= remaining;
			invulnerabilityTimer = 0.15f;
		}

		if (currentHealth <= 0.0f)
		{
			currentHealth = 0.0f;
			LastTreeMessage = "You fell inside the rift.";
			EmitSignal(SignalName.PlayerDied);
		}

		EmitSignal(SignalName.StatsChanged);
	}

	public float GetActiveCooldownMultiplier()
	{
		if (HasAbility("noble_directive") || HasAbility("majestic_advice") || HasAbility("alchemic_assistance"))
		{
			return 0.60f;
		}

		if (HasAbility("royal_order"))
		{
			return 0.75f;
		}

		return 1.0f;
	}

	public string GetBuildName()
	{
		return AbilityDefinitions.GetBranchName(ChosenBranch);
	}

	public string[] GetActiveLoadout()
	{
		return ChosenBranch switch
		{
			BuildBranch.Warrior => new[] { "Slash", "Heavy Cut", "Devastating Cut" },
			BuildBranch.Ranger => new[] { "Multi-Arrow", "Quick Fire", "Critical Fire" },
			BuildBranch.Scout => new[] { "Dash Hit", "Dart Wave", "Enemy Crash" },
			_ => new[] { "Unlock a branch in the tree" },
		};
	}

	private void TickTimers(float delta)
	{
		attackTimer -= delta;
		shieldRechargeTimer = Mathf.Max(0.0f, shieldRechargeTimer - delta);
		quickFireTimer = Mathf.Max(0.0f, quickFireTimer - delta);
		criticalFireTimer = Mathf.Max(0.0f, criticalFireTimer - delta);
		enemyCrashTimer = Mathf.Max(0.0f, enemyCrashTimer - delta);
		invulnerabilityTimer = Mathf.Max(0.0f, invulnerabilityTimer - delta);
		dashTimer = Mathf.Max(0.0f, dashTimer - delta);

		List<string> keys = new(cooldowns.Keys);
		foreach (string key in keys)
		{
			cooldowns[key] -= delta;
		}
	}

	private void UpdateMovement(double delta)
	{
		Vector2 direction = Input.GetVector("left", "right", "up", "down");
		if (dashTimer > 0.0f)
		{
			Velocity = dashDirection * (DashDistance / DashDuration);
		}
		else
		{
			Velocity = direction * GetMoveSpeed();
		}

		MoveAndSlide();
		if (dashTimer > 0.0f)
		{
			ApplyDashDamage();
		}

		if (sprite == null)
		{
			return;
		}

		if (Velocity.LengthSquared() < 1.0f)
		{
			sprite.Frame = 0;
			walkTimer = 0.0f;
			return;
		}

		walkTimer += delta * (1.8 + Velocity.Length() / 120.0f);
		if (walkTimer >= 1.0f)
		{
			walkTimer = 0.0f;
			sprite.Frame = (sprite.Frame + 1) % Mathf.Max(1, sprite.Hframes);
		}

		sprite.FlipH = GetGlobalMousePosition().X < GlobalPosition.X;
	}

	private void UpdateAiming()
	{
		weapon?.AimAt(GetGlobalMousePosition(), (int)ChosenBranch);
	}

	private void HandleAttack()
	{
		if (dashTimer > 0.0f || !Input.IsActionPressed("attack") || attackTimer > 0.0f)
		{
			return;
		}

		float basicCooldown = ChosenBranch switch
		{
			BuildBranch.Ranger => 0.45f,
			BuildBranch.Scout => 0.38f,
			_ => 0.58f,
		};

		float attackSpeedMultiplier = 1.0f;
		if (quickFireTimer > 0.0f || criticalFireTimer > 0.0f)
		{
			attackSpeedMultiplier *= 2.0f;
		}
		if (enemyCrashTimer > 0.0f)
		{
			attackSpeedMultiplier *= 1.25f;
		}

		attackTimer = basicCooldown / attackSpeedMultiplier;
		Vector2 direction = (GetGlobalMousePosition() - GlobalPosition).Normalized();
		weapon?.Swing();

		switch (ChosenBranch)
		{
			case BuildBranch.Ranger:
				SpawnProjectile(direction, GetRangedDamage(true), 300.0f, 2.0f, HasAbility("majestic_advice"), false, new Color(0.86f, 0.77f, 0.35f));
				break;
			case BuildBranch.Scout:
				bool closeHit = TryMeleeArc(direction, 34.0f, 0.72f, GetMeleeDamage(true) * 0.65f);
				SpawnProjectile(direction, 0.0f, 280.0f, 1.2f, false, false, new Color(0.48f, 0.88f, 0.7f), true);
				if (closeHit)
				{
					weapon?.FlashHit();
				}
				break;
			default:
				if (TryMeleeArc(direction, 42.0f, 1.1f, GetMeleeDamage(true)))
				{
					weapon?.FlashHit();
				}
				break;
		}
	}

	private void HandleActiveAbilities()
	{
		if (dashTimer > 0.0f)
		{
			return;
		}

		if (Input.IsActionJustPressed("ability_1"))
		{
			UseBranchAbility(0);
		}
		if (Input.IsActionJustPressed("ability_2"))
		{
			UseBranchAbility(1);
		}
		if (Input.IsActionJustPressed("ability_3"))
		{
			UseBranchAbility(2);
		}
	}

	private void UseBranchAbility(int slot)
	{
		if (ChosenBranch == BuildBranch.None)
		{
			LastTreeMessage = "Unlock a branch in the tree first.";
			return;
		}

		string abilityId = GetBranchAbilityId(slot);
		if (!HasAbility(abilityId))
		{
			LastTreeMessage = "This skill is still locked.";
			return;
		}

		if (cooldowns.TryGetValue(abilityId, out float remaining) && remaining > 0.0f)
		{
			LastTreeMessage = AbilityDefinitions.All[abilityId].DisplayName + " cooldown: " + remaining.ToString("0.0") + "s";
			return;
		}

		Vector2 direction = (GetGlobalMousePosition() - GlobalPosition).Normalized();
		if (direction == Vector2.Zero)
		{
			direction = Vector2.Right;
		}

		bool used = ChosenBranch switch
		{
			BuildBranch.Warrior => UseWarriorAbility(slot, direction),
			BuildBranch.Ranger => UseRangerAbility(slot, direction),
			BuildBranch.Scout => UseScoutAbility(slot, direction),
			_ => false,
		};

		if (used)
		{
			weapon?.Swing(true);
			EmitSignal(SignalName.StatsChanged);
		}
	}

	private bool UseWarriorAbility(int slot, Vector2 direction)
	{
		return slot switch
		{
			0 => ConsumeCooldown("slash", 4.0f) && UseSlash(direction),
			1 => ConsumeCooldown("heavy_cut", 5.5f) && UseHeavyCut(direction),
			2 => ConsumeCooldown("devastating_cut", 8.5f) && UseDevastatingCut(direction),
			_ => false,
		};
	}

	private bool UseRangerAbility(int slot, Vector2 direction)
	{
		return slot switch
		{
			0 => ConsumeCooldown("multi_arrow", 4.5f) && UseMultiArrow(direction),
			1 => ConsumeCooldown("quick_fire", 8.0f) && UseQuickFire(),
			2 => ConsumeCooldown("critical_fire", 10.0f) && UseCriticalFire(),
			_ => false,
		};
	}

	private bool UseScoutAbility(int slot, Vector2 direction)
	{
		return slot switch
		{
			0 => ConsumeCooldown("dash_hit", 6.0f) && UseDashHit(direction, false),
			1 => ConsumeCooldown("dart_wave", 5.0f) && UseDartWave(direction),
			2 => ConsumeCooldown("enemy_crash", 9.0f) && UseDashHit(direction, true),
			_ => false,
		};
	}

	private bool UseSlash(Vector2 direction)
	{
		LastTreeMessage = "Slash carved through the mob pack.";
		return TryMeleeArc(direction, 60.0f, 1.7f, GetMeleeDamage(false) * 1.3f, 8.0f);
	}

	private bool UseHeavyCut(Vector2 direction)
	{
		LastTreeMessage = "Heavy Cut lands a crushing single hit.";
		return TryMeleeArc(direction, 54.0f, 0.45f, GetMeleeDamage(false) * 2.4f);
	}

	private bool UseDevastatingCut(Vector2 direction)
	{
		GlobalPosition += direction * 56.0f;
		LastTreeMessage = "Devastating Cut crashes onto a priority target.";
		return TryMeleeArc(direction, 68.0f, 1.2f, GetMeleeDamage(false) * 3.8f);
	}

	private bool UseMultiArrow(Vector2 direction)
	{
		for (int index = -2; index <= 2; index++)
		{
			SpawnProjectile(direction.Rotated(index * 0.12f), GetRangedDamage(false) * 0.9f, 295.0f, 1.7f, HasAbility("majestic_advice"), false, new Color(0.95f, 0.8f, 0.25f));
		}

		LastTreeMessage = "Multi-Arrow fills the lane with arrows.";
		return true;
	}

	private bool UseQuickFire()
	{
		quickFireTimer = 4.0f;
		LastTreeMessage = "Quick Fire doubles your rate of fire.";
		return true;
	}

	private bool UseCriticalFire()
	{
		criticalFireTimer = 4.0f;
		quickFireTimer = Mathf.Max(quickFireTimer, 4.0f);
		LastTreeMessage = "Critical Fire turns you into a ranged burst machine.";
		return true;
	}

	private bool UseDartWave(Vector2 direction)
	{
		for (int burst = 0; burst < 3; burst++)
		{
			for (int index = -1; index <= 1; index++)
			{
				Projectile projectile = SpawnProjectile(direction.Rotated(index * 0.22f), 0.0f, 300.0f - burst * 20.0f, 1.5f + burst * 0.15f, false, false, new Color(0.38f, 0.9f, 0.68f), true);
				projectile.GlobalPosition += direction * burst * 5.0f;
			}
		}

		LastTreeMessage = "Dart Wave clears swarms while you keep moving.";
		return true;
	}

	private bool UseDashHit(Vector2 direction, bool enemyCrash)
	{
		StartDash(direction, GetMeleeDamage(false) * (enemyCrash ? 2.8f : 2.3f));
		if (enemyCrash)
		{
			enemyCrashTimer = 5.0f;
			LastTreeMessage = "Enemy Crash grants movement and attack speed after the dash.";
		}
		else
		{
			LastTreeMessage = "Dash Hit bursts through the line.";
		}

		return true;
	}

	private void StartDash(Vector2 direction, float damage)
	{
		dashDirection = direction == Vector2.Zero ? Vector2.Right : direction.Normalized();
		dashDamage = damage;
		dashTimer = DashDuration;
		invulnerabilityTimer = Mathf.Max(invulnerabilityTimer, DashDuration + 0.05f);
		dashHitEnemies.Clear();
		ApplyDashDamage();
	}

	private void ApplyDashDamage()
	{
		if (dashTimer <= 0.0f)
		{
			return;
		}

		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is not Enemy enemy || !IsInstanceValid(enemy) || dashHitEnemies.Contains(enemy))
			{
				continue;
			}

			Vector2 offset = enemy.GlobalPosition - GlobalPosition;
			if (offset.Length() > DashHitRadius)
			{
				continue;
			}

			dashHitEnemies.Add(enemy);
			enemy.TakeDamage(ApplyCritical(dashDamage, true), HasAbility("alchemic_assistance") && ChosenBranch == BuildBranch.Scout);
			enemy.GlobalPosition += dashDirection * 10.0f;
			weapon?.FlashHit();
		}
	}

	private bool ConsumeCooldown(string abilityId, float seconds)
	{
		cooldowns[abilityId] = seconds * GetActiveCooldownMultiplier();
		return true;
	}

	private bool TryMeleeArc(Vector2 direction, float radius, float arcWidth, float damage, float knockback = 0.0f)
	{
		bool hitSomething = false;
		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is not Enemy enemy || !IsInstanceValid(enemy))
			{
				continue;
			}

			Vector2 offset = enemy.GlobalPosition - GlobalPosition;
			if (offset.Length() > radius)
			{
				continue;
			}

			if (Mathf.Abs(direction.AngleTo(offset.Normalized())) > arcWidth)
			{
				continue;
			}

			float finalDamage = ApplyCritical(damage, ChosenBranch == BuildBranch.Ranger ? false : true);
			enemy.TakeDamage(finalDamage, HasAbility("alchemic_assistance") && ChosenBranch == BuildBranch.Scout);
			if (knockback > 0.0f)
			{
				enemy.GlobalPosition += offset.Normalized() * knockback;
			}
			hitSomething = true;
		}

		return hitSomething;
	}

	private Projectile SpawnProjectile(Vector2 direction, float damage, float speed, float lifetime, bool pierce, bool poison, Color color, bool scoutDebuff = false)
	{
		Projectile projectile = ProjectileScene.Instantiate<Projectile>();
		projectile.FromPlayer = true;
		projectile.Direction = direction;
		projectile.Damage = ApplyCritical(damage, false);
		projectile.Speed = speed;
		projectile.Lifetime = lifetime;
		projectile.CanPierce = pierce;
		projectile.AppliesPoison = poison;
		projectile.AppliesScoutDebuff = scoutDebuff;
		projectile.ScoutSlowMultiplier = 0.6f;
		projectile.ScoutPoisonTickDamage = HasAbility("alchemic_assistance") ? 2.0f : 1.0f;
		projectile.ScoutPoisonTickInterval = 4.0f;
		projectile.ScoutDebuffDuration = HasAbility("alchemic_assistance") ? 10.0f : 8.0f;
		projectile.Tint = color;
		projectile.GlobalPosition = projectileOrigin != null ? projectileOrigin.GlobalPosition : GlobalPosition + direction * 18.0f;
		projectileContainer?.AddChild(projectile);
		return projectile;
	}

	private float GetMoveSpeed()
	{
		float value = RangerBaseMoveSpeed;
		if (ChosenBranch == BuildBranch.Warrior)
		{
			value -= WarriorMovePenalty;
		}
		if (ChosenBranch == BuildBranch.Scout)
		{
			value += ScoutMoveBonus;
		}
		if (enemyCrashTimer > 0.0f)
		{
			value *= 1.25f;
		}
		if (HasAbility("payment_time") && currentHealth <= maxHealth * 0.2f)
		{
			value *= 1.10f;
		}

		return value;
	}

	private float GetMeleeDamage(bool basicAttack)
	{
		float value = BaseMeleeDamage;
		if (ChosenBranch == BuildBranch.Warrior)
		{
			value *= 1.18f;
		}
		if (ChosenBranch == BuildBranch.Scout)
		{
			value *= 1.08f;
		}
		if (HasAbility("strikes_of_judgement"))
		{
			value *= 1.25f;
		}
		if (HasAbility("payment_time") && currentHealth <= maxHealth * 0.2f)
		{
			value *= 1.10f;
		}
		if (basicAttack && ChosenBranch == BuildBranch.Scout)
		{
			value *= 0.75f;
		}
		return value;
	}

	private float GetRangedDamage(bool arrow)
	{
		float value = BaseRangedDamage;
		if (ChosenBranch == BuildBranch.Ranger)
		{
			value *= 1.18f;
		}
		if (HasAbility("kings_courtesy"))
		{
			value *= 1.15f;
		}
		if (arrow && HasAbility("arrows_of_vengeance"))
		{
			value *= 1.25f;
		}
		if (HasAbility("payment_time") && currentHealth <= maxHealth * 0.2f)
		{
			value *= 1.10f;
		}
		return value;
	}

	private float ApplyCritical(float baseDamage, bool melee)
	{
		float critChance = BaseCritChance;
		float critMultiplier = BaseCritMultiplier + (Level >= 3 ? 0.05f : 0.0f);

		if (currentHealth <= maxHealth * 0.2f)
		{
			if (HasAbility("strikes_of_judgement") && melee)
			{
				critChance += 0.75f;
			}
			else if (HasAbility("arrows_of_vengeance") && !melee)
			{
				critChance += 0.75f;
			}
			else if (HasAbility("payment_time"))
			{
				critChance += 0.75f;
			}
			else if (HasAbility("strikes_of_justice"))
			{
				critChance += 0.50f;
			}
		}

		if (criticalFireTimer > 0.0f)
		{
			critMultiplier += 0.40f;
		}

		return GD.Randf() <= critChance ? baseDamage * critMultiplier : baseDamage;
	}

	private void RefreshStats()
	{
		float previousMaxHealth = maxHealth;
		float previousMaxShield = maxShield;
		float healthRatio = previousMaxHealth > 0.0f ? currentHealth / previousMaxHealth : 1.0f;
		float shieldRatio = previousMaxShield > 0.0f ? currentShield / previousMaxShield : 1.0f;
		float healthBonus = 0.0f;
		if (Level >= 2)
		{
			healthBonus += 5.0f;
		}
		if (Level >= 3)
		{
			healthBonus += 10.0f;
		}

		maxHealth = RangerBaseHealth + healthBonus;
		if (ChosenBranch == BuildBranch.Warrior)
		{
			maxHealth += WarriorHealthBonus;
		}
		else if (ChosenBranch == BuildBranch.Scout)
		{
			maxHealth -= ScoutHealthPenalty;
		}

		maxShield = 0.0f;

		if (HasAbility("kings_grace"))
		{
			maxShield = 5.0f;
		}
		if (HasAbility("kings_blessing"))
		{
			maxShield = 15.0f;
		}
		if (HasAbility("noble_directive"))
		{
			maxShield = 15.0f;
		}

		currentHealth = Mathf.Clamp(maxHealth * healthRatio, 0.0f, maxHealth);
		currentShield = Mathf.Clamp(maxShield * shieldRatio, 0.0f, maxShield);
		if (maxShield > 0.0f && currentShield == 0.0f && shieldRechargeTimer <= 0.0f)
		{
			currentShield = maxShield;
		}
	}

	private void RechargeShield(float delta)
	{
		if (shieldRechargeTimer > 0.0f || maxShield <= 0.0f)
		{
			return;
		}

		currentShield = Mathf.Min(maxShield, currentShield + ShieldRechargeRate * delta);
	}

	private bool HasAnyAbility(params string[] ids)
	{
		foreach (string id in ids)
		{
			if (unlockedAbilities.Contains(id))
			{
				return true;
			}
		}

		return false;
	}

	private string GetBranchAbilityId(int slot)
	{
		return ChosenBranch switch
		{
			BuildBranch.Warrior => slot switch
			{
				0 => "slash",
				1 => "heavy_cut",
				_ => "devastating_cut",
			},
			BuildBranch.Ranger => slot switch
			{
				0 => "multi_arrow",
				1 => "quick_fire",
				_ => "critical_fire",
			},
			BuildBranch.Scout => slot switch
			{
				0 => "dash_hit",
				1 => "dart_wave",
				_ => "enemy_crash",
			},
			_ => string.Empty,
		};
	}
}
