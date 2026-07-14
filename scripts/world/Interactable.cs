#nullable enable

using Godot;

public enum InteractableKind
{
	Board,
	Portal,
	Station,
	Npc,
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

	// Teacher only: the branch this guardian trains. Progressive teaching grants the
	// next unlearned active in this branch whose prerequisites are met.
	[Export] public BuildBranch TeachBranch { get; set; } = BuildBranch.None;

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
			_ => "[F] Interact",
		};
	}
}
