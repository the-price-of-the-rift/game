#nullable enable

using Godot;
using System.Collections.Generic;

public sealed class WeaponDefinition
{
	public string Id { get; init; } = string.Empty;
	public string DisplayName { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public BuildBranch Branch { get; init; } = BuildBranch.None;
	public int Price { get; init; } = 1;

	// Multiplies GetMeleeDamage/GetRangedDamage.
	public float DamageMultiplier { get; init; } = 1.0f;
	// Multiplies basic-attack rate of fire (>1 faster, <1 slower); folds into the same
	// multiplier stack as Quick Fire / Enemy Crash in Player.HandleAttack.
	public float AttackSpeedMultiplier { get; init; } = 1.0f;
	// Added flat to Player.ApplyCritical's base crit chance.
	public float CritChanceBonus { get; init; } = 0.0f;
	// Warrior-style crowd control: chance per melee hit (TryMeleeArc) to freeze the
	// target for StunDuration seconds (Enemy.ApplyStun).
	public float StunChance { get; init; } = 0.0f;
	public float StunDuration { get; init; } = 0.0f;
	// Added flat to every TryMeleeArc radius check (can be negative for a shorter blade).
	public float MeleeRangeBonus { get; init; } = 0.0f;
	// Scout-only: whether this weapon's dart/projectile carries the poison tick at all.
	public bool PoisonOnHit { get; init; } = false;
	// Scout-only: whether this weapon's dart/projectile slows the target on hit at all
	// (Enemy.ApplyScoutDebuff's move-speed cut). Kept a separate flag from PoisonOnHit
	// even though only Darts uses either, so the two effects can diverge later.
	public bool SlowOnHit { get; init; } = false;

	public Color Tint { get; init; } = Colors.White;
	// Local-space polygon for the branch's single dominant visual shape (Weapon.SetShape).
	public Vector2[] ShapePoints { get; init; } = System.Array.Empty<Vector2>();
}

// Blacksmith stock, one set per class, per docs/LATEST.md section 7.4 ("Оружие"). Every
// class's "no purchase yet" baseline is fully neutral (1.0x damage/speed, no crit/stun/
// poison) - the same reference point the docs call "Стартовое снаряжение". Each weapon
// below is a real tradeoff off that baseline, not a strict upgrade, except the two
// higher tiers of a single-weapon-type class (Ranger's bow, Scout's daggers): those are
// the same weapon reforged, so they only trade more damage for slower attacks and cost -
// visually they only change color and their one dominant shape polygon (Weapon.SetShape),
// per the "same weapon, recolor it" rule for power tiers.
public static class WeaponDefinitions
{
	// Warrior blade shapes (WarriorVisual/Blade local space, matches the scene's existing
	// scale: x in ~[0,26], y in ~[-10,10]).
	private static readonly Vector2[] SwordShape = { new(2, -3), new(24, -2), new(24, 2), new(2, 3) };
	// Axe head drawn as a "T": a thin haft plus a crossbar near the tip, with the
	// upper (negative-y) arm of the crossbar a bit longer than the lower arm.
	private static readonly Vector2[] AxeShape =
	{
		new(2, -1), new(12, -1), new(12, -12), new(16, -12),
		new(16, 6), new(12, 6), new(12, 1), new(2, 1),
	};
	// Nearly twice the Sword's reach so its silhouette reads as long as its +18 melee
	// range bonus actually is - a long haft with a short diamond point at the tip.
	private static readonly Vector2[] SpearShape = { new(2, -1), new(34, -1), new(42, 0), new(34, 1), new(2, 1) };

