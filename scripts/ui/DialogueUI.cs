#nullable enable

using Godot;

// Persona-style minimal dialogue: a line of text, a primary action button, an optional
// row of stone-trade buttons, and Leave.
//
// The primary "Learn" button changes meaning per NPC role (see ActionKind): King's
// guardians teach an existing tree active (free-unlock), merchants sell HP potions, the
// King's steward accepts collected taxes for reputation, and taxable villagers can be
// shaken down for coin. The three Trade buttons are shown only for the stone-trader
// (Weller), who buys magic stones for coin. Flavor NPCs (Lina, plain villagers) show
// reputation-tiered text only. Coin is a single currency (Player.Money) shared by stone
// sales, taxes, and purchases.
public partial class DialogueUI : CanvasLayer
{
	[Export] public NodePath NameLabelPath { get; set; } = new NodePath();
	[Export] public NodePath BodyLabelPath { get; set; } = new NodePath();
	[Export] public NodePath LearnButtonPath { get; set; } = new NodePath();
	[Export] public NodePath TradeButtonPath { get; set; } = new NodePath();
	[Export] public NodePath TradeTenButtonPath { get; set; } = new NodePath();
	[Export] public NodePath TradeAllButtonPath { get; set; } = new NodePath();
	[Export] public NodePath LeaveButtonPath { get; set; } = new NodePath();

	private const int TradeTenCount = 10;

	private enum ActionKind
	{
		None,
		Teach,
		BuyPotion,
		DeliverTaxes,
		CollectTax,
	}

	private Player? player;
	private Label? nameLabel;
	private RichTextLabel? bodyLabel;
	private Button? actionButton;
	private Button? tradeButton;
	private Button? tradeTenButton;
	private Button? tradeAllButton;
	private Button? leaveButton;

	private ActionKind pendingAction = ActionKind.None;
	private string pendingAbilityId = "";
	private Interactable? currentNpc;

