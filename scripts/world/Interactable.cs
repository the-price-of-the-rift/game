#nullable enable

using Godot;

public enum InteractableKind
{
	Board,
	Portal,
	Station,
	Npc,
	Shop,
	Merchant,
	TaxOfficer,
}

// A world object the player can trigger with the interact key.
// Pure configuration + a prompt; WorldController owns the dispatch (it already
// centralizes player/rift/dialogue state), matching the existing distance-poll pattern.
public partial class Interactable : Node2D
{
	[Export] public InteractableKind Kind { get; set; } = InteractableKind.Board;
	[Export] public float Radius { get; set; } = 44.0f;
	[Export] public string DisplayName { get; set; } = "object";
	[Export(PropertyHint.MultilineText)] public string FlavorText { get; set; } = "";

	// Station only.
	[Export] public bool HealsPlayer { get; set; } = false;

	// Npc only.
	[Export] public Faction Faction { get; set; } = Faction.Villagers;
	[Export] public bool IsTeacher { get; set; } = false;
	[Export] public string TeachAbilityId { get; set; } = "";
	[Export] public bool IsStoneTrader { get; set; } = false;

	// Teacher only: the branch this guardian trains. Progressive teaching grants the
	// next unlearned active in this branch whose prerequisites are met.
	[Export] public BuildBranch TeachBranch { get; set; } = BuildBranch.None;

	// Lina, the one villager who trusts the reeve. Her node is shown/hidden by phase
	// (present before the rift, gone while she is lost, back once rescued).
	[Export] public bool IsLina { get; set; } = false;

	// Taxable villager (Npc only): the reeve can collect a one-time tax of coins.
	[Export] public bool GivesTax { get; set; } = false;
	[Export] public int TaxAmount { get; set; } = 10;

	// Merchant only: how many coins one HP potion costs.
	[Export] public int PotionPrice { get; set; } = 8;

	// Reputation-tiered NPC flavor. When a line is empty it falls back to FlavorText.
	[Export(PropertyHint.MultilineText)] public string HostileText { get; set; } = "";
	[Export(PropertyHint.MultilineText)] public string FriendlyText { get; set; } = "";

	public override void _Ready()
	{
		AddToGroup("interactables");
	}

	public string GetPrompt()
	{
		return Kind switch
		{
			InteractableKind.Board => "[F] Read the notice board",
			InteractableKind.Portal => "[F] Enter the rift",
			InteractableKind.Station => "[F] Use the " + DisplayName,
			InteractableKind.Npc => "[F] Talk to " + DisplayName,
			InteractableKind.Shop => "[F] Browse the " + DisplayName,
			InteractableKind.Merchant => "[F] Trade with " + DisplayName,
			InteractableKind.TaxOfficer => "[F] Report to " + DisplayName,
			_ => "[F] Interact",
		};
	}
}
