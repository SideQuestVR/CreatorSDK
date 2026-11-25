using Unity.VisualScripting;
using BS;

namespace BS.VisualScripting
{
    /// <summary>
    /// The world this room belongs to: the same answer in the world's public instance and in every private or event
    /// instance of it.
    /// </summary>
    /// <remarks>
    /// <c>Is World</c> is false outside a SideQuest world, in SDK Play Mode and before the room is joined; the other
    /// outputs are then empty. <c>World Id</c> is the id as text. <c>Access Level</c> is 1000 for a public world and
    /// lower for a restricted one. <c>Is Owner</c> says whether the local player owns the world, which is not the same
    /// as having created the private instance they are in (<see cref="GetRoomInfo"/>'s <c>Is Admin</c>).
    ///
    /// Port keys are serialized into saved graphs: never rename them.
    /// </remarks>
    [UnitTitle("Get World Info")]
    [UnitShortTitle("Get World Info")]
    [UnitCategory("BS\\World")]
    [TypeIcon(typeof(BSObjectId))]
    public class GetWorldInfo : Unit
    {
        [DoNotSerialize] public ControlInput inputTrigger;
        [DoNotSerialize] public ControlOutput outputTrigger;

        [DoNotSerialize] public ValueOutput isWorld;
        [DoNotSerialize] public ValueOutput worldId;
        [DoNotSerialize] public ValueOutput worldName;
        [DoNotSerialize] public ValueOutput description;
        [DoNotSerialize] public ValueOutput imageUrl;
        [DoNotSerialize] public ValueOutput bannerUrl;
        [DoNotSerialize] public ValueOutput accessLevel;
        [DoNotSerialize] public ValueOutput isOwner;

        protected override void Definition()
        {
            inputTrigger = ControlInput("", flow =>
            {
                // The app answers info at once, so the reply is here before the flow goes on.
                WorldInstanceReply reply = null;
                WorldInstances.GetInfo(r => reply = r);
                var world = reply != null && reply.ok ? reply.world : null;
                flow.SetValue(isWorld, world != null);
                flow.SetValue(worldId, world != null ? world.id.ToString(System.Globalization.CultureInfo.InvariantCulture) : "");
                flow.SetValue(worldName, world?.name ?? "");
                flow.SetValue(description, world?.description ?? "");
                flow.SetValue(imageUrl, world?.imageUrl ?? "");
                flow.SetValue(bannerUrl, world?.bannerUrl ?? "");
                flow.SetValue(accessLevel, world != null ? world.accessLevel : 0);
                flow.SetValue(isOwner, world != null && world.isOwner);
                return outputTrigger;
            });
            outputTrigger = ControlOutput("");

            isWorld = ValueOutput<bool>("Is World");
            worldId = ValueOutput<string>("World Id");
            worldName = ValueOutput<string>("Name");
            description = ValueOutput<string>("Description");
            imageUrl = ValueOutput<string>("Image Url");
            bannerUrl = ValueOutput<string>("Banner Url");
            accessLevel = ValueOutput<int>("Access Level");
            isOwner = ValueOutput<bool>("Is Owner");

            Succession(inputTrigger, outputTrigger);
            Assignment(inputTrigger, isWorld);
            Assignment(inputTrigger, worldId);
            Assignment(inputTrigger, worldName);
            Assignment(inputTrigger, description);
            Assignment(inputTrigger, imageUrl);
            Assignment(inputTrigger, bannerUrl);
            Assignment(inputTrigger, accessLevel);
            Assignment(inputTrigger, isOwner);
        }
    }
}
