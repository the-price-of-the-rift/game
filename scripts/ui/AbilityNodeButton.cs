#nullable enable

using Godot;

public partial class AbilityNodeButton : Button
{
	[Export] public string AbilityId { get; set; } = string.Empty;
}
