#nullable enable

using Godot;

public partial class HudController : CanvasLayer
{
	[Export] public NodePath StatsLabelPath { get; set; } = new NodePath();
	[Export] public NodePath LoadoutLabelPath { get; set; } = new NodePath();
	[Export] public NodePath MessageLabelPath { get; set; } = new NodePath();
	[Export] public NodePath HealthFillPath { get; set; } = new NodePath();
	[Export] public NodePath ShieldFillPath { get; set; } = new NodePath();

	private Player? player;
	private MainController? world;
	private Label? statsLabel;
	private Label? loadoutLabel;
	private Label? messageLabel;
	private Control? healthFill;
	private Control? shieldFill;
	private Vector2 healthFillBaseSize;
	private Vector2 shieldFillBaseSize;

	public override void _Ready()
	{
		statsLabel = GetNodeOrNull<Label>(StatsLabelPath);
		loadoutLabel = GetNodeOrNull<Label>(LoadoutLabelPath);
		messageLabel = GetNodeOrNull<Label>(MessageLabelPath);
		healthFill = GetNodeOrNull<Control>(HealthFillPath);
		shieldFill = GetNodeOrNull<Control>(ShieldFillPath);
		healthFillBaseSize = healthFill?.Size ?? Vector2.Zero;
		shieldFillBaseSize = shieldFill?.Size ?? Vector2.Zero;
	}

	public override void _Process(double delta)
	{
		if (player == null)
		{
			return;
		}

		float healthRatio = player.MaxHealth <= 0.0f ? 0.0f : player.CurrentHealth / player.MaxHealth;
		float shieldRatio = player.MaxShield <= 0.0f ? 0.0f : player.CurrentShield / Mathf.Max(1.0f, player.MaxShield);

		if (healthFill != null)
		{
			healthFill.Size = new Vector2(healthFillBaseSize.X * healthRatio, healthFillBaseSize.Y);
		}

		if (shieldFill != null)
		{
			shieldFill.Size = new Vector2(shieldFillBaseSize.X * shieldRatio, shieldFillBaseSize.Y);
		}

		if (statsLabel != null && world != null)
		{
			statsLabel.Text =
				"HP " + player.CurrentHealth.ToString("0") + "/" + player.MaxHealth.ToString("0") +
				"  Shield " + player.CurrentShield.ToString("0") + "/" + player.MaxShield.ToString("0") +
				"\nLevel " + player.Level + "  XP " + player.Xp + "  Rep " + player.Reputation + "  Stones " + player.MagicStones + "  Coin " + player.Money +
				"\nBuild " + player.GetBuildName() + "  Rift Tier " + world.CurrentTier + (world.InRift ? " (in rift)" : " (village)") +
				"\nVillagers: " + Factions.GetStanding(Faction.Villagers, player.Reputation) +
				"  King's Faction: " + Factions.GetStanding(Faction.Kings, player.Reputation);
		}

		if (loadoutLabel != null)
		{
			loadoutLabel.Text = "Q/E/R: " + string.Join(" | ", player.GetActiveLoadout());
		}

		if (messageLabel != null)
		{
			messageLabel.Text = player.LastTreeMessage;
		}
	}

	public void Bind(Player boundPlayer, MainController boundWorld)
	{
		player = boundPlayer;
		world = boundWorld;
	}
}
