using Godot;
using System;

public partial class PlayerStats : Node
{
	[Signal]
	public delegate void XpChangedEventHandler(long currentXp, long maxXp);

	[Signal]
	public delegate void LevelUpEventHandler(long newLevel);

	// Cumulative XP required to reach each level, per the GDD leveling table
	// (index 0 -> level 1, index 1 -> level 2, index 2 -> level 3, ...).
	[Export] public int[] LevelXpThresholds { get; set; } = { 0, 25, 60 };

	public int CurrentLevel { get; private set; } = 1;
	public int CurrentXp { get; private set; } = 0;
	public int MaxXpForLevel { get; private set; } = 0;

	public override void _Ready()
	{
		MaxXpForLevel = CalculateMaxXp(CurrentLevel);

		// Initialize the UI with starting values right away
		EmitSignal(SignalName.XpChanged, CurrentXp, MaxXpForLevel);
	}

	// XP needed to go from the given level to the next one, per LevelXpThresholds.
	// Returns 0 once no further level is defined in the table (level capped).
	public int CalculateMaxXp(int level)
	{
		int index = level - 1;
		if (index + 1 < LevelXpThresholds.Length)
		{
			return LevelXpThresholds[index + 1] - LevelXpThresholds[index];
		}

		return 0;
	}

	// Call this method whenever a quest is finished or an enemy is killed
	public void GainXp(int amount)
	{
		if (MaxXpForLevel <= 0)
		{
			return; // already at the highest level defined in LevelXpThresholds
		}

		CurrentXp += amount;

		while (MaxXpForLevel > 0 && CurrentXp >= MaxXpForLevel)
		{
			CurrentXp -= MaxXpForLevel;
			CurrentLevel++;
			MaxXpForLevel = CalculateMaxXp(CurrentLevel);

			EmitSignal(SignalName.LevelUp, CurrentLevel);
		}

		if (MaxXpForLevel <= 0)
		{
			CurrentXp = 0; // capped: keep the bar full instead of overflowing
		}

		// Notify listeners of the final numbers
		EmitSignal(SignalName.XpChanged, CurrentXp, MaxXpForLevel);
	}
}
