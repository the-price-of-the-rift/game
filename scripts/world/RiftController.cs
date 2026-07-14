#nullable enable

using Godot;
using System.Collections.Generic;

// The rift is a separate combat world, mounted in Main's WorldSlot. It owns spawns,
// scaling hazards, poison, toxic areas, and the living-enemy tally. When the last enemy
// dies it hands control back to Main (ReturnToVillage(cleared: true), which bumps the tier).
// The Player is passed in by Main and never freed; this controller is discarded on exit.
public partial class RiftController : Node2D
{
	[Export] public PackedScene EnemyScene { get; set; } = null!;
	[Export] public PackedScene ProjectileScene { get; set; } = null!;
	[Export] public PackedScene ToxicAreaScene { get; set; } = null!;
	[Export] public NodePath EntitiesRootPath { get; set; } = new NodePath();
	[Export] public NodePath SpawnPointsRootPath { get; set; } = new NodePath();
	[Export] public NodePath PoisonVisualPath { get; set; } = new NodePath();
	[Export] public float PoisonRadius { get; set; } = 58.0f;
	[Export] public float BaseProjectileEnemyRatio { get; set; } = 0.2f;
	[Export] public float BaseAcidEnemyRatio { get; set; } = 0.25f;
	[Export] public float ProjectileRatioPerTier { get; set; } = 0.15f;
	[Export] public float AcidRatioPerTier { get; set; } = 0.18f;

	private readonly List<Enemy> livingEnemies = new();
	private readonly HashSet<Enemy> projectileEnemies = new();
	private readonly HashSet<Enemy> acidEnemies = new();

	private Player? player;
	private MainController? main;
	private Node2D? entitiesRoot;
	private Node? spawnPointsRoot;
	private CanvasItem? poisonVisual;

	private int currentTier = 1;
	private float projectileVolleyTimer = 2.8f;
	private float poisonTickTimer = 0.4f;
	private float toxicAreaDamageScale = 1.0f;
	private bool cleared = false;

	public override void _Ready()
	{
		entitiesRoot = GetNodeOrNull<Node2D>(EntitiesRootPath);
		spawnPointsRoot = GetNodeOrNull<Node>(SpawnPointsRootPath);
		poisonVisual = GetNodeOrNull<CanvasItem>(PoisonVisualPath);

		if (poisonVisual != null)
		{
			poisonVisual.Visible = false;
		}
	}

	// Called by Main right after AddChild; the player container is already re-pointed
	// at this scene's Entities node. Spawn the wave for this tier immediately.
	public void Init(Player boundPlayer, MainController mainController, int tier)
	{
		player = boundPlayer;
		main = mainController;
		currentTier = tier;

		if (player == null || entitiesRoot == null)
		{
			return;
		}

		player.ProjectileScene = ProjectileScene;

		Godot.Collections.Array<Vector2> spawnPoints = GetChildPositions(spawnPointsRoot);
		if (spawnPoints.Count == 0)
		{
			player.SetLastTreeMessage("No spawn points are assigned in the rift scene.");
			return;
		}

		int enemyCount = 3 + currentTier;
		for (int index = 0; index < enemyCount; index++)
		{
			Enemy enemy = EnemyScene.Instantiate<Enemy>();
			Vector2 spawnPoint = spawnPoints[index % spawnPoints.Count];
			enemy.GlobalPosition = spawnPoint + new Vector2((index % 2 == 0 ? 1 : -1) * 12.0f * index, 0.0f);
			bool elite = currentTier >= 3 && index == enemyCount - 1;
			enemy.Configure(player, currentTier, elite);
			enemy.EnemyKilled += OnEnemyKilled;
			entitiesRoot.AddChild(enemy);
			livingEnemies.Add(enemy);
		}

		AssignScaledEnemyRoles();

		player.SetLastTreeMessage(currentTier >= 2
			? "The rift now uses poison and projectile pressure. Different builds solve it differently."
			: "Rift tier " + currentTier + " started.");
	}

	public override void _Process(double delta)
	{
		if (player == null || main == null || cleared || main.UiBlocking)
		{
			return;
		}

		HandleScalingHazards((float)delta);
		HandleToxicAreas();
		UpdatePrompt();
	}

	private void HandleScalingHazards(float delta)
	{
		if (player == null)
		{
			return;
		}

		if (poisonVisual != null)
		{
			poisonVisual.Visible = currentTier >= 2;
		}

		if (currentTier >= 2 && poisonVisual is Node2D poisonNode && player.GlobalPosition.DistanceTo(poisonNode.GlobalPosition) <= PoisonRadius)
		{
			poisonTickTimer -= delta;
			if (poisonTickTimer <= 0.0f)
			{
				poisonTickTimer = 0.5f;
				player.TakeDamage(3.0f + currentTier);
			}
		}
		else
		{
			poisonTickTimer = 0.3f;
		}

		if (currentTier < 2 || livingEnemies.Count == 0)
		{
			projectileVolleyTimer = 2.0f;
			toxicAreaDamageScale = 1.0f;
			return;
		}

		toxicAreaDamageScale = 1.0f + (currentTier - 2) * 0.2f;

		projectileVolleyTimer -= delta;
		if (projectileVolleyTimer <= 0.0f)
		{
			projectileVolleyTimer = Mathf.Max(0.9f, 2.8f - currentTier * 0.2f);
			FireHazardVolley();
		}
	}

