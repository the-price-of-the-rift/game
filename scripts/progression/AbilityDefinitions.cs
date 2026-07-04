#nullable enable

using Godot;
using System.Collections.Generic;

public enum BuildBranch
{
	None,
	Warrior,
	Ranger,
	Scout,
}

public enum AbilityKind
{
	Active,
	Passive,
	Key,
}

public sealed class AbilityDefinition
{
	public string Id { get; init; } = string.Empty;
	public string DisplayName { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public int Cost { get; init; } = 1;
	public int RequiredLevel { get; init; } = 1;
	public int RequiredReputation { get; init; } = 0;
	public BuildBranch Branch { get; init; } = BuildBranch.None;
	public AbilityKind Kind { get; init; } = AbilityKind.Passive;
	public bool IsBranchChoice { get; init; } = false;
	public Vector2 TreePosition { get; init; } = Vector2.Zero;
	public string[] Prerequisites { get; init; } = new string[0];

	public bool IsActive => Kind == AbilityKind.Active || Kind == AbilityKind.Key;
}

public static class AbilityDefinitions
{
	public static readonly string[] DisplayOrder =
	[
		"slash",
		"heavy_cut",
		"devastating_cut",
		"multi_arrow",
		"quick_fire",
		"critical_fire",
		"dart_wave",
		"dash_hit",
		"enemy_crash",
		"kings_grace",
		"kings_blessing",
		"kings_courtesy",
		"kings_pardon",
		"strikes_of_justice",
		"strikes_of_judgement",
		"arrows_of_vengeance",
		"payment_time",
		"royal_order",
		"noble_directive",
		"majestic_advice",
		"alchemic_assistance",
	];

