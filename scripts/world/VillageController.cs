#nullable enable

using Godot;

// The peaceful village world, mounted in Main's WorldSlot. It owns interaction dispatch
// (board, stations, NPCs, the rift portal) and the dialogue open/close loop. No combat.
// The portal hands control to Main.EnterRift(). On load it applies the scripted house
// collapse whenever Main says the swap has happened (idempotent - the flag survives the
// round trip, so a returning player always sees the damaged village).
public partial class VillageController : Node2D
{
	[Export] public NodePath HousesRootPath { get; set; } = new NodePath();

	private Player? player;
	private MainController? main;
	private DialogueUI? dialogueUi;
	private ShopUI? shopUi;
	private Node? housesRoot;

	public override void _Ready()
	{
		housesRoot = GetNodeOrNull<Node>(HousesRootPath);
	}

	// Called by Main right after AddChild, once the player is placed at PlayerSpawn and
	// its projectile container is re-pointed. Applies the house swap if it already happened.
	public void Init(Player boundPlayer, MainController mainController, DialogueUI boundDialogue, ShopUI? boundShop)
	{
		player = boundPlayer;
		main = mainController;
		dialogueUi = boundDialogue;
		shopUi = boundShop;

		if (main.HouseSwapDone)
		{
			ApplyHouseSwap();
		}

		ApplyLinaPhase();
	}

	// Lina stands in the village before the rift opens and again once she is rescued;
	// while she is lost inside the rift her node is hidden (and skipped by interaction,
	// which already ignores invisible interactables).
	private void ApplyLinaPhase()
	{
		if (main == null)
		{
			return;
		}

		foreach (Node node in GetTree().GetNodesInGroup("interactables"))
		{
			if (node is not Interactable interactable || !interactable.IsLina)
			{
				continue;
			}

			interactable.Visible = main.LinaPresent;

			if (main.CampaignComplete)
			{
				interactable.FlavorText = "You pulled me out of that place. I do not have the words, reeve. Whatever they call you now - to me you are the one who came back.";
				interactable.HostileText = interactable.FlavorText;
				interactable.FriendlyText = interactable.FlavorText;
			}
		}
	}

	public override void _Process(double delta)
	{
		if (player == null || main == null)
		{
			return;
		}

		if (dialogueUi != null && dialogueUi.Visible)
		{
			if (Input.IsActionJustPressed("interact"))
			{
				dialogueUi.Close();
			}
			return;
		}

		if (shopUi != null && shopUi.Visible)
		{
			if (Input.IsActionJustPressed("interact"))
			{
				shopUi.Close();
			}
			return;
		}

		if (main.UiBlocking)
		{
			return;
		}

		HandleInteractions();
		UpdatePrompt();
	}

	private Interactable? GetInteractableInRange()
	{
		if (player == null)
		{
			return null;
		}

		Interactable? nearest = null;
		float nearestDistance = float.MaxValue;

		foreach (Node node in GetTree().GetNodesInGroup("interactables"))
		{
			if (node is not Interactable interactable || !IsInstanceValid(interactable) || !interactable.Visible)
			{
				continue;
			}

			float distance = player.GlobalPosition.DistanceTo(interactable.GlobalPosition);
			if (distance <= interactable.Radius && distance < nearestDistance)
			{
				nearest = interactable;
				nearestDistance = distance;
			}
		}

		return nearest;
	}

	private void HandleInteractions()
	{
		if (player == null || !Input.IsActionJustPressed("interact"))
		{
			return;
		}

		Interactable? target = GetInteractableInRange();
		if (target != null)
		{
			Interact(target);
		}
	}

	private void Interact(Interactable target)
	{
		if (player == null)
		{
			return;
		}

		switch (target.Kind)
		{
			case InteractableKind.Portal:
				main?.EnterRift();
				break;
			case InteractableKind.Board:
				player.SetLastTreeMessage(BuildBoardText());
				break;
			case InteractableKind.Station:
				if (target.HealsPlayer)
				{
					player.HealFull();
					player.SetLastTreeMessage("You rest at the " + target.DisplayName + " and recover fully.");
				}
				else
				{
					player.SetLastTreeMessage(string.IsNullOrEmpty(target.FlavorText)
						? "The " + target.DisplayName + " is not ready for use yet."
						: target.FlavorText);
				}
				break;
			case InteractableKind.Npc:
			case InteractableKind.Merchant:
			case InteractableKind.TaxOfficer:
				dialogueUi?.Open(target);
				break;
			case InteractableKind.Shop:
				shopUi?.Open(target.DisplayName);
				break;
		}
	}

	private string BuildBoardText()
	{
		if (player == null)
		{
			return "";
		}

		return "Village Notice Board - Reputation " + player.Reputation +
			"\nVillagers: " + Factions.GetStanding(Faction.Villagers, player.Reputation) +
			"  King's Faction: " + Factions.GetStanding(Faction.Kings, player.Reputation) +
			"\nClear rifts to earn the village's trust.";
	}

	private void UpdatePrompt()
	{
		if (main == null)
		{
			return;
		}

		Interactable? target = GetInteractableInRange();
		main.SetPrompt(target != null
			? target.GetPrompt()
			: "Move with WASD, attack with LMB, use Q/E/R actives, press B for the tree.");
	}

	// One-time scripted set-dressing: designated houses swap their "Intact" child for
	// their "Broken" child. Idempotent - safe to call on every village load.
	private void ApplyHouseSwap()
	{
		if (housesRoot == null)
		{
			return;
		}

		foreach (Node child in housesRoot.GetChildren())
		{
			Node? intact = child.GetNodeOrNull("Intact");
			Node? broken = child.GetNodeOrNull("Broken");
			if (intact is CanvasItem intactVisual && broken is CanvasItem brokenVisual)
			{
				intactVisual.Visible = false;
				brokenVisual.Visible = true;
			}
		}
	}
}
