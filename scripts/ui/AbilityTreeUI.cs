#nullable enable

using Godot;

public partial class AbilityTreeUI : CanvasLayer
{
	[Export] public NodePath InfoLabelPath { get; set; } = new NodePath();
	[Export] public NodePath NodesRootPath { get; set; } = new NodePath();
	[Export] public Color LockedColor { get; set; } = new Color(0.22f, 0.24f, 0.28f);
	[Export] public Color AvailableColor { get; set; } = new Color(0.68f, 0.55f, 0.2f);
	[Export] public Color UnlockedColor { get; set; } = new Color(0.22f, 0.55f, 0.28f);
	[Export] public Color BorderColor { get; set; } = new Color(0.85f, 0.85f, 0.82f);

	private PlayerPrototype? player;
	private Label? infoLabel;
	private Control? nodesRoot;
	private readonly Godot.Collections.Dictionary<string, AbilityNodeButton> buttons = new();

	public override void _Ready()
	{
		Visible = false;
		infoLabel = GetNodeOrNull<Label>(InfoLabelPath);
		nodesRoot = GetNodeOrNull<Control>(NodesRootPath);
		CollectButtons(nodesRoot);
	}

	public void Bind(PlayerPrototype boundPlayer)
	{
		player = boundPlayer;
		player.StatsChanged += Refresh;
		player.AbilityUnlocked += OnAbilityUnlocked;
		Refresh();
	}

	public void Toggle()
	{
		Visible = !Visible;
		Refresh();
	}

	private void CollectButtons(Node? node)
	{
		if (node == null)
		{
			return;
		}

		foreach (Node child in node.GetChildren())
		{
			if (child is AbilityNodeButton button && !string.IsNullOrWhiteSpace(button.AbilityId))
			{
				string abilityId = button.AbilityId;
				button.Pressed += () => OnAbilityPressed(abilityId);
				buttons[abilityId] = button;
			}

			CollectButtons(child);
		}
	}

	private void OnAbilityPressed(string abilityId)
	{
		player?.TryUnlock(abilityId);
		Refresh();
	}

	private void OnAbilityUnlocked(string abilityId)
	{
		Refresh();
	}

	private void Refresh()
	{
		if (player == null)
		{
			return;
		}

		if (infoLabel != null)
		{
			infoLabel.Text = player.LastTreeMessage + "\nBuild: " + player.GetBuildName() + " | Level " + player.Level + " | Rep " + player.Reputation + " | Stones " + player.MagicStones;
		}

		foreach ((string abilityId, AbilityNodeButton button) in buttons)
		{
			if (!AbilityDefinitions.All.TryGetValue(abilityId, out AbilityDefinition? definition))
			{
				continue;
			}

			bool unlocked = player.HasAbility(abilityId);
			bool canUnlock = player.CanUnlock(abilityId);
			button.Text = definition.DisplayName + "\nCost " + definition.Cost;
			button.TooltipText = definition.Description + "\nNeed Level " + definition.RequiredLevel + " | Rep " + definition.RequiredReputation;

			Color fill = unlocked ? UnlockedColor : canUnlock ? AvailableColor : LockedColor;
			StyleBoxFlat normal = new()
			{
				BgColor = fill,
				BorderColor = BorderColor,
				BorderWidthLeft = 2,
				BorderWidthTop = 2,
				BorderWidthRight = 2,
				BorderWidthBottom = 2,
			};

			StyleBoxFlat hover = (StyleBoxFlat)normal.Duplicate();
			hover.BgColor = fill.Lightened(0.08f);

			button.AddThemeStyleboxOverride("normal", normal);
			button.AddThemeStyleboxOverride("hover", hover);
			button.AddThemeStyleboxOverride("pressed", hover);
			button.AddThemeColorOverride("font_color", Colors.White);
		}
	}
}
