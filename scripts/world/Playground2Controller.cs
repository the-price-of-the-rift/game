#nullable enable

using Godot;
using System.Collections.Generic;

public partial class Playground2Controller : Node2D
{
	[Export] public PackedScene EnemyScene { get; set; } = null!;
	[Export] public PackedScene ProjectileScene { get; set; } = null!;
	[Export] public PackedScene ToxicAreaScene { get; set; } = null!;
	[Export] public NodePath PlayerPath { get; set; } = new NodePath();
	[Export] public NodePath HudPath { get; set; } = new NodePath();
	[Export] public NodePath AbilityTreePath { get; set; } = new NodePath();
	[Export] public NodePath EntitiesRootPath { get; set; } = new NodePath();
	[Export] public NodePath WorldPromptLabelPath { get; set; } = new NodePath();
	[Export] public NodePath BoardMarkerPath { get; set; } = new NodePath();
	[Export] public NodePath PortalMarkerPath { get; set; } = new NodePath();
	[Export] public NodePath RespawnPointPath { get; set; } = new NodePath();
	[Export] public NodePath SpawnPointsRootPath { get; set; } = new NodePath();
	[Export] public NodePath HazardOriginsRootPath { get; set; } = new NodePath();
	[Export] public NodePath PoisonVisualPath { get; set; } = new NodePath();
	[Export] public float BoardInteractionRadius { get; set; } = 60.0f;
	[Export] public float PortalInteractionRadius { get; set; } = 60.0f;
	[Export] public float PoisonRadius { get; set; } = 58.0f;
	[Export] public float BaseProjectileEnemyRatio { get; set; } = 0.2f;
	[Export] public float BaseAcidEnemyRatio { get; set; } = 0.25f;
	[Export] public float ProjectileRatioPerTier { get; set; } = 0.15f;
	[Export] public float AcidRatioPerTier { get; set; } = 0.18f;

	private readonly List<EnemyPrototype> livingEnemies = new();
	private readonly HashSet<EnemyPrototype> projectileEnemies = new();
	private readonly HashSet<EnemyPrototype> acidEnemies = new();

	private PlayerPrototype? player;
	private HudController? hud;
	private AbilityTreeUI? treeUi;
	private Node2D? entitiesRoot;
	private Label? worldPrompt;
	private Node2D? boardMarker;
	private Node2D? portalMarker;
	private Node2D? respawnPoint;
	private Node? spawnPointsRoot;
	private Node? hazardOriginsRoot;
	private CanvasItem? poisonVisual;
	private int currentTier = 1;
	private float projectileVolleyTimer = 2.8f;
	private float poisonTickTimer = 0.4f;
	private float toxicAreaDamageScale = 1.0f;
	private bool riftActive = false;
	private bool intermissionChoiceUsed = false;

	public int CurrentTier => currentTier;
	public bool RiftActive => riftActive;

	public override void _Ready()
	{
		player = GetNodeOrNull<PlayerPrototype>(PlayerPath);
		hud = GetNodeOrNull<HudController>(HudPath);
		treeUi = GetNodeOrNull<AbilityTreeUI>(AbilityTreePath);
		entitiesRoot = GetNodeOrNull<Node2D>(EntitiesRootPath);
		worldPrompt = GetNodeOrNull<Label>(WorldPromptLabelPath);
		boardMarker = GetNodeOrNull<Node2D>(BoardMarkerPath);
		portalMarker = GetNodeOrNull<Node2D>(PortalMarkerPath);
		respawnPoint = GetNodeOrNull<Node2D>(RespawnPointPath);
		spawnPointsRoot = GetNodeOrNull<Node>(SpawnPointsRootPath);
		hazardOriginsRoot = GetNodeOrNull<Node>(HazardOriginsRootPath);
		poisonVisual = GetNodeOrNull<CanvasItem>(PoisonVisualPath);

		if (player == null)
		{
			return;
		}

		player.ProjectileScene = ProjectileScene;
		player.PlayerDied += OnPlayerDied;
		player.SetProjectileContainer(entitiesRoot ?? this);
		hud?.Bind(player, this);
		treeUi?.Bind(player);

		if (poisonVisual != null)
		{
			poisonVisual.Visible = false;
		}

		UpdatePrompt();
	}

	public override void _Process(double delta)
	{
		if (player == null)
		{
			return;
		}

		if (Input.IsActionJustPressed("toggle_abilities"))
		{
			ToggleAbilityTree();
		}

		if (treeUi != null && treeUi.Visible)
		{
			player.InputLocked = true;
			UpdatePrompt();
			return;
		}

		player.InputLocked = false;
		HandleWorldChoices();
		HandleScalingHazards((float)delta);
		HandleToxicAreas();
		UpdatePrompt();
	}

