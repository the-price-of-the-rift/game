#nullable enable

using Godot;

// Placeholder vendor panel. There is no currency or item system in the game yet (weapon
// prices are meant to scale linearly with damage per the balance doc, but nothing sells
// weapons yet), so every slot is an empty stub. Wired the same way DialogueUI is - Main
// owns the instance, VillageController opens it on interact and closes it on [F]/re-press -
// so a real inventory can replace the slot contents later without touching the dispatch.
public partial class ShopUI : CanvasLayer
{
	[Export] public NodePath TitleLabelPath { get; set; } = new NodePath();
	[Export] public NodePath StatusLabelPath { get; set; } = new NodePath();
	[Export] public NodePath CloseButtonPath { get; set; } = new NodePath();
	[Export] public NodePath Slot1ButtonPath { get; set; } = new NodePath();
	[Export] public NodePath Slot2ButtonPath { get; set; } = new NodePath();
	[Export] public NodePath Slot3ButtonPath { get; set; } = new NodePath();
	[Export] public NodePath Slot4ButtonPath { get; set; } = new NodePath();

	private Label? titleLabel;
	private Label? statusLabel;

	public override void _Ready()
	{
		Visible = false;
		titleLabel = GetNodeOrNull<Label>(TitleLabelPath);
		statusLabel = GetNodeOrNull<Label>(StatusLabelPath);

		Button? closeButton = GetNodeOrNull<Button>(CloseButtonPath);
		if (closeButton != null)
		{
			closeButton.Pressed += Close;
		}

		BindSlot(Slot1ButtonPath);
		BindSlot(Slot2ButtonPath);
		BindSlot(Slot3ButtonPath);
		BindSlot(Slot4ButtonPath);
	}

	private void BindSlot(NodePath slotPath)
	{
		Button? slotButton = GetNodeOrNull<Button>(slotPath);
		if (slotButton != null)
		{
			slotButton.Pressed += OnSlotPressed;
		}
	}

	public void Open(string vendorName)
	{
		if (titleLabel != null)
		{
			titleLabel.Text = vendorName;
		}
		if (statusLabel != null)
		{
			statusLabel.Text = "Nothing for sale yet - come back later.";
		}

		Visible = true;
	}

	public void Close()
	{
		Visible = false;
	}

	private void OnSlotPressed()
	{
		if (statusLabel != null)
		{
			statusLabel.Text = "This slot is empty.";
		}
	}
}
