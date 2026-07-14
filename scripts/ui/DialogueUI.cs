#nullable enable

using Godot;

// Persona-style minimal dialogue: a line of text plus Learn/Leave.
// Teacher NPCs offer one existing tree active (free-unlocked); flavor NPCs show text only.
// Standing gates whether teaching is offered at all.
public partial class DialogueUI : CanvasLayer
{
	[Export] public NodePath NameLabelPath { get; set; } = new NodePath();
	[Export] public NodePath BodyLabelPath { get; set; } = new NodePath();
	[Export] public NodePath LearnButtonPath { get; set; } = new NodePath();
	[Export] public NodePath LeaveButtonPath { get; set; } = new NodePath();

	private Player? player;
	private Label? nameLabel;
	private Label? bodyLabel;
	private Button? learnButton;
	private Button? leaveButton;
	private string pendingAbilityId = "";

	public override void _Ready()
	{
		Visible = false;
		nameLabel = GetNodeOrNull<Label>(NameLabelPath);
		bodyLabel = GetNodeOrNull<Label>(BodyLabelPath);
		learnButton = GetNodeOrNull<Button>(LearnButtonPath);
		leaveButton = GetNodeOrNull<Button>(LeaveButtonPath);

		if (learnButton != null)
		{
			learnButton.Pressed += OnLearnPressed;
		}
		if (leaveButton != null)
		{
			leaveButton.Pressed += Close;
		}
	}

	public void Bind(Player boundPlayer)
	{
		player = boundPlayer;
	}

	public void Open(Interactable npc)
	{
		if (player == null)
		{
			return;
		}

		Standing standing = Factions.GetStanding(npc.Faction, player.Reputation);
		if (nameLabel != null)
		{
			nameLabel.Text = npc.DisplayName + "  (" + Factions.GetFactionName(npc.Faction) + " - " + standing + ")";
		}

		pendingAbilityId = "";
		bool offerTeach = false;

		if (standing == Standing.Hostile)
		{
			SetBody(HostileLine(npc.Faction));
		}
		else if (npc.IsTeacher && npc.Faction == Faction.Kings && standing == Standing.Friendly && npc.TeachBranch != BuildBranch.None)
		{
			string nextAbility = player.GetNextTeachableActive(npc.TeachBranch);
			if (string.IsNullOrEmpty(nextAbility))
			{
				SetBody("I have nothing left to teach you.");
			}
			else if (AbilityDefinitions.All.TryGetValue(nextAbility, out AbilityDefinition? definition))
			{
				pendingAbilityId = nextAbility;
				offerTeach = true;
				SetBody("You have earned my trust. Let me teach you " + definition.DisplayName + ".\n" + definition.Description);
			}
			else
			{
				SetBody(string.IsNullOrEmpty(npc.FlavorText) ? "Well met." : npc.FlavorText);
			}
		}
		else if (npc.IsTeacher && npc.Faction == Faction.Kings && npc.TeachBranch != BuildBranch.None)
		{
			SetBody("Prove yourself to the crown first. Then I will teach you what I know.");
		}
		else
		{
			SetBody(string.IsNullOrEmpty(npc.FlavorText) ? "Safe travels, reeve." : npc.FlavorText);
		}

		if (learnButton != null)
		{
			learnButton.Visible = offerTeach;
			learnButton.Disabled = !offerTeach;
		}

		Visible = true;
	}

	public void Close()
	{
		Visible = false;
	}

	private void OnLearnPressed()
	{
		if (player == null || string.IsNullOrEmpty(pendingAbilityId))
		{
			return;
		}

		if (player.FreeUnlock(pendingAbilityId))
		{
			if (AbilityDefinitions.All.TryGetValue(pendingAbilityId, out AbilityDefinition? definition))
			{
				SetBody("Learned " + definition.DisplayName + ". Use it with Q, E, or R.");
			}
		}
		else
		{
			SetBody(player.LastTreeMessage);
		}

		if (learnButton != null)
		{
			learnButton.Visible = false;
			learnButton.Disabled = true;
		}
	}

	private void SetBody(string text)
	{
		if (bodyLabel != null)
		{
			bodyLabel.Text = text;
		}
	}

	private static string HostileLine(Faction faction)
	{
		return faction switch
		{
			Faction.Kings => "The King's men have no words for a tax collector. Earn your standing.",
			Faction.Villagers => "Leave us be, reeve. You have taken enough.",
			_ => "Go away.",
		};
	}
}
