using Godot;

// Test-only helper: press the "debug_grant_xp" action (bound to K) to grant XP
// and verify leveling/HUD updates without needing real enemies or quests yet.
public partial class DebugXpGranter : Node
{
	[Export] public int XpPerPress { get; set; } = 10;

	private PlayerStats _stats;

	public override void _Ready()
	{
		_stats = GetNode<PlayerStats>("../Player/PlayerStats");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("debug_grant_xp"))
		{
			_stats.GainXp(XpPerPress);
			GD.Print($"[DebugXpGranter] +{XpPerPress} XP -> level {_stats.CurrentLevel}, {_stats.CurrentXp}/{_stats.MaxXpForLevel}");
		}
	}
}
