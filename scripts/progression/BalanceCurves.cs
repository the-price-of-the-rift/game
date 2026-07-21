#nullable enable

using Godot;

// Central home for the game's progression curves, so every caller reads the same shape
// instead of re-deriving it. Two curves live here:
//   - XP reward per kill grows EXPONENTIALLY with rift tier (harder tiers pay off faster).
//   - Level, as a function of accumulated XP, grows LOGARITHMICALLY: Level(XP) ~ log(XP).
//     Inverted, that means the XP REQUIREMENT per level is exponential in level - each
//     level costs double the XP of the previous one. That is the "harsh law of
//     diminishing returns": the curve punishes leveling further, it doesn't reward it.
//
// The two curves are cross-calibrated, not picked independently: with enemy count fixed
// at (3 + tier) per RiftController, these constants put level 2 just after clearing tier 1
// and level 3 (the level cap) about halfway through tier 3 - so hitting max level takes
// real multi-tier progress instead of happening inside the very first rift.
public static class BalanceCurves
{
	private const float BaseKillXp = 6.0f;
	private const float XpRewardGrowthPerTier = 1.2f;

	public static int GetXpReward(int riftTier)
	{
		return Mathf.RoundToInt(BaseKillXp * Mathf.Pow(XpRewardGrowthPerTier, riftTier - 1));
	}

	private const float LevelBaseXp = 40.0f;
	private const float LevelXpDoublingRate = 2.0f;

	// Level 1 is the free starting level (0 XP). Level 2 needs 40 XP; every level after
	// that doubles the previous level's requirement (40, 80, 160, ...), so climbing gets
	// harder, not easier, the further the player progresses.
	public static int GetXpRequiredForLevel(int level)
	{
		if (level <= 1)
		{
			return 0;
		}

		return Mathf.RoundToInt(LevelBaseXp * Mathf.Pow(LevelXpDoublingRate, level - 2));
	}
}
