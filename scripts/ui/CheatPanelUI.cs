#nullable enable

using Godot;

// Debug/testing panel: press ` to toggle. Every action reuses the same player/world
// APIs real gameplay uses (GainXp, GainMagicStones, ReturnToVillage, ...) so cheat
// results stay consistent with what actually earning them would grant.
public partial class CheatPanelUI : CanvasLayer
{
	[Export] public NodePath StatusLabelPath { get; set; } = new NodePath();
	[Export] public NodePath CompleteRiftButtonPath { get; set; } = new NodePath();
	[Export] public NodePath AddXpButtonPath { get; set; } = new NodePath();
	[Export] public NodePath LevelUpButtonPath { get; set; } = new NodePath();
	[Export] public NodePath AddStonesButtonPath { get; set; } = new NodePath();
	[Export] public NodePath AddReputationButtonPath { get; set; } = new NodePath();
	[Export] public NodePath FullHealButtonPath { get; set; } = new NodePath();
	[Export] public NodePath ImmortalityButtonPath { get; set; } = new NodePath();
	[Export] public NodePath CloseButtonPath { get; set; } = new NodePath();

	private Player? player;
	private MainController? world;
	private Label? statusLabel;
	private Button? immortalityButton;

	public override void _Ready()
	{
		Visible = false;
		statusLabel = GetNodeOrNull<Label>(StatusLabelPath);
		immortalityButton = GetNodeOrNull<Button>(ImmortalityButtonPath);

		Button? completeRiftButton = GetNodeOrNull<Button>(CompleteRiftButtonPath);
		Button? addXpButton = GetNodeOrNull<Button>(AddXpButtonPath);
		Button? levelUpButton = GetNodeOrNull<Button>(LevelUpButtonPath);
		Button? addStonesButton = GetNodeOrNull<Button>(AddStonesButtonPath);
		Button? addReputationButton = GetNodeOrNull<Button>(AddReputationButtonPath);
		Button? fullHealButton = GetNodeOrNull<Button>(FullHealButtonPath);
		Button? closeButton = GetNodeOrNull<Button>(CloseButtonPath);

		if (completeRiftButton != null)
		{
			completeRiftButton.Pressed += OnCompleteRift;
		}
		if (addXpButton != null)
		{
			addXpButton.Pressed += OnAddXp;
		}
		if (levelUpButton != null)
		{
			levelUpButton.Pressed += OnLevelUp;
		}
		if (addStonesButton != null)
		{
			addStonesButton.Pressed += OnAddStones;
		}
		if (addReputationButton != null)
		{
			addReputationButton.Pressed += OnAddReputation;
		}
		if (fullHealButton != null)
		{
			fullHealButton.Pressed += OnFullHeal;
		}
		if (immortalityButton != null)
		{
			immortalityButton.Pressed += OnToggleImmortality;
		}
		if (closeButton != null)
		{
			closeButton.Pressed += Close;
		}
	}

	public void Bind(Player boundPlayer, MainController boundWorld)
	{
		player = boundPlayer;
		world = boundWorld;
		player.StatsChanged += Refresh;
		Refresh();
	}

	public void Toggle()
	{
		Visible = !Visible;
		Refresh();
	}

	public void Close()
	{
		Visible = false;
	}

	private void OnCompleteRift()
	{
		world?.CheatCompleteRift();
	}

	private void OnAddXp()
	{
		player?.GainXp(25);
	}

	private void OnLevelUp()
	{
		player?.CheatForceLevelUp();
	}

	private void OnAddStones()
	{
		player?.GainMagicStones(5);
	}

	private void OnAddReputation()
	{
		player?.GainReputation(10);
	}

	private void OnFullHeal()
	{
		player?.HealFull();
	}

	private void OnToggleImmortality()
	{
		if (player == null)
		{
			return;
		}

		player.IsImmortal = !player.IsImmortal;
		Refresh();
	}

	private void Refresh()
	{
		if (player == null)
		{
			return;
		}

		if (statusLabel != null)
		{
			statusLabel.Text =
				"Level " + player.Level + "  XP " + player.Xp +
				"  Rep " + player.Reputation + "  Stones " + player.MagicStones +
				"\nHP " + player.CurrentHealth.ToString("0") + "/" + player.MaxHealth.ToString("0") +
				"  Rift Tier " + (world?.CurrentTier ?? 1) +
				"\nImmortality: " + (player.IsImmortal ? "ON" : "OFF");
		}

		if (immortalityButton != null)
		{
			immortalityButton.Text = player.IsImmortal ? "Immortality: ON" : "Immortality: OFF";
		}
	}
}
