#nullable enable

using Godot;

public enum Faction
{
	Villagers,
	Order,
	Outcasts,
}

public enum Standing
{
	Hostile,
	Neutral,
	Friendly,
}

// One global reputation drives every faction; each reacts at its own thresholds.
// Order stays cold until high rep (harsh Order favors the Warrior build, per the GDD);
// Outcasts warm up earliest. No per-faction bookkeeping.
public static class Factions
{
	public static Standing GetStanding(Faction faction, int reputation)
	{
		(int neutral, int friendly) = faction switch
		{
			Faction.Order => (25, 55),
			Faction.Villagers => (15, 40),
			Faction.Outcasts => (5, 25),
			_ => (15, 40),
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
			Faction.Order => "Order",
			Faction.Outcasts => "Outcasts",
			_ => "Villagers",
		};
	}
}
