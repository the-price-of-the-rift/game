# The Price of the Rift

*Egor Malkov, Ilnur Ramazanov, Aidar Suleimanov*

An Action RPG about a tax collector, a village that hates him, and a magical rift that
swallowed the one person there who didn't.

---

## Pitch

The player is a reeve - a servant of the crown - sent to a poor village to collect
taxes. The villagers despise him on sight; only one person, a cloth merchant, treats him
decently. While walking together, they stumble on a **rift**: a tear in reality leading
into a distorted mini-world. She vanishes into it.

Now the reeve must clear a sequence of these rifts, one after another, to find her -
while everything he does in the village between rifts (how he collects taxes, who he
helps, what he sells, which class he commits to) shapes who he becomes: a faithful
servant of the crown, a village hero, or something in between.

**Central question:** will the hero remain a cold servant of power, or become someone
willing to risk himself for another?

## Core fantasy

Not a chosen hero - a **disputed man**, who decides through his own actions whether he
becomes a savior, a bureaucrat, a traitor, or someone seeking redemption. The player's
agency is primarily narrative and moral: NPC attitudes, merchant prices, skill access,
the missing woman's fate, and the ending are all shaped by what the player actually
does, not by dialogue-wheel flavor text.

## Genre

Action RPG with narrative choice, a reputation system, and linear progression through a
sequence of distorted mini-worlds ("rifts").

---

## Core loop

```
Village (prepare)  →  Rift Gate  →  Rift (fight)  →  Boss  →  Rift cleared
      ↑                                                             |
      └─────────────────────  return to village  ───────────────────┘
```

1. **In the village:** talk to NPCs, spend or sell magic stones, learn active skills
   from trainers, rest at the bonfire, browse the shop, read the notice board.
2. **Enter the rift** through the Rift Gate. Each rift is a self-contained combat arena
   that gets meaningfully harder every tier.
3. **Clear the rift** by killing every spawned enemy (including a tougher "elite" enemy
   starting tier 3). Doing so pays out experience, magic stones, and reputation, then
   advances the rift tier and returns the player to the village, fully healed.
4. **Repeat**, with the village and the player both a little different than before.

The meta-loop (reputation, economy, class commitment) and the core loop (combat, rift
tiers) are deliberately entangled: choices made in the village change what the next
rift feels like, and rift income changes what's possible back in the village.

---

## The world

- **The Village.** Houses, a notice board, a bonfire (free full heal), a shop (in
  progress - see Current Implementation Status), and several NPCs. Two houses are
  scripted to visibly collapse into rubble the first time a rift tears open nearby,
  including the missing woman's own house - a permanent, visible reminder of the stakes.
- **The Rift Gate.** The portal into the current rift tier. Rifts are procedurally
  populated combat arenas, not persistent places - clearing one closes it and opens the
  next, tougher one.
- **Two factions:**
  - **Villagers** - ordinary people. Not combatants; the value of good standing with
	them is information, trade, and access (e.g. Weller, the stone trader, won't deal
	with a player they consider hostile).
  - **The King's Faction** - the crown's guardians, who train the player's active
    combat skills, but only once trusted. Colder and slower to warm up than the
    villagers.

  Reputation gates NPC dialogue depth (from a full conversation down to a single
  refusal line at Hostile standing), trainer access, and Weller's exchange rate (see
  Economy below).

---

## Character progression

### Levels
Leveling is **uncapped**. XP required to reach a level grows exponentially - each level
costs **double** the XP of the one before it (level 2: 40 XP, level 3: 80 XP, and so on)
- because as a function of accumulated XP, level itself grows logarithmically
(`Level(XP) ≈ log(XP)`); inverting that relationship gives the doubling requirement.
This is a deliberate "harsh law of diminishing returns": climbing gets harder, not
easier, the further the player goes.

- **Level 1** - *King's Grace*: 5 shield points that regenerate over time.
- **Level 2** (40 XP) - +15 reputation, +5 max HP, unlocks *Strikes of Justice*: +50%
  crit chance below 20% HP.
- **Level 3** (80 XP) - +10 reputation, +10 max HP, +5% crit damage, unlocks *Royal
  Order*: -25% active-skill cooldowns.
- **Level 4 and beyond** - no new passives; instead, every level grants a smaller
  generic bump: +8 max HP, +2% melee/ranged damage, +5 reputation. This keeps XP
  meaningful indefinitely without inventing content past the three designed passive
  tiers.

