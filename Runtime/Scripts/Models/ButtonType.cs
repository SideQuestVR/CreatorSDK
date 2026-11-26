public enum ButtonType
{
    TRIGGER,
    GRIP,
    PRIMARY,
    SECONDARY,
    THUMBSTICKCLICK,
    /// <summary>The hand's grab, from any device (a controller's grip, a hand-tracked grab, a gamepad shoulder, a
    /// desktop or touch grab): the player's grab action, not a button. Pressed past half way.</summary>
    GRAB_ACTION,
    /// <summary>The hand's trigger, from any device (a controller's trigger, a hand-tracked pinch, a gamepad
    /// trigger, a desktop or touch trigger): the player's trigger action, not a button. Pressed past half way.</summary>
    TRIGGER_ACTION,
}