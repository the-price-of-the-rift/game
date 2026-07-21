#nullable enable

using Godot;

// Persistent root: owns the Player and all UI, and swaps village.tscn <-> rift.tscn
// inside WorldSlot. The Player node is never freed, so progression state needs no
// serialization across the round trip. Owns UI input (B / InputLocked), scene swaps,
// and the persistent currentTier + houseSwapDone flags.
public partial class MainController : Node2D
{
	[Export] public PackedScene VillageScene { get; set; } = null!;
	[Export] public PackedScene RiftScene { get; set; } = null!;
	[Export] public NodePath PlayerPath { get; set; } = new NodePath();
	[Export] public NodePath HudPath { get; set; } = new NodePath();
	[Export] public NodePath AbilityTreePath { get; set; } = new NodePath();
	[Export] public NodePath DialogueUiPath { get; set; } = new NodePath();
	[Export] public NodePath CheatPanelPath { get; set; } = new NodePath();
	[Export] public NodePath WorldPromptLabelPath { get; set; } = new NodePath();
	[Export] public NodePath WorldSlotPath { get; set; } = new NodePath();

	private Player? player;
	private HudController? hud;
	private AbilityTreeUI? treeUi;
	private DialogueUI? dialogueUi;
	private CheatPanelUI? cheatPanel;
	private Label? worldPrompt;
	private Node2D? worldSlot;

	private int currentTier = 1;
	private bool houseSwapDone = false;
	private bool inRift = false;

	public int CurrentTier => currentTier;
	public bool InRift => inRift;
	public bool HouseSwapDone => houseSwapDone;
	public bool UiBlocking => (treeUi?.Visible ?? false) || (dialogueUi?.Visible ?? false) || (cheatPanel?.Visible ?? false);

	public override void _Ready()
	{
		player = GetNodeOrNull<Player>(PlayerPath);
		hud = GetNodeOrNull<HudController>(HudPath);
		treeUi = GetNodeOrNull<AbilityTreeUI>(AbilityTreePath);
		dialogueUi = GetNodeOrNull<DialogueUI>(DialogueUiPath);
		cheatPanel = GetNodeOrNull<CheatPanelUI>(CheatPanelPath);
		worldPrompt = GetNodeOrNull<Label>(WorldPromptLabelPath);
		worldSlot = GetNodeOrNull<Node2D>(WorldSlotPath);

		if (player == null)
		{
			return;
		}

		player.PlayerDied += OnPlayerDied;
		hud?.Bind(player, this);
		treeUi?.Bind(player);
		dialogueUi?.Bind(player);
		cheatPanel?.Bind(player, this);

		SwapToVillage();
	}

	public override void _Process(double delta)
	{
		if (player == null)
		{
			return;
		}

		if (Input.IsActionJustPressed("toggle_abilities"))
		{
			treeUi?.Toggle();
		}

		if (Input.IsActionJustPressed("toggle_cheats"))
		{
			cheatPanel?.Toggle();
		}

		player.InputLocked = UiBlocking;

		if (UiBlocking && worldPrompt != null)
		{
			worldPrompt.Text = (treeUi?.Visible ?? false)
				? "Ability Tree open - click a node to unlock, press B to close."
				: (cheatPanel?.Visible ?? false)
					? "Cheat Panel open - press ` to close."
					: "Press [F] to leave the conversation.";
		}
	}

	public void SetPrompt(string text)
	{
		if (worldPrompt != null)
		{
			worldPrompt.Text = text;
		}
	}

	public void EnterRift()
	{
		if (inRift)
		{
			return;
		}

		// First rift entry is the scripted collapse beat: mark it so the village
		// reloads with broken houses from here on (applied idempotently on load).
		if (!houseSwapDone)
		{
			houseSwapDone = true;
			player?.SetLastTreeMessage("A rift tears open in the village. Nearby houses collapse - including hers.");
		}

		CallDeferred(nameof(SwapToRift));
	}

	// Cheat-only: grants the exact rewards RiftController.OnEnemyKilled would have paid
	// out for every enemy in the current tier's wave, then advances the tier and returns
	// to the village - same net effect as actually clearing the rift.
	public void CheatCompleteRift()
	{
		if (player == null)
		{
			return;
		}

		int tier = currentTier;
		int enemyCount = 3 + tier;
		for (int index = 0; index < enemyCount; index++)
		{
			bool elite = tier >= 3 && index == enemyCount - 1;
			player.GainXp(8 + tier * 2);
			player.GainMagicStones(1);
			player.GainReputation(2 + (elite ? 3 : 0));
		}

		player.SetLastTreeMessage("Cheat: rift tier " + tier + " auto-completed.");
		ReturnToVillage(true);
	}

	public void ReturnToVillage(bool cleared)
	{
		if (cleared)
		{
			currentTier += 1;
		}

		CallDeferred(nameof(SwapToVillage));
	}

	private void SwapToRift()
	{
		if (RiftScene == null)
		{
			return;
		}

		inRift = true;
		Node2D root = LoadScene(RiftScene);

		if (player != null)
		{
			Node2D? entry = root.GetNodeOrNull<Node2D>("EntryMarker");
			Node? entities = root.GetNodeOrNull<Node>("Entities");
			if (entry != null)
			{
				player.GlobalPosition = entry.GlobalPosition;
			}
			player.SetProjectileContainer(entities ?? root);
		}

		if (root is RiftController rift && player != null)
		{
			rift.Init(player, this, currentTier);
		}
	}

	private void SwapToVillage()
	{
		if (VillageScene == null)
		{
			return;
		}

		inRift = false;
		Node2D root = LoadScene(VillageScene);

		if (player != null)
		{
			Node2D? spawn = root.GetNodeOrNull<Node2D>("PlayerSpawn");
			Node? entities = root.GetNodeOrNull<Node>("Entities");
			if (spawn != null)
			{
				player.GlobalPosition = spawn.GlobalPosition;
			}
			player.SetProjectileContainer(entities ?? root);
			player.HealFull();
		}

		if (root is VillageController village && player != null && dialogueUi != null)
		{
			village.Init(player, this, dialogueUi);
		}
	}

	// Free the current world immediately (we are at idle time via CallDeferred, never
	// mid-signal) so the two scenes never coexist, then instance and mount the new one.
	private Node2D LoadScene(PackedScene scene)
	{
		if (worldSlot != null)
		{
			foreach (Node child in worldSlot.GetChildren())
			{
				worldSlot.RemoveChild(child);
				child.Free();
			}
		}

		Node2D root = scene.Instantiate<Node2D>();
		worldSlot?.AddChild(root);
		return root;
	}

	private void OnPlayerDied()
	{
		if (player == null)
		{
			return;
		}

		player.SetLastTreeMessage("You fell in the rift and woke at the village edge. Progression is kept so you can test builds fast.");
		ReturnToVillage(false);
	}
}