	private void HandleWorldChoices()
	{
		if (player == null)
		{
			return;
		}

		if (!riftActive && boardMarker != null && Input.IsActionJustPressed("help_village") && player.GlobalPosition.DistanceTo(boardMarker.GlobalPosition) < BoardInteractionRadius && !intermissionChoiceUsed)
		{
			intermissionChoiceUsed = true;
			currentTier += 1;
			player.GainReputation(15);
			player.HealFull();
			player.SetLastTreeMessage("You helped the village, but the rift grew stronger while you were away.");
		}

		if (!riftActive && portalMarker != null && Input.IsActionJustPressed("next_rift") && player.GlobalPosition.DistanceTo(portalMarker.GlobalPosition) < PortalInteractionRadius)
		{
			StartNextRift();
		}
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

		if (!riftActive || currentTier < 2 || livingEnemies.Count == 0)
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

	private void StartNextRift()
	{
		if (riftActive || player == null || entitiesRoot == null)
		{
			return;
		}

		Godot.Collections.Array<Vector2> spawnPoints = GetChildPositions(spawnPointsRoot);
		if (spawnPoints.Count == 0)
		{
			player.SetLastTreeMessage("No spawn points are assigned in playground2.");
			return;
		}

		ClearEntities();
		riftActive = true;
		livingEnemies.Clear();
		projectileEnemies.Clear();
		acidEnemies.Clear();

		int enemyCount = 3 + currentTier;
		for (int index = 0; index < enemyCount; index++)
		{
			EnemyPrototype enemy = EnemyScene.Instantiate<EnemyPrototype>();
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

	private void OnEnemyKilled(EnemyPrototype enemy)
	{
		if (player == null)
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

		if (livingEnemies.Count == 0)
		{
			riftActive = false;
			intermissionChoiceUsed = false;
			player.SetLastTreeMessage("Rift tier cleared. Use B to unlock more power, H to help the village, or N for the next tier.");
			currentTier += 1;
		}
	}

	private void FireHazardVolley()
	{
		if (player == null || entitiesRoot == null)
		{
			return;
		}

		foreach (EnemyPrototype enemy in livingEnemies)
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

	private void ToggleAbilityTree()
	{
		if (treeUi == null || player == null)
		{
			return;
		}

		treeUi.Toggle();
		player.InputLocked = treeUi.Visible;
	}

	private void OnPlayerDied()
	{
		if (player == null)
		{
			return;
		}

		ClearEntities();
		livingEnemies.Clear();
		projectileEnemies.Clear();
		acidEnemies.Clear();
		riftActive = false;
		player.GlobalPosition = respawnPoint?.GlobalPosition ?? Vector2.Zero;
		player.HealFull();
		player.SetLastTreeMessage("You were reset to the village edge. The prototype keeps your progression so you can test builds fast.");
	}

	private void ClearEntities()
	{
		if (entitiesRoot == null)
		{
			return;
		}

		foreach (Node child in entitiesRoot.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void UpdatePrompt()
	{
		if (player == null || worldPrompt == null)
		{
			return;
		}

		if (treeUi != null && treeUi.Visible)
		{
			worldPrompt.Text = "Ability Tree open - click a node to unlock, press B to close.";
			return;
		}

		if (!riftActive && boardMarker != null && player.GlobalPosition.DistanceTo(boardMarker.GlobalPosition) < BoardInteractionRadius && !intermissionChoiceUsed)
		{
			worldPrompt.Text = "Meaningful choice: [H] help the village for reputation, but skip ahead to a stronger rift tier.";
			return;
		}

		if (!riftActive && portalMarker != null && player.GlobalPosition.DistanceTo(portalMarker.GlobalPosition) < PortalInteractionRadius)
		{
			worldPrompt.Text = "Meaningful choice: [N] enter the rift for XP and magic stones.";
			return;
		}

		if (currentTier >= 2)
		{
			worldPrompt.Text = "Tier " + currentTier + ": poison cloud + projectile pressure + mob pack scaling are active.";
			return;
		}

		worldPrompt.Text = "Move with WASD, attack with LMB, use Q/E/R actives, press B for the tree.";
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
		List<EnemyPrototype> shuffledEnemies = new(livingEnemies);

		for (int index = shuffledEnemies.Count - 1; index > 0; index--)
		{
			int swapIndex = (int)GD.RandRange(0, index);
			(shuffledEnemies[index], shuffledEnemies[swapIndex]) = (shuffledEnemies[swapIndex], shuffledEnemies[index]);
		}

		foreach (EnemyPrototype enemy in livingEnemies)
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
