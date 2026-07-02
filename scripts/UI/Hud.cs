using Godot;

public partial class Hud : CanvasLayer
{
	private TextureProgressBar _xpBar;
	private Label _xpLevelLabel;

	public override void _Ready()
	{
		_xpBar = GetNode<TextureProgressBar>("XpBarFrame/XpBarFilled");
		_xpLevelLabel = GetNode<Label>("XpLevelLabel");

		var stats = GetNode<PlayerStats>("../Player/PlayerStats");
		stats.XpChanged += OnXpChanged;
		stats.LevelUp += OnLevelUp;

		_xpLevelLabel.Text = $"Lvl {stats.CurrentLevel}";
	}

	private void OnXpChanged(long currentXp, long maxXp)
	{
		if (maxXp <= 0)
		{
			// Capped: no further level defined yet, show the bar as full.
			_xpBar.MaxValue = 1;
			_xpBar.Value = 1;
			return;
		}

		_xpBar.MaxValue = maxXp;
		_xpBar.Value = currentXp;
	}

	private void OnLevelUp(long newLevel)
	{
		_xpLevelLabel.Text = $"Lvl {newLevel}";
	}
}
