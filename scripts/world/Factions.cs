#nullable enable

using Godot;

public enum Faction
{
	Villagers,
	Kings,
}

public enum Standing
{
	Hostile,
	Neutral,
	Friendly,
}

// One global reputation drives every faction; each reacts at its own thresholds.
// The King's faction stays cold until high rep (its guardians only teach the trusted);
// Villagers warm up earlier. No per-faction bookkeeping.
public static class Factions
{
	public static Standing GetStanding(Faction faction, int reputation)
	{
		(int neutral, int friendly) = faction switch
		{
			Faction.Kings => (20, 50),
			Faction.Villagers => (10, 30),
			_ => (10, 30),
		};

		if (reputation >= friendly)
		{
			return Standing.Friendly;
		}

		return reputation >= neutral ? Standing.Neutral : Standing.Hostile;
	}

	public static string GetFactionName(Faction faction)
	{
		return faction switch
		{
			Faction.Villagers => "Villagers",
			Faction.Kings => "King's Faction",
			_ => "Villagers",
		};
	}
}