	public override void _Ready()
	{
		Visible = false;
		nameLabel = GetNodeOrNull<Label>(NameLabelPath);
		bodyLabel = GetNodeOrNull<RichTextLabel>(BodyLabelPath);
		actionButton = GetNodeOrNull<Button>(LearnButtonPath);
		tradeButton = GetNodeOrNull<Button>(TradeButtonPath);
		tradeTenButton = GetNodeOrNull<Button>(TradeTenButtonPath);
		tradeAllButton = GetNodeOrNull<Button>(TradeAllButtonPath);
		leaveButton = GetNodeOrNull<Button>(LeaveButtonPath);

		if (actionButton != null)
		{
			actionButton.Pressed += OnActionPressed;
		}
		if (tradeButton != null)
		{
			tradeButton.Pressed += OnTradePressed;
		}
		if (tradeTenButton != null)
		{
			tradeTenButton.Pressed += OnTradeTenPressed;
		}
		if (tradeAllButton != null)
		{
			tradeAllButton.Pressed += OnTradeAllPressed;
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

		currentNpc = npc;
		pendingAction = ActionKind.None;
		pendingAbilityId = "";

		Standing standing = Factions.GetStanding(npc.Faction, player.Reputation);
		if (nameLabel != null)
		{
			nameLabel.Text = npc.DisplayName + "  (" + Factions.GetFactionName(npc.Faction) + " - " + standing + ")";
		}

		// A hostile NPC never trades, teaches, or takes taxes - just refuses.
		if (standing == Standing.Hostile && !npc.GivesTax)
		{
			SetBody(FormatSpeech(RepFlavor(npc)));
		}
		else if (npc.IsStoneTrader)
		{
			SetBody(FormatSpeech(TradeOfferLine(player)));
		}
		else
		{
			switch (npc.Kind)
			{
				case InteractableKind.Merchant:
					OpenMerchant(npc);
					break;
				case InteractableKind.TaxOfficer:
					OpenTaxOfficer(npc, standing);
					break;
				default:
					OpenNpc(npc, standing);
					break;
			}
		}

		RefreshActionButton();
		RefreshTradeButtons();
		Visible = true;
	}

	public void Close()
	{
		Visible = false;
		currentNpc = null;
	}

	private void OpenMerchant(Interactable npc)
	{
		SetBody(FormatSpeech(RepFlavor(npc)) +
			"\n\n- HP potions restore health when you press [1]. " + npc.PotionPrice + " coin each.");
		pendingAction = ActionKind.BuyPotion;
	}

	private void OpenTaxOfficer(Interactable npc, Standing standing)
	{
		if (player == null)
		{
			return;
		}

		if (player.Money <= 0)
		{
			SetBody(FormatSpeech("Bring me the taxes you gather from the village, reeve. The crown is waiting."));
			return;
		}

		SetBody(FormatSpeech("You have " + player.Money + " coin collected. Hand it to the crown and your standing will rise."));
		pendingAction = ActionKind.DeliverTaxes;
	}

	private void OpenNpc(Interactable npc, Standing standing)
	{
		if (player == null)
		{
			return;
		}

		// King's guardians teach the next active in their branch once trusted.
		if (npc.IsTeacher && npc.Faction == Faction.Kings && npc.TeachBranch != BuildBranch.None)
		{
			OpenTeacher(npc, standing);
			return;
		}

		// Taxable villager: reputation-flavored line, plus a one-time shakedown.
		SetBody(FormatSpeech(RepFlavor(npc)));

		if (npc.GivesTax)
		{
			if (player.HasCollectedTax(npc.DisplayName))
			{
				AppendBody("\n\n- (You have already taken this household's tax.)");
			}
			else
			{
				pendingAction = ActionKind.CollectTax;
			}
		}
	}

	private void OpenTeacher(Interactable npc, Standing standing)
	{
		if (player == null)
		{
			return;
		}

		if (standing != Standing.Friendly)
		{
			SetBody(FormatSpeech("Prove yourself to the crown first. Then I will teach you what I know."));
			return;
		}

		string nextAbility = player.GetNextTeachableActive(npc.TeachBranch);
		if (string.IsNullOrEmpty(nextAbility))
		{
			SetBody(FormatSpeech("I have nothing left to teach you."));
			return;
		}

		if (!AbilityDefinitions.All.TryGetValue(nextAbility, out AbilityDefinition? definition))
		{
			SetBody(FormatSpeech(string.IsNullOrEmpty(npc.FlavorText) ? "Well met." : npc.FlavorText));
			return;
		}

		pendingAbilityId = nextAbility;
		pendingAction = ActionKind.Teach;
		string speech = FormatSpeech("You have earned my trust. Let me teach you " + definition.DisplayName + ".\n" + definition.Description);
		string outro = definition.IsBranchChoice && player.ChosenBranch == BuildBranch.None
			? "\n\n[font_size=8][i]\"" + GetClassIntro(definition.Branch) + "\"[/i][/font_size]"
			: "";
		SetBody(speech + outro);
	}

	private void OnActionPressed()
	{
		if (player == null || currentNpc == null)
		{
			return;
		}

		switch (pendingAction)
		{
			case ActionKind.Teach:
				DoTeach();
				break;
			case ActionKind.BuyPotion:
				DoBuyPotion();
				break;
			case ActionKind.DeliverTaxes:
				DoDeliverTaxes();
				break;
			case ActionKind.CollectTax:
				DoCollectTax();
				break;
		}

		RefreshActionButton();
	}

	private void DoTeach()
	{
		if (player == null || string.IsNullOrEmpty(pendingAbilityId))
		{
			return;
		}

		if (player.FreeUnlock(pendingAbilityId) &&
			AbilityDefinitions.All.TryGetValue(pendingAbilityId, out AbilityDefinition? definition))
		{
			SetBody(FormatSpeech("Learned " + definition.DisplayName + ". Use it with Q, E, or R."));
		}
		else
		{
			SetBody(FormatSpeech(player.LastTreeMessage));
		}

		pendingAction = ActionKind.None;
	}

	private void DoBuyPotion()
	{
		if (player == null || currentNpc == null)
		{
			return;
		}

		if (player.SpendMoney(currentNpc.PotionPrice))
		{
			player.AddPotions(1);
			SetBody(FormatSpeech("Sold. You now carry " + player.Potions + " HP potion(s). " +
				player.Money + " coin left. Press [1] to drink one."));
		}
		else
		{
			SetBody(FormatSpeech("Not enough coin. A potion costs " + currentNpc.PotionPrice +
				"; you have " + player.Money + "."));
			pendingAction = ActionKind.None;
		}
	}

	private void DoDeliverTaxes()
	{
		if (player == null)
		{
			return;
		}

		int delivered = player.Money;
		if (delivered <= 0)
		{
			SetBody(FormatSpeech("You carry no coin to hand over."));
			pendingAction = ActionKind.None;
			return;
		}

		// Every 2 coin delivered earns 1 reputation with the crown (min +1).
		int repGain = Mathf.Max(1, delivered / 2);
		player.SpendMoney(delivered);
		player.GainReputation(repGain);
		SetBody(FormatSpeech("The crown thanks you. " + delivered + " coin delivered, reputation +" + repGain + "."));
		pendingAction = ActionKind.None;
	}

	private void DoCollectTax()
	{
		if (player == null || currentNpc == null)
		{
			return;
		}

		if (player.CollectTax(currentNpc.DisplayName, currentNpc.TaxAmount))
		{
			// Squeezing a household earns coin but costs a little standing in the village.
			player.GainReputation(-2);
			SetBody(FormatSpeech("You collect " + currentNpc.TaxAmount + " coin in the King's name. " +
				"They will not forget this."));
		}
		else
		{
			SetBody(FormatSpeech("You have already taken this household's tax."));
		}

		pendingAction = ActionKind.None;
	}

	// Sets the primary action button label/visibility from the current pending action.
	private void RefreshActionButton()
	{
		if (actionButton == null)
		{
			return;
		}

		string label = pendingAction switch
		{
			ActionKind.Teach => "Learn",
			ActionKind.BuyPotion => "Buy potion (" + (currentNpc?.PotionPrice ?? 0) + ")",
			ActionKind.DeliverTaxes => "Deliver taxes",
			ActionKind.CollectTax => "Collect tax (" + (currentNpc?.TaxAmount ?? 0) + ")",
			_ => "",
		};

		actionButton.Visible = pendingAction != ActionKind.None;
		actionButton.Disabled = pendingAction == ActionKind.None;
		actionButton.Text = label;
	}

	// The three stone-trade buttons only appear for the stone trader (Weller).
	private void RefreshTradeButtons()
	{
		bool offerTrade = player != null && currentNpc != null && currentNpc.IsStoneTrader &&
			Factions.GetStanding(currentNpc.Faction, player.Reputation) != Standing.Hostile;

		if (tradeButton != null)
		{
			tradeButton.Visible = offerTrade;
			tradeButton.Disabled = !offerTrade || player!.MagicStones <= 0;
		}
		if (tradeTenButton != null)
		{
			tradeTenButton.Visible = offerTrade;
			tradeTenButton.Disabled = !offerTrade || player!.MagicStones < TradeTenCount;
		}
		if (tradeAllButton != null)
		{
			tradeAllButton.Visible = offerTrade;
			tradeAllButton.Disabled = !offerTrade || player!.MagicStones <= 0;
		}
	}

	private void OnTradePressed()
	{
		HandleTrade(1);
	}

	private void OnTradeTenPressed()
	{
		HandleTrade(TradeTenCount);
	}

	private void OnTradeAllPressed()
	{
		if (player != null)
		{
			HandleTrade(player.MagicStones);
		}
	}

	private void HandleTrade(int count)
	{
		if (player == null)
		{
			return;
		}

		if (player.SellMagicStones(count) > 0)
		{
			SetBody(FormatSpeech(TradeOfferLine(player)));
		}
		else
		{
			SetBody(FormatSpeech("You have no magic stones left to trade."));
		}

		RefreshTradeButtons();
	}

	private static string TradeOfferLine(Player player)
	{
		if (player.MagicStones <= 0)
		{
			return "No stones on you? Clear a rift and come back - I'll always be here.";
		}

		return player.GetMagicStonePrice() + " coin a stone, and it's gone for good - no buying it back. " +
			"You are holding " + player.MagicStones + " stone(s) and " + player.Money + " coin.";
	}

	// Picks the reputation-tiered line for an NPC, falling back to FlavorText when a tier
	// line is left empty in the scene.
	private string RepFlavor(Interactable npc)
	{
		if (player == null)
		{
			return npc.FlavorText;
		}

		Standing standing = Factions.GetStanding(npc.Faction, player.Reputation);
		string tier = standing switch
		{
			Standing.Hostile => npc.HostileText,
			Standing.Friendly => npc.FriendlyText,
			_ => npc.FlavorText,
		};

		if (!string.IsNullOrEmpty(tier))
		{
			return tier;
		}

		return string.IsNullOrEmpty(npc.FlavorText) ? HostileLine(npc.Faction) : npc.FlavorText;
	}

	private void SetBody(string text)
	{
		if (bodyLabel != null)
		{
			bodyLabel.Text = text;
		}
	}

	private void AppendBody(string text)
	{
		if (bodyLabel != null)
		{
			bodyLabel.Text += text;
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
