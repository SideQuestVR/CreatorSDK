using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BS
{
    /// <summary>
    /// Private instances of the world this script runs in: separate rooms of the same world, for up to 10 people,
    /// that share nothing with the world's public instance or with each other (no players, voice, space state or
    /// synced objects).
    ///
    /// <para>A world can <see cref="Create"/> one for the local player, which answers with its <b>key</b> and
    /// <b>join code</b>; hand those to the other players (space state, a one-shot) and each client calls
    /// <see cref="Join"/>. A script can only create and join instances of its own world, and the app asks the player
    /// before it moves them: if they say no, nothing happens.</para>
    ///
    /// <para>The app provides this through the "instances" host extension (<see cref="BSHostExtensions"/>), which
    /// page scripts reach as <c>scene.CreatePrivateInstance</c>, <c>scene.JoinPrivateInstance</c> and
    /// <c>scene.GetRoomInfo</c>. In SDK Play Mode, and in a build without it, every call answers
    /// <c>app_unavailable</c>. Replies arrive on the main thread.</para>
    /// </summary>
    public static class WorldInstances
    {
        /// <summary>The host extension's name. A contract with the app and the page API: never changes.</summary>
        public const string Extension = "instances";

        /// <summary>Who a private instance admits besides its creator and the people given its join code.</summary>
        public const string AccessPrivate = "private";
        public const string AccessFriends = "friends";
        public const string AccessFriendsPlus = "friends_plus";

        /// <summary>
        /// The extension's event (<see cref="BSHostExtensions.Emitted"/>, and a page's <c>host-ext</c> event): raised
        /// each time this client has joined a room, with <see cref="GetInfo"/>'s fields as JSON.
        /// </summary>
        public const string RoomEvent = "room";

        /// <summary><see cref="WorldInstanceReply.roomKind"/> values.</summary>
        public const string RoomKindPublic = "public";
        public const string RoomKindPrivate = "private";
        public const string RoomKindEvent = "event";

        /// <summary>
        /// What kind of room this client is in (<see cref="WorldInstanceReply.roomKind"/>), how many people it holds,
        /// and whether the player may create a private instance of this world. Answers at once.
        /// </summary>
        public static void GetInfo(Action<WorldInstanceReply> onReply)
            => Send("info", new JObject(), onReply);

        /// <summary>
        /// Creates a private instance of this world for the local player (or returns the one they already have) and
        /// answers with its <see cref="WorldInstanceReply.key"/> and <see cref="WorldInstanceReply.joinCode"/>. It
        /// does not move anyone: call <see cref="Join"/> for that. <paramref name="access"/> is
        /// <see cref="AccessPrivate"/> (the default), <see cref="AccessFriends"/> or <see cref="AccessFriendsPlus"/>.
        /// Refused for guests (<c>not_signed_in</c>) and where the world's owner turned private instances off
        /// (<c>not_allowed</c>).
        /// </summary>
        public static void Create(string access, Action<WorldInstanceReply> onReply)
            => Send("create", new JObject { ["access"] = string.IsNullOrEmpty(access) ? AccessPrivate : access }, onReply);

        /// <summary>
        /// Asks the player whether to move to the private instance <paramref name="key"/> of this world, and moves
        /// them if they agree (the world reloads there). <paramref name="joinCode"/> lets in someone the instance's
        /// access would not admit; its creator needs none. Answers <c>declined</c> when the player says no.
        /// </summary>
        public static void Join(string key, string joinCode, Action<WorldInstanceReply> onReply)
        {
            var body = new JObject { ["key"] = key ?? "" };
            if (!string.IsNullOrEmpty(joinCode)) body["joinCode"] = joinCode;
            Send("join", body, onReply);
        }

        /// <summary>
        /// Asks the player whether to go back to this world's normal room from the private (or event) instance they
        /// are in, and moves them if they agree (the world reloads there). The same question and the same
        /// <c>declined</c> / <c>too_soon</c> / <c>timeout</c> answers as <see cref="Join"/>; <c>not_in_instance</c>
        /// when they are in the normal room already.
        /// </summary>
        public static void Leave(Action<WorldInstanceReply> onReply)
            => Send("leave", new JObject(), onReply);

        private static void Send(string op, JObject body, Action<WorldInstanceReply> onReply)
        {
            BSHostExtensions.Request(Extension, op, body.ToString(Formatting.None), json =>
            {
                if (onReply == null) return;
                WorldInstanceReply reply = null;
                try { reply = JsonConvert.DeserializeObject<WorldInstanceReply>(json); }
                catch (JsonException) { }
                onReply(reply ?? new WorldInstanceReply { ok = false, error = "internal_error" });
            });
        }
    }

    /// <summary>The "instances" host extension's reply. Field names are the wire names: never rename them.</summary>
    public class WorldInstanceReply
    {
        public bool ok;

        /// <summary>
        /// When <see cref="ok"/> is false: <c>app_unavailable</c> (SDK Play Mode, or an app without private instances),
        /// <c>unavailable</c>, <c>loading</c>, <c>not_a_world</c>, <c>not_signed_in</c>, <c>not_allowed</c>,
        /// <c>rate_limited</c>, <c>bad_request</c>, <c>other_world</c>, <c>not_in_instance</c>, <c>busy</c>, <c>too_soon</c>,
        /// <c>declined</c>, <c>timeout</c>, or the server's own reason for refusing a join.
        /// </summary>
        public string error;

        /// <summary>A sentence about <see cref="error"/> for the log; not for showing to players.</summary>
        public string message;

        /// <summary>"public" (the world's public instance), "private" (a private instance) or "event".</summary>
        public string roomKind;

        /// <summary>The private instance's key: of the one created, the one joined, or the one this client is in.</summary>
        public string key;

        /// <summary>From <see cref="WorldInstances.Create"/> only: the code that lets other players into it.</summary>
        public string joinCode;

        /// <summary>The instance's access: "private", "friends" or "friends_plus".</summary>
        public string access;

        /// <summary>How many people this room holds; 0 when the app does not know.</summary>
        public int maxOccupancy;

        /// <summary>Whether the player may create a private instance of this world.</summary>
        public bool canCreate;

        /// <summary>In a private instance (<see cref="roomKind"/> "private").</summary>
        public bool isPrivateInstance;

        /// <summary>In an event's own instance (<see cref="roomKind"/> "event").</summary>
        public bool isEventInstance;

        /// <summary>The event an event instance belongs to; 0 otherwise.</summary>
        public long eventsV2Id;

        /// <summary>Whether this room spreads over several overflows when it fills, as a world's public instance does.</summary>
        public bool canOverflow;

        /// <summary>
        /// Whether the local player is an admin of this room: the creator of a private instance inside it, and the
        /// world's owner and moderators everywhere. Read from the player's own app, so fine for what to show and
        /// not proof of anything.
        /// </summary>
        public bool isAdmin;

        /// <summary>The world this room belongs to, the same in every room of it; null outside a SideQuest world.</summary>
        public WorldInstanceWorld world;
    }

    /// <summary>The world a room belongs to, as <see cref="WorldInstances.GetInfo"/> reports it. Wire names: never rename them.</summary>
    public class WorldInstanceWorld
    {
        public long id;
        public string name;
        public string description;
        public string imageUrl;
        public string bannerUrl;

        /// <summary>Who can enter the world: 1000 is public; anything lower is restricted (100 private, 200 friends...).</summary>
        public int accessLevel;

        /// <summary>Whether the local player owns the world (not the same as creating a private instance of it).</summary>
        public bool isOwner;
    }
}