	public static readonly Dictionary<string, AbilityDefinition> All = new()
	{
		["slash"] = new AbilityDefinition
		{
			Id = "slash",
			DisplayName = "Slash",
			Description = "Wide sword sweep that hits several nearby enemies.",
			Cost = 1,
			Branch = BuildBranch.Warrior,
			Kind = AbilityKind.Active,
			IsBranchChoice = true,
			RequiredReputation = 10,
			TreePosition = new Vector2(70, 120),
		},
		["heavy_cut"] = new AbilityDefinition
		{
			Id = "heavy_cut",
			DisplayName = "Heavy Cut",
			Description = "Strong single-target sword strike.",
			Cost = 1,
			Branch = BuildBranch.Warrior,
			Kind = AbilityKind.Active,
			RequiredReputation = 20,
			TreePosition = new Vector2(255, 72),
			Prerequisites = ["slash"],
		},
		["devastating_cut"] = new AbilityDefinition
		{
			Id = "devastating_cut",
			DisplayName = "Devastating Cut",
			Description = "Leap toward the target and crash down for huge damage.",
			Cost = 2,
			Branch = BuildBranch.Warrior,
			Kind = AbilityKind.Key,
			RequiredLevel = 2,
			RequiredReputation = 35,
			TreePosition = new Vector2(255, 182),
			Prerequisites = ["heavy_cut"],
		},
		["multi_arrow"] = new AbilityDefinition
		{
			Id = "multi_arrow",
			DisplayName = "Multi-Arrow",
			Description = "Fire 5 arrows in a spread.",
			Cost = 1,
			Branch = BuildBranch.Ranger,
			Kind = AbilityKind.Active,
			IsBranchChoice = true,
			RequiredReputation = 10,
			TreePosition = new Vector2(480, 120),
		},
		["quick_fire"] = new AbilityDefinition
		{
			Id = "quick_fire",
			DisplayName = "Quick Fire",
			Description = "Double your firing speed for a short time.",
			Cost = 1,
			Branch = BuildBranch.Ranger,
			Kind = AbilityKind.Active,
			RequiredReputation = 20,
			TreePosition = new Vector2(665, 72),
			Prerequisites = ["multi_arrow"],
		},
		["critical_fire"] = new AbilityDefinition
		{
			Id = "critical_fire",
			DisplayName = "Critical Fire",
			Description = "Double firing speed and add 40% crit damage for a short time.",
			Cost = 2,
			Branch = BuildBranch.Ranger,
			Kind = AbilityKind.Key,
			RequiredLevel = 2,
			RequiredReputation = 35,
			TreePosition = new Vector2(665, 182),
			Prerequisites = ["quick_fire"],
		},
		["dart_wave"] = new AbilityDefinition
		{
			Id = "dart_wave",
			DisplayName = "Dart Wave",
			Description = "Throw 3 darts three times in a row.",
			Cost = 1,
			Branch = BuildBranch.Scout,
			Kind = AbilityKind.Active,
			IsBranchChoice = true,
			RequiredReputation = 10,
			TreePosition = new Vector2(890, 120),
		},
		["dash_hit"] = new AbilityDefinition
		{
			Id = "dash_hit",
			DisplayName = "Dash Hit",
			Description = "Dash ahead and deal huge melee damage.",
			Cost = 1,
			Branch = BuildBranch.Scout,
			Kind = AbilityKind.Active,
			RequiredReputation = 20,
			TreePosition = new Vector2(1075, 72),
			Prerequisites = ["dart_wave"],
		},
		["enemy_crash"] = new AbilityDefinition
		{
			Id = "enemy_crash",
			DisplayName = "Enemy Crash",
			Description = "Dash through enemies, then gain movement and attack speed.",
			Cost = 2,
			Branch = BuildBranch.Scout,
			Kind = AbilityKind.Key,
			RequiredLevel = 2,
			RequiredReputation = 35,
			TreePosition = new Vector2(1075, 182),
			Prerequisites = ["dash_hit"],
		},
		["kings_grace"] = new AbilityDefinition
		{
			Id = "kings_grace",
			DisplayName = "King's Grace",
			Description = "Gain 5 shield HP that slowly regenerates.",
			Cost = 1,
			Kind = AbilityKind.Passive,
			TreePosition = new Vector2(480, 340),
		},
		["kings_blessing"] = new AbilityDefinition
		{
			Id = "kings_blessing",
			DisplayName = "King's Blessing",
			Description = "Increase shield HP to 15.",
			Cost = 1,
			Branch = BuildBranch.Warrior,
			Kind = AbilityKind.Passive,
			TreePosition = new Vector2(250, 450),
			Prerequisites = ["kings_grace"],
		},
		["kings_courtesy"] = new AbilityDefinition
		{
			Id = "kings_courtesy",
			DisplayName = "King's Courtesy",
			Description = "Keep 5 shield HP and increase ranged damage by 15%.",
			Cost = 1,
			Branch = BuildBranch.Ranger,
			Kind = AbilityKind.Passive,
			TreePosition = new Vector2(480, 450),
			Prerequisites = ["kings_grace"],
		},
		["kings_pardon"] = new AbilityDefinition
		{
			Id = "kings_pardon",
			DisplayName = "King's Pardon",
			Description = "Keep 5 shield HP and gain 10% dodge chance.",
			Cost = 1,
			Branch = BuildBranch.Scout,
			Kind = AbilityKind.Passive,
			TreePosition = new Vector2(710, 450),
			Prerequisites = ["kings_grace"],
		},
		["strikes_of_justice"] = new AbilityDefinition
		{
			Id = "strikes_of_justice",
			DisplayName = "Strikes of Justice",
			Description = "Below 20% HP, gain 50% crit chance.",
			Cost = 1,
			Kind = AbilityKind.Passive,
			RequiredLevel = 2,
			TreePosition = new Vector2(480, 560),
		},
		["strikes_of_judgement"] = new AbilityDefinition
		{
			Id = "strikes_of_judgement",
			DisplayName = "Strikes of Judgement",
			Description = "Increase melee damage by 25% and gain 75% crit chance below 20% HP.",
			Cost = 1,
			Branch = BuildBranch.Warrior,
			Kind = AbilityKind.Passive,
			TreePosition = new Vector2(250, 670),
			Prerequisites = ["strikes_of_justice"],
		},
		["arrows_of_vengeance"] = new AbilityDefinition
		{
			Id = "arrows_of_vengeance",
			DisplayName = "Arrows of Vengeance",
			Description = "Increase arrow damage by 25% and gain 75% crit chance below 20% HP.",
			Cost = 1,
			Branch = BuildBranch.Ranger,
			Kind = AbilityKind.Passive,
			TreePosition = new Vector2(480, 670),
			Prerequisites = ["strikes_of_justice"],
		},
		["payment_time"] = new AbilityDefinition
		{
			Id = "payment_time",
			DisplayName = "Payment Time",
			Description = "Below 20% HP, boost damage, crit chance, and movement speed.",
			Cost = 1,
			Branch = BuildBranch.Scout,
			Kind = AbilityKind.Passive,
			TreePosition = new Vector2(710, 670),
			Prerequisites = ["strikes_of_justice"],
		},
		["royal_order"] = new AbilityDefinition
		{
			Id = "royal_order",
			DisplayName = "Royal Order",
			Description = "Reduce active skill cooldowns by 25%.",
			Cost = 1,
			Kind = AbilityKind.Passive,
			RequiredLevel = 3,
			TreePosition = new Vector2(480, 780),
		},
		["noble_directive"] = new AbilityDefinition
		{
			Id = "noble_directive",
			DisplayName = "Noble Directive",
			Description = "Reduce active skill cooldowns by 40% and gain 15 shield HP.",
			Cost = 1,
			Branch = BuildBranch.Warrior,
			Kind = AbilityKind.Passive,
			TreePosition = new Vector2(250, 890),
			Prerequisites = ["royal_order"],
		},
		["majestic_advice"] = new AbilityDefinition
		{
			Id = "majestic_advice",
			DisplayName = "Majestic Advice",
			Description = "Reduce active skill cooldowns by 40% and let arrows pierce enemies.",
			Cost = 1,
			Branch = BuildBranch.Ranger,
			Kind = AbilityKind.Passive,
			TreePosition = new Vector2(480, 890),
			Prerequisites = ["royal_order"],
		},
		["alchemic_assistance"] = new AbilityDefinition
		{
			Id = "alchemic_assistance",
			DisplayName = "Alchemic Assistance",
			Description = "Reduce active skill cooldowns by 40% and poison enemies with darts.",
			Cost = 1,
			Branch = BuildBranch.Scout,
			Kind = AbilityKind.Passive,
			TreePosition = new Vector2(710, 890),
			Prerequisites = ["royal_order"],
		},
	};

	public static string GetBranchName(BuildBranch branch)
	{
		return branch switch
		{
			BuildBranch.Warrior => "Warrior",
			BuildBranch.Ranger => "Ranger",
			BuildBranch.Scout => "Scout",
			_ => "None",
		};
	}
}
