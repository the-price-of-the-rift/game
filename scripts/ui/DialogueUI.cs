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
	private RichTextLabel? bodyLabel;
	private Button? learnButton;
	private Button? leaveButton;
	private string pendingAbilityId = "";

	public override void _Ready()
	{
		Visible = false;
		nameLabel = GetNodeOrNull<Label>(NameLabelPath);
		bodyLabel = GetNodeOrNull<RichTextLabel>(BodyLabelPath);
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
			SetBody(FormatSpeech(HostileLine(npc.Faction)));
		}
		else if (npc.IsTeacher && npc.Faction == Faction.Kings && standing == Standing.Friendly && npc.TeachBranch != BuildBranch.None)
		{
			string nextAbility = player.GetNextTeachableActive(npc.TeachBranch);
			if (string.IsNullOrEmpty(nextAbility))
			{
				SetBody(FormatSpeech("I have nothing left to teach you."));
			}
			else if (AbilityDefinitions.All.TryGetValue(nextAbility, out AbilityDefinition? definition))
			{
				pendingAbilityId = nextAbility;
				offerTeach = true;
				string speech = FormatSpeech("You have earned my trust. Let me teach you " + definition.DisplayName + ".\n" + definition.Description);
				string outro = definition.IsBranchChoice && player.ChosenBranch == BuildBranch.None
					? "\n\n[font_size=8][i]\"" + GetClassIntro(definition.Branch) + "\"[/i][/font_size]"
					: "";
				SetBody(speech + outro);
			}
			else
			{
				SetBody(FormatSpeech(string.IsNullOrEmpty(npc.FlavorText) ? "Well met." : npc.FlavorText));
			}
		}
		else if (npc.IsTeacher && npc.Faction == Faction.Kings && npc.TeachBranch != BuildBranch.None)
		{
			SetBody(FormatSpeech("Prove yourself to the crown first. Then I will teach you what I know."));
		}
		else
		{
			SetBody(FormatSpeech(string.IsNullOrEmpty(npc.FlavorText) ? "Safe travels, reeve." : npc.FlavorText));
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
				SetBody(FormatSpeech("Learned " + definition.DisplayName + ". Use it with Q, E, or R."));
			}
		}
		else
		{
			SetBody(FormatSpeech(player.LastTreeMessage));
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

	// Prefixes each line with a dash so speech reads as the NPC talking, distinct from
	// the unprefixed, smaller, italicized class-intro text appended after it.
	private static string FormatSpeech(string text)
	{
		string[] lines = text.Split('\n');
		for (int i = 0; i < lines.Length; i++)
		{
			lines[i] = "- " + lines[i];
		}
		return string.Join("\n", lines);
	}

	// Shown once, right before the player's first branch-choice active is taught, since
	// learning it locks ChosenBranch permanently with no respec anywhere in the game.
	private static string GetClassIntro(BuildBranch branch)
	{
		return branch switch
		{
			BuildBranch.Warrior =>
				"Choose this path and you will carry a sword. The Warrior is a heavily armored " +
				"melee fighter: high damage and defense, able to block incoming hits with a shield, " +
				"but slow on their feet and poor at dodging or fighting at range.",
			BuildBranch.Ranger =>
				"Choose this path and you will carry a bow. The Ranger is a swift ranged fighter: " +
				"the highest damage output and best mobility of the three, but fragile in melee and " +
				"weak once the fight closes in.",
			BuildBranch.Scout =>
				"Choose this path and you will carry daggers and throwing darts. The Scout is a nimble " +
				"hybrid, mixing quick melee strikes with ranged darts and excellent evasion, but with " +
				"low resistance to damage.",
			_ => "",
		};
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