XP earned per kill also scales with rift tier, but on a gentler, **exponential** growth
curve tuned specifically so that a player reaches level 2 shortly after clearing rift
tier 1, and hits level 3 around the midpoint of tier 3 - a deliberate multi-tier pacing
target, not an instant power spike.

### Classes
Committing to a class happens organically: the first time a player learns a
branch-defining active skill from one of three trainer NPCs, that choice locks in
permanently (no respec). Each trainer's dialogue explains the weapon and playstyle
before the player commits.

| Class | Weapon | Fantasy | Strength | Weakness | Resource |
|---|---|---|---|---|---|
| **Warrior** (Melee) | Sword | Heavily armored tank | High damage & defense, can block with a shield | Slow, poor at range/dodging | Shield |
| **Ranger** | Bow | Ranged glass cannon | Highest DPS, best mobility | Weak in melee, low damage resistance | Arrows |
| **Scout** | Daggers & darts | Agile hybrid | Mixes melee and ranged, excellent evasion | Low damage resistance | Darts |

Each class has 3 active skills (basic → improvement → key skill, taught for free by
trainer NPCs, gated by reputation - never purchasable) and 3 branch-specific passive
skills (purchased with magic stones, gated by level and reputation, layered on top of
the 3 universal passive gates above).

### Magic stones - the central resource
Magic stones drop from every enemy kill (1 per kill) and are the currency for the
passive skill tree. A full build (3 universal gates + your class's 3 branch passives)
costs 15 stones total - a target chosen so it's completed around the same point the
player reaches level 3, so both progression ceilings land together as one deliberate
"power spike" instead of triggering independently and too early.

**The core economic choice:** every stone can instead be sold to **Weller**, a village
trader, for money. Selling is irreversible - sold stones can never be bought back. The
price scales with reputation with the Villagers faction (better standing, better rate),
so the player is constantly weighing *"spend this stone on my build, or cash it in?"* -
exactly the tension the game is designed around.

---

## Economy

- **Magic stones** - earned from combat, spent on the passive skill tree or sold to
  Weller for money.
- **Money** - earned only by selling magic stones to Weller. Price per stone starts at
  5 coin and rises 2% for every reputation point above the Villagers' "Neutral"
  threshold (5 coin at minimum tradeable reputation, 7 at Friendly standing, 9 at high
  reputation). Money is intended to eventually purchase weapons and gear from the
  village shop.
- **Reputation** - earned from combat (kills, elite bonus) and leveling; gates NPC
  dialogue depth, trainer access, and Weller's price.

---

## Rifts and enemy scaling

Each rift tier is meaningfully harder than the last, on a **linear** curve: enemy max
HP, contact damage, and move speed all increase by a fixed amount per tier (not a
compounding percentage) - a deliberate choice for a scaling curve that stays
predictable and easy to balance around, rather than exploding at high tiers. Enemy count
per rift also grows by one per tier. Starting at tier 2, hazards like poison clouds and
projectile pressure from ranged enemies are added; starting at tier 3, one enemy per
wave is upgraded to an "elite" variant with a flat stat bonus.

Every class solves the same fight differently - for example, a boss-tier encounter that
spawns adds, drips poison, and fires projectiles: the Warrior tanks hits with a timed
shield block, the Ranger kites from cover, the Scout uses raw mobility and evasion to
stay untouched while whittling the boss down.

---

## Current implementation status

This is a playable vertical slice, not a finished game. What's real and working:

- Full village + rift core loop, with border-walled village navigation.
- Three classes, uncapped leveling, the full passive skill tree, active-skill training.
- Magic stone economy (earn → spend on tree, or sell to Weller for reputation-scaled
  money).
- Reputation-gated dialogue for every NPC, with a class-selection intro shown once per
  playthrough.
- Rift combat with tier-scaled enemies, poison/projectile hazards, and an elite variant.
- A debug cheat panel (press `` ` ``) for fast iteration: complete the current rift
  instantly, grant XP/levels/stones/reputation/money, full heal, and toggle immortality.

What's intentionally still a stub, pending further work:
- **The shop** exists as a station with four empty item slots - no weapons or gear are
  purchasable yet.
- **A single global reputation counter** currently drives both factions' standing at
  once; the intended design (independent per-faction standing, so favoring the crown
  and the village are a real tradeoff) isn't implemented yet.
- **No tier cap or endgame plan** - rift difficulty scales forever; player power growth
  slows down past level 3. Long-term balance depends on either capping difficulty or
  giving the (currently empty) shop a real role in scaling player damage.