	private void OnEnemyKilled(Enemy enemy)
	{
		if (player == null || main == null)
		{
			return;
		}

		if (acidEnemies.Contains(enemy))
		{
			SpawnDeathToxicArea(enemy.GlobalPosition);
		}

		projectileEnemies.Remove(enemy);
		acidEnemies.Remove(enemy);
		livingEnemies.Remove(enemy);
		player.GainXp(8 + currentTier * 2);
		player.GainMagicStones(1);
		player.GainReputation(2 + (enemy.IsElite ? 3 : 0));

		if (livingEnemies.Count == 0)
		{
			cleared = true;
			player.SetLastTreeMessage("Rift cleared. You return to the village - press B to spend what you earned.");
			main.ReturnToVillage(true);
		}
	}

	private void FireHazardVolley()
	{
		if (player == null || entitiesRoot == null)
		{
			return;
		}

		foreach (Enemy enemy in livingEnemies)
		{
			if (!IsInstanceValid(enemy) || !projectileEnemies.Contains(enemy) || !enemy.CanUseRangedAttack())
			{
				continue;
			}

			Vector2 origin = enemy.GlobalPosition;
			Projectile projectile = ProjectileScene.Instantiate<Projectile>();
			projectile.FromPlayer = false;
			projectile.Speed = 180.0f + currentTier * 8.0f;
			projectile.Damage = 4.0f + currentTier;
			projectile.Lifetime = 3.4f;
			projectile.Tint = new Color(0.87f, 0.32f, 0.36f);
			projectile.Direction = (player.GlobalPosition - origin).Normalized();
			projectile.GlobalPosition = origin;
			entitiesRoot.AddChild(projectile);
			enemy.TriggerRangedAttack(projectileVolleyTimer);
		}
	}

	private void HandleToxicAreas()
	{
		if (player == null)
		{
			return;
		}

		foreach (Node node in GetTree().GetNodesInGroup("toxic_areas"))
		{
			if (node is not ToxicArea toxicArea || !IsInstanceValid(toxicArea))
			{
				continue;
			}

			if (player.GlobalPosition.DistanceTo(toxicArea.GlobalPosition) > toxicArea.Radius)
			{
				continue;
			}

			if (toxicArea.CanTick())
			{
				player.TakeDamage(toxicArea.DamagePerTick * toxicAreaDamageScale);
			}
		}
	}

	private void UpdatePrompt()
	{
		if (main == null)
		{
			return;
		}

		main.SetPrompt(currentTier >= 2
			? "Tier " + currentTier + ": poison cloud + projectile pressure + mob pack scaling are active."
			: "Clear the rift: move with WASD, attack with LMB, use Q/E/R actives.");
	}

	private static Godot.Collections.Array<Vector2> GetChildPositions(Node? root)
	{
		Godot.Collections.Array<Vector2> points = new();
		if (root == null)
		{
			return points;
		}

		foreach (Node child in root.GetChildren())
		{
			if (child is Node2D node2D)
			{
				points.Add(node2D.GlobalPosition);
			}
		}

		return points;
	}

	private void SpawnDeathToxicArea(Vector2 position)
	{
		if (currentTier < 2 || entitiesRoot == null || ToxicAreaScene == null)
		{
			return;
		}

		ToxicArea toxicArea = ToxicAreaScene.Instantiate<ToxicArea>();
		toxicArea.GlobalPosition = position;
		toxicArea.Radius = 22.0f + currentTier * 3.0f;
		toxicArea.Duration = 4.0f + currentTier * 0.6f;
		toxicArea.DamagePerTick = 3.0f + currentTier * 0.5f;
		entitiesRoot.AddChild(toxicArea);
	}

	private int GetScaledEnemyCount(int totalEnemies, float baseRatio, float ratioPerTier)
	{
		if (currentTier < 2 || totalEnemies <= 0)
		{
			return 0;
		}

		float ratio = Mathf.Clamp(baseRatio + (currentTier - 2) * ratioPerTier, 0.0f, 1.0f);
		return Mathf.Clamp(Mathf.CeilToInt(totalEnemies * ratio), 0, totalEnemies);
	}

	private void AssignScaledEnemyRoles()
	{
		projectileEnemies.Clear();
		acidEnemies.Clear();

		int projectileEnemyCount = GetScaledEnemyCount(livingEnemies.Count, BaseProjectileEnemyRatio, ProjectileRatioPerTier);
		int acidEnemyCount = GetScaledEnemyCount(livingEnemies.Count, BaseAcidEnemyRatio, AcidRatioPerTier);
		List<Enemy> shuffledEnemies = new(livingEnemies);

		for (int index = shuffledEnemies.Count - 1; index > 0; index--)
		{
			int swapIndex = (int)GD.RandRange(0, index);
			(shuffledEnemies[index], shuffledEnemies[swapIndex]) = (shuffledEnemies[swapIndex], shuffledEnemies[index]);
		}

		foreach (Enemy enemy in livingEnemies)
		{
			if (enemy.IsElite)
			{
				projectileEnemies.Add(enemy);
				acidEnemies.Add(enemy);
			}
		}

		for (int index = 0; index < shuffledEnemies.Count && projectileEnemies.Count < projectileEnemyCount; index++)
		{
			projectileEnemies.Add(shuffledEnemies[index]);
		}

		for (int index = 0; index < shuffledEnemies.Count && acidEnemies.Count < acidEnemyCount; index++)
		{
			acidEnemies.Add(shuffledEnemies[index]);
		}
	}
}
