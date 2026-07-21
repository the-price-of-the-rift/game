#nullable enable

using Godot;
using System.Collections.Generic;

// Blacksmith panel. Stocks the player's own class's weapons (WeaponDefinitions), one per
// slot, priced per docs/LATEST.md section 7.4 and rebalanced there where the doc's numbers
// were too far apart (see WeaponDefinitions for the reasoning). Hovering a slot shows its
// description in a fixed-width wrapping label instead of Godot's native tooltip - the
// native one sizes to the text and can run off the 640x360 screen for long descriptions.
// Purchases are permanent (Player.OwnedWeaponIds); [Z] cycles the equipped weapon among
// owned ones for free while this panel is open (Player.CycleWeapon). Wired the same way
// DialogueUI is - Main owns the instance, VillageController opens it on interact and closes
// it on [F]/re-press.
public partial class ShopUI : CanvasLayer
{
	[Export] public NodePath TitleLabelPath { get; set; } = new NodePath();
	[Export] public NodePath StatusLabelPath { get; set; } = new NodePath();
	[Export] public NodePath DescriptionLabelPath { get; set; } = new NodePath();
	[Export] public NodePath CloseButtonPath { get; set; } = new NodePath();
	[Export] public NodePath Slot1ButtonPath { get; set; } = new NodePath();
	[Export] public NodePath Slot2ButtonPath { get; set; } = new NodePath();
	[Export] public NodePath Slot3ButtonPath { get; set; } = new NodePath();
	[Export] public NodePath Slot4ButtonPath { get; set; } = new NodePath();

	private Player? player;
	private Label? titleLabel;
	private Label? statusLabel;
	private Label? descriptionLabel;
	private Button?[] slotButtons = new Button?[0];
	private string[] slotWeaponIds = new string[0];

	public override void _Ready()
	{
		Visible = false;
		titleLabel = GetNodeOrNull<Label>(TitleLabelPath);
		statusLabel = GetNodeOrNull<Label>(StatusLabelPath);
		descriptionLabel = GetNodeOrNull<Label>(DescriptionLabelPath);

		Button? closeButton = GetNodeOrNull<Button>(CloseButtonPath);
		if (closeButton != null)
		{
			closeButton.Pressed += Close;
		}

		slotButtons = new[]
		{
			GetNodeOrNull<Button>(Slot1ButtonPath),
			GetNodeOrNull<Button>(Slot2ButtonPath),
			GetNodeOrNull<Button>(Slot3ButtonPath),
			GetNodeOrNull<Button>(Slot4ButtonPath),
		};
		slotWeaponIds = new string[slotButtons.Length];

		for (int index = 0; index < slotButtons.Length; index++)
		{
			int capturedIndex = index;
			Button? button = slotButtons[index];
			if (button != null)
			{
				button.Pressed += () => OnSlotPressed(capturedIndex);
				button.MouseEntered += () => OnSlotHovered(capturedIndex);
				button.MouseExited += OnSlotUnhovered;
			}
		}
	}

	public override void _Process(double delta)
	{
		if (!Visible || player == null)
		{
			return;
		}

		if (Input.IsActionJustPressed("cycle_weapon"))
		{
			player.CycleWeapon();
			RefreshSlots();
			SetStatus(player.LastTreeMessage);
		}
	}

	public void Bind(Player boundPlayer)
	{
		player = boundPlayer;
	}

	public void Open(string vendorName)
	{
		if (titleLabel != null)
		{
			titleLabel.Text = vendorName;
		}

		Visible = true;
		RefreshSlots();
		SetDescription("");
	}

	public void Close()
	{
		Visible = false;
	}

	private void RefreshSlots()
	{
		if (player == null)
		{
			return;
		}

		if (player.ChosenBranch == BuildBranch.None)
		{
			SetStatus("Choose a class from a King's guardian before the smith will show you steel.");
			for (int index = 0; index < slotButtons.Length; index++)
			{
				HideSlot(index);
			}
			return;
		}

		IReadOnlyList<string> weaponIds = WeaponDefinitions.GetForBranch(player.ChosenBranch);
		SetStatus("You have " + player.Money + " coin. Owned: " + DescribeOwned() + ". Equipped: " +
			(string.IsNullOrEmpty(player.EquippedWeaponId)
				? "starting gear"
				: WeaponDefinitions.All[player.EquippedWeaponId].DisplayName) +
			". Press [Z] to switch between owned weapons.\n" + DescribeReputationPricing());

		for (int index = 0; index < slotButtons.Length; index++)
		{
			if (index >= weaponIds.Count || !WeaponDefinitions.All.TryGetValue(weaponIds[index], out WeaponDefinition? definition))
			{
				HideSlot(index);
				continue;
			}

			slotWeaponIds[index] = definition.Id;
			Button? button = slotButtons[index];
			if (button == null)
			{
				continue;
			}

			bool owned = player.OwnsWeapon(definition.Id);
			bool equipped = player.EquippedWeaponId == definition.Id;
			int price = player.GetWeaponPrice(definition);
			button.Visible = true;
			button.Disabled = equipped || (!owned && player.Money < price);
			string priceLine = owned ? "owned" : price + " coin";
			button.Text = definition.DisplayName + "\n" + priceLine + (equipped ? "\n(equipped)" : "");
		}
	}

	// Blacksmith prices track standing with the Villagers (Player.GetWeaponPrice):
	// hostile reputation adds a markup, friendly reputation discounts. Spelled out here
	// since the per-slot prices already reflect it but don't say why.
	private string DescribeReputationPricing()
	{
		if (player == null)
		{
			return "";
		}

		Standing standing = Factions.GetStanding(Faction.Villagers, player.Reputation);
		return standing switch
		{
			Standing.Hostile => "Villagers: Hostile - the smith charges you a 25% markup.",
			Standing.Friendly => "Villagers: Friendly - the smith cuts you a discount.",
			_ => "Villagers: Neutral - standard prices. Raise reputation for a discount.",
		};
	}

	private string DescribeOwned()
	{
		if (player == null || player.OwnedWeaponIds.Count == 0)
		{
			return "none yet";
		}

		string result = "";
		foreach (string id in player.OwnedWeaponIds)
		{
			if (!WeaponDefinitions.All.TryGetValue(id, out WeaponDefinition? definition))
			{
				continue;
			}

			result += (result.Length > 0 ? ", " : "") + definition.DisplayName;
		}

		return result.Length > 0 ? result : "none yet";
	}

	private void HideSlot(int index)
	{
		slotWeaponIds[index] = "";
		if (slotButtons[index] != null)
		{
			slotButtons[index]!.Visible = false;
		}
	}

	private void OnSlotPressed(int index)
	{
		if (player == null || index >= slotWeaponIds.Length || string.IsNullOrEmpty(slotWeaponIds[index]))
		{
			return;
		}

		player.BuyWeapon(slotWeaponIds[index]);
		RefreshSlots();
		SetStatus(player.LastTreeMessage);
	}

	private void OnSlotHovered(int index)
	{
		if (index >= slotWeaponIds.Length || string.IsNullOrEmpty(slotWeaponIds[index]) ||
			!WeaponDefinitions.All.TryGetValue(slotWeaponIds[index], out WeaponDefinition? definition))
		{
			SetDescription("");
			return;
		}

		SetDescription(definition.Description);
	}

	private void OnSlotUnhovered()
	{
		SetDescription("");
	}

	private void SetStatus(string text)
	{
		if (statusLabel != null)
		{
			statusLabel.Text = text;
		}
	}

	private void SetDescription(string text)
	{
		if (descriptionLabel != null)
		{
			descriptionLabel.Text = text;
		}
	}
}