	// Ranger bow-arc shapes (RangerVisual/BowArc local space).
	private static readonly Vector2[] BowShape = { new(-3, -12), new(1, -10), new(5, -4), new(6, 0), new(5, 4), new(1, 10), new(-3, 12), new(-1, 0) };
	private static readonly Vector2[] MasterworkBowShape = { new(-4, -13), new(1, -10), new(5, -4), new(6, 0), new(5, 4), new(1, 10), new(-4, 13), new(-1, 0) };
	private static readonly Vector2[] RoyalLongbowShape = { new(-3, -17), new(1, -14), new(6, -6), new(7, 0), new(6, 6), new(1, 14), new(-3, 17), new(-1, 0) };

	// Scout dagger/dart shapes (ScoutVisual/Dagger local space).
	private static readonly Vector2[] DaggerShape = { new(1, -2), new(18, -1), new(22, 0), new(18, 1), new(1, 2) };
	private static readonly Vector2[] DartShape = { new(0, -2), new(10, -4), new(16, 0), new(10, 4), new(0, 2) };
	private static readonly Vector2[] MasterworkDaggerShape = { new(1, -2), new(22, -1), new(27, 0), new(22, 1), new(1, 2) };

	public static readonly Dictionary<string, WeaponDefinition> All = new()
	{
		// Warrior - the doc's Sword/Axe/Spear, each a real tradeoff off the neutral Sword.
		["sword"] = new WeaponDefinition
		{
			Id = "sword",
			DisplayName = "Sword",
			Description = "Balanced steel, no surprises. No bonus, no penalty - the baseline blade.",
			Branch = BuildBranch.Warrior,
			Price = 20,
			DamageMultiplier = 1.0f,
			AttackSpeedMultiplier = 1.0f,
			Tint = new Color(0.75f, 0.78f, 0.82f),
			ShapePoints = SwordShape,
		},
		["axe"] = new WeaponDefinition
		{
			Id = "axe",
			DisplayName = "Axe",
			Description = "Heavy chopping head. +35% damage, -30% attack speed, 15% chance to stun for 1s. Hits like a cart, swings like one too.",
			Branch = BuildBranch.Warrior,
			Price = 24,
			DamageMultiplier = 1.35f,
			AttackSpeedMultiplier = 0.70f,
			StunChance = 0.15f,
			StunDuration = 1.0f,
			Tint = new Color(0.55f, 0.28f, 0.12f),
			ShapePoints = AxeShape,
		},
		["spear"] = new WeaponDefinition
		{
			Id = "spear",
			DisplayName = "Spear",
			Description = "Long haft, quick jabs. -15% damage, +15% attack speed, +18 melee reach. Trades power for safety.",
			Branch = BuildBranch.Warrior,
			Price = 18,
			DamageMultiplier = 0.85f,
			AttackSpeedMultiplier = 1.15f,
			MeleeRangeBonus = 18.0f,
			Tint = new Color(0.25f, 0.45f, 0.4f),
			ShapePoints = SpearShape,
		},

		// Ranger - the doc lists one weapon type; the pricier picks are the same bow
		// reforged, so they only trade more damage for a slower, heavier draw.
		["bow"] = new WeaponDefinition
		{
			Id = "bow",
			DisplayName = "Hunting Bow",
			Description = "A plain bow, fires from range. No bonus, no penalty - the baseline weapon.",
			Branch = BuildBranch.Ranger,
			Price = 20,
			DamageMultiplier = 1.0f,
			AttackSpeedMultiplier = 1.0f,
			Tint = new Color(0.3f, 0.55f, 0.25f),
			ShapePoints = BowShape,
		},
		["masterwork_bow"] = new WeaponDefinition
		{
			Id = "masterwork_bow",
			DisplayName = "Masterwork Bow",
			Description = "The same draw, reinforced. +30% damage, -10% attack speed - hits harder, looses slower.",
			Branch = BuildBranch.Ranger,
			Price = 28,
			DamageMultiplier = 1.30f,
			AttackSpeedMultiplier = 0.90f,
			Tint = new Color(0.85f, 0.7f, 0.25f),
			ShapePoints = MasterworkBowShape,
		},
		["royal_longbow"] = new WeaponDefinition
		{
			Id = "royal_longbow",
			DisplayName = "Royal Longbow",
			Description = "Crown-forged limbs on the same frame. +60% damage, -20% attack speed, +5% crit chance. Hardest-hitting bow in the village, slowest draw.",
			Branch = BuildBranch.Ranger,
			Price = 36,
			DamageMultiplier = 1.60f,
			AttackSpeedMultiplier = 0.80f,
			CritChanceBonus = 0.05f,
			Tint = new Color(0.95f, 0.85f, 0.55f),
			ShapePoints = RoyalLongbowShape,
		},

		// Scout plays as a kite class: Darts poke from range and slow + poison the target
		// (weak on their own), then a melee Dash Hit/Enemy Crash finisher - built from the
		// same GetMeleeDamage the Daggers feed - closes the gap for a big hit, with a bonus
		// against anything currently slowed (Player.ApplyDashDamage). Daggers hit softer
		// than a Warrior's Sword on their own; their payoff is the dash finisher and crit
		// chance, not basic-attack damage. Masterwork Daggers reforges the dagger line the
		// same way the Ranger's bow tiers work.
		["daggers"] = new WeaponDefinition
		{
			Id = "daggers",
			DisplayName = "Daggers",
			Description = "Fast, light blades. -20% damage, +10% crit chance, -4 melee reach, no slow/poison. Weak on their own - the payoff is a Dash Hit/Enemy Crash finisher, not the basic swing.",
			Branch = BuildBranch.Scout,
			Price = 16,
			DamageMultiplier = 0.80f,
			CritChanceBonus = 0.10f,
			MeleeRangeBonus = -4.0f,
			PoisonOnHit = false,
			SlowOnHit = false,
			Tint = new Color(0.82f, 0.86f, 0.9f),
			ShapePoints = DaggerShape,
		},
		["darts"] = new WeaponDefinition
		{
			Id = "darts",
			DisplayName = "Poison Darts",
			Description = "Poison-tipped throwing darts. -20% damage, -10% attack speed, slows on hit and poisons for 1 damage every 4s (2 with Alchemic Assistance). Kite with these, then finish with a dash - it hits harder on a slowed target.",
			Branch = BuildBranch.Scout,
			Price = 19,
			DamageMultiplier = 0.80f,
			AttackSpeedMultiplier = 0.90f,
			PoisonOnHit = true,
			SlowOnHit = true,
			Tint = new Color(0.65f, 0.7f, 0.25f),
			ShapePoints = DartShape,
		},
		["masterwork_daggers"] = new WeaponDefinition
		{
			Id = "masterwork_daggers",
			DisplayName = "Masterwork Daggers",
			Description = "The same blades, folded steel. +30% damage over the plain Daggers, +10% crit chance, -4 melee reach, still no slow/poison.",
			Branch = BuildBranch.Scout,
			Price = 25,
			DamageMultiplier = 1.04f,
			CritChanceBonus = 0.10f,
			MeleeRangeBonus = -4.0f,
			PoisonOnHit = false,
			SlowOnHit = false,
			Tint = new Color(0.65f, 0.35f, 0.85f),
			ShapePoints = MasterworkDaggerShape,
		},
	};

	// Shop display order per class - the blacksmith only stocks the player's own class.
	public static readonly Dictionary<BuildBranch, string[]> ByBranch = new()
	{
		[BuildBranch.Warrior] = new[] { "sword", "axe", "spear" },
		[BuildBranch.Ranger] = new[] { "bow", "masterwork_bow", "royal_longbow" },
		[BuildBranch.Scout] = new[] { "daggers", "darts", "masterwork_daggers" },
	};

	public static IReadOnlyList<string> GetForBranch(BuildBranch branch)
	{
		return ByBranch.TryGetValue(branch, out string[]? ids) ? ids : System.Array.Empty<string>();
	}
}
