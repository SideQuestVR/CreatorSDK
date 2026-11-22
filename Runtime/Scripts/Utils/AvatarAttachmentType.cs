namespace BS
{
    public enum AvatarAttachmentType
    {
        AttachToAvatar,
        AvatarAttachTo,
        /// <summary>
        /// A moving platform the local player rides while inside its trigger colliders (a deck, a lift, a train car):
        /// nothing holds them on (friction does), but other players draw them relative to the platform instead of
        /// trailing behind it. See BSAttachedObject.
        /// </summary>
        RideZone,
    }
}