# Private Instances

A private instance is a separate room of your world: the same page and the same content, with its own small group of players. Your world can create one for a player and move players into it, which is how you give a party its own round of a game, a table its own match, or a group its own run of a level.

!> **Only in the app.** In SDK Play Mode every call answers `app_unavailable`. Publish your world to try it.

## What a private instance is

- **Up to 10 people.** A private instance holds 10 players, or your world's capacity if that is lower. Today it is a single room: when it is full, the next player is turned away. That may change, so read `canOverflow` from `GetRoomInfo()` rather than assuming it.
- **Nothing is shared between instances.** A private instance and your world's public instance are different rooms, and so are two private instances. Players in one can't see or hear players in another, and [space state](user-multiplayer.md), synced objects and one-shots stay inside the room they were written in. If your game needs something to carry over, save it somewhere of your own, or have the players bring it with them (see [Bringing players along](#bringing-players-along)).
- **It belongs to the player who created it.** Each player has at most one private instance of a world at a time; creating again returns the one they already have. Its creator can kick people from it and is a space admin inside it (`isSpaceAdmin`), but not in your world's public instance.
- **Your world's rules still apply.** Nobody gets into a private instance of a world they couldn't enter directly, and a player banned from your world is banned from its instances.
- **Owners can switch them off.** Private instances are on by default; a world's owner can turn them off in the world's settings on altvr.app. Then nobody can create one, from the menu or from a script.

Players can also create a private instance themselves, from **Settings > General > Create Private Instance**. What your script creates is the same thing.

## Query strings no longer make separate rooms

`https://my-world.example/?room=a` and `https://my-world.example/?room=b` used to be two different rooms. They are now the same one: everything after the `?` is ignored when the app works out which room a player is in, so every address of your world leads to its one public instance. Your page still receives the query string and can read it.

If your world relied on query strings to split players into separate rooms, use private instances instead.

## Which room am I in?

```js
const scene = BS.Scene.GetInstance();
const room = await scene.GetRoomInfo();

if (room.ok && room.isPrivateInstance) {
    console.log("In private instance", room.key, "of", room.world.name, "which holds", room.maxOccupancy);
}
```

| Field | What it is |
|---|---|
| `ok` | `false` with `error: "app_unavailable"` in SDK Play Mode |
| `roomKind` | `"public"` for your world's public instance, `"private"` for a private instance, `"event"` for an event's instance; `""` before the room is joined |
| `isPrivateInstance`, `isEventInstance` | `true` in a private instance, or in an event's instance |
| `key` | The instance's key; `null` in the public instance. Every player in the instance can read it. It names the instance and is not a secret; the join code is never here |
| `eventsV2Id` | The event's id in an event's instance; `0` otherwise |
| `access` | The instance's access (see below); `null` in the public instance |
| `maxOccupancy` | How many people this room holds; `0` when the app doesn't know |
| `canOverflow` | Whether this room spreads over several overflows when it fills (each holding `maxOccupancy`), as a world's public instance does |
| `canCreate` | Whether this player may create a private instance of your world |
| `isAdmin` | Whether this player is an admin of this room: the creator of a private instance while inside it, and your world's owner and moderators in every room of it (the same as `isSpaceAdmin`) |
| `world` | The world this room belongs to (below); `null` outside a SideQuest world |

`world` is the same in your world's public instance and in every private or event instance of it:

| Field | What it is |
|---|---|
| `id` | The world's id |
| `name`, `description` | As set on the world |
| `imageUrl`, `bannerUrl` | The world's image and banner; `null` when it has none |
| `accessLevel` | Who can enter the world: `1000` is public, anything lower is restricted |
| `isOwner` | Whether this player owns the world. The creator of a private instance is its admin, not the world's owner |

`isAdmin` and `isOwner` come from the player's own app, like `isSpaceAdmin`: fine for deciding what to show, not proof of anything. Use [World Roles](world-roles.md) when it matters.

### When the room is joined

A move between instances reloads your world, so your page always starts in the room it will stay in. The room is joined a moment after the page loads; until then `roomKind` is `""`. Rather than polling, listen for the `host-ext` event the app raises each time the player has joined a room (once after loading, and again after a reconnect). Its `data` has the same fields as `GetRoomInfo()`:

```js
scene.On("host-ext", (e) => {
    const { command, evt, data } = e.detail;
    if (command !== "instances" || evt !== "room") return;

    setUpFor(data.isPrivateInstance ? "match" : "lobby", data);
});
```

## Creating one

```js
const made = await scene.CreatePrivateInstance("friends");
if (made.ok) {
    console.log("Key", made.key, "join code", made.joinCode);
} else {
    console.warn("No private instance:", made.error, made.message);
}
```

Creating moves nobody: it makes the instance (or finds the one the player already has) and answers with its `key` and `joinCode`. The player it is created for is the one whose app runs the call, and they must be signed in.

The argument says who can get in **without** the join code, besides the creator:

| Access | Who gets in without the join code |
|---|---|
| `"private"` (the default) | Nobody |
| `"friends"` | The creator's friends |
| `"friends_plus"` | The creator's friends, and friends of those friends |

Anyone signed in who can enter your world gets in **with** the join code, whatever the access. Guests can't use a join code.

`CreatePrivateInstance` never rejects. When `ok` is false, `error` is one of:

| `error` | Why |
|---|---|
| `app_unavailable` | SDK Play Mode, or an app version without private instances |
| `not_signed_in` | The player is a guest |
| `not_allowed` | The world's owner turned private instances off |
| `rate_limited` | The player created too many in the last hour |
| `not_a_world` | The page isn't running in a SideQuest world, or its room isn't joined yet |
| `loading`, `space_changed` | The world was loading, or the player left before the answer came |
| `bad_request` | The access isn't one of the three above |
| `unavailable`, `failed`, `busy`, `timeout` | It couldn't be done now; `message` says more |

## Joining one

```js
const reply = await scene.JoinPrivateInstance(key, joinCode);
if (!reply.ok) {
    console.log("Still here:", reply.error); // "declined" when the player said no
}
```

**The player is always asked first.** The app shows "Move to a private instance?" and moves the player only if they agree. If they decline, nothing happens and the reply is `{ ok: false, error: "declined" }`; your world then can't ask them again for half a minute (`too_soon`). Ask when the player has done something that shows they want to go, such as pressing your "Start match" button, not on a timer.

When the player agrees, your world reloads for them in the private instance. The page that called `JoinPrivateInstance` ends there, so do anything you need to do before you call it.

- `key` is the instance to join. You can only name instances of the world your page is running in.
- `joinCode` is needed when the instance's access wouldn't admit this player. Its creator never needs one. Leave it out when you have none.

## Leaving one

```js
const reply = await scene.LeavePrivateInstance();
```

This asks the player whether to go back to your world's public instance, with the same question, the same `declined` and `too_soon` answers and the same reload as joining. It answers `not_in_instance` in the public instance. Players can also leave by themselves from **Settings > General > Leave Private Instance**.

There is no call that closes a private instance or removes people from it: it empties as players leave, and its creator can kick people from the People menu.

`JoinPrivateInstance` never rejects. When `ok` is false, `error` is one of:

| `error` | Why |
|---|---|
| `app_unavailable` | SDK Play Mode, or an app version without private instances |
| `declined` | The player chose to stay |
| `too_soon` | The player declined a move from this world in the last 30 seconds |
| `busy` | The player is already being asked about a move |
| `timeout` | The player hadn't answered after about two minutes. The question is taken down, so a late answer can't move them |
| `other_world` | The key is for an instance of another world |
| `invalid_room`, `world_inst_closed`, `world_inst_access_denied`, `not_allowed` | There is no such instance, it has closed, or it wouldn't let this player in |
| `not_a_world`, `loading`, `space_changed`, `bad_request`, `unavailable`, `failed` | As for creating |

## Bringing players along

A private instance starts empty, and nothing in it comes from the room the players left. So one player creates the instance and tells the others where it is, through the room they are all still in. Space state suits this: everyone in the room gets it, including players who arrive a moment later.

```js
const scene = BS.Scene.GetInstance();

// 1. On the host's client, when they press "Start match": create, and share the key and join code
async function startMatch() {
    const made = await scene.CreatePrivateInstance("private");
    if (!made.ok) return console.warn("No match room:", made.error);

    scene.SetPublicSpaceProps({ matchKey: made.key, matchCode: made.joinCode });
    await scene.JoinPrivateInstance(made.key); // the host needs no join code
}

// 2. On every other client: offer the match when it appears, and join when the player takes it
scene.On("space-state-changed", () => {
    const { matchKey, matchCode } = scene.spaceState.public;
    if (matchKey) showJoinButton(() => scene.JoinPrivateInstance(matchKey, matchCode));
});

// 3. In the match room: the page has reloaded, so find out where it is
scene.On("host-ext", (e) => {
    const { command, evt, data } = e.detail;
    if (command === "instances" && evt === "room" && data.isPrivateInstance) startRound(data.key);
});

// 4. When the round is over: back to the public instance (each player is asked)
async function endRound() {
    await scene.LeavePrivateInstance();
}
```

Things to keep in mind:

- **Each player is asked before they move**, so wire `JoinPrivateInstance` to something the player pressed. A player who says no can't be asked again for half a minute.
- **The key names the instance; the join code lets people in.** Anyone in the instance can read the key from `GetRoomInfo()`, and can pass it on. Only the creator's `CreatePrivateInstance` reply carries the join code, so the creator's client is the one that shares it. Without the code, the key gets in only the people the instance's access admits.
- **Public space state is readable by everyone in that room**, and writable by anyone in it. That is what you want for "the match is over here"; it means a join code shared this way is known to everyone in the room, which is what keeps out everyone else. Clear the two values when the match is over so later arrivals aren't offered a finished one. Use protected space state if only your world's admins should be able to start a match.
- **Nothing carries over.** Pass what the new room needs in the same state, or have the first player to arrive write it into the new room's space state.

## The Instance Join component

**Instance Join** (`BSInstanceJoin` in Unity, `BS.InstanceJoin` in a page script; under **BS** in Add Component) does the same as the `scene` calls from a component, for a button wired with a UnityEvent, a graph or a script. It needs a **BS Object Id** on the same object, which Unity adds for you.

| Property | What it is |
|---|---|
| `access` (**Access**) | Who an instance this component creates admits besides the player and the people given its join code: `private` (also when left empty), `friends` or `friends_plus` |
| `instanceKey` (**Instance Key**) | The instance `Join` goes to. `Create` fills it in; set it yourself to join one another player created |
| `joinCode` (**Join Code**) | The join code sent with `Join`, when the player needs one. `Create` fills it in |

| Method | What it does |
|---|---|
| `Create()` | Creates a private instance for the player (or finds the one they have) and fills in `instanceKey` and `joinCode`. Moves nobody |
| `CreateAndJoin()` | `Create`, then `Join` |
| `Join()` | Asks the player to move to `instanceKey` |
| `Leave()` | Asks the player to go back to the world's public instance |

The player is asked before every move, exactly as with `scene.JoinPrivateInstance`: the question is the app's, so a component can't skip it.

```js
const button = await scene.Find("PlayPrivately"); // needs a BS Object Id
const instance = await button.AddComponent(new BS.InstanceJoin({ access: "friends" }));
button.On("click", () => instance.CreateAndJoin());
```

The methods answer nothing themselves. A page hears how each went through the `host-ext` event, with `data.object` the component's object (`gameObject.unityId`):

```js
scene.On("host-ext", (e) => {
    const { command, evt, data } = e.detail;
    if (command !== "instances" || String(data.object) !== String(button.unityId)) return;

    if (evt === "created") share(data.key, data.joinCode);
    if (evt === "failed") console.warn("Not moved:", data.error); // "declined" when the player said no
});
```

| `evt` | `data` |
|---|---|
| `created` | `Create` worked: `key` and `joinCode` |
| `joining` | The player agreed to a `Join` or a `Leave`; the world is about to reload |
| `failed` | `error`, one of the codes in the tables above |

In Unity the same three are the **On Created**, **On Joining** and **On Failed** UnityEvents, and the methods are `_Create()`, `_CreateAndJoin()`, `_Join()` and `_Leave()` (wire a button's UnityEvent to them, or use the nodes of the same names in a graph). `InstanceKey`, `JoinCode` and `Access` can be read and set from C#, and `LastError` read.

Nothing on the component is sent to other players: its properties and events stay on the player's own app. The join code it holds after `Create` is the creator's to share, as in [Bringing players along](#bringing-players-along).

From your own C#, `BS.WorldInstances.GetInfo`, `Create`, `Join` and `Leave` take a callback with the same reply fields (`reply.world` for the world).

## Visual Scripting

| Node | Where | What it does |
|---|---|---|
| Get Room Info | `BS > World` | Outputs `Room Kind`, `Is Private Instance`, `Is Event Instance`, `Instance Key`, `Access`, `Event Id`, `Max Occupancy`, `Can Overflow`, `Can Create` and `Is Admin` |
| Get World Info | `BS > World` | The world this room belongs to. Outputs `Is World`, `World Id`, `Name`, `Description`, `Image Url`, `Banner Url`, `Access Level` and `Is Owner` |
| Create Private Instance | `BS > World` | Creates one with `Access` (`private`, `friends` or `friends_plus`); the answer arrives on On Private Instance Result |
| Join Private Instance | `BS > World` | Asks the player to move to `Key`, with `Join Code` if they need one; the answer arrives on On Private Instance Result |
| Leave Private Instance | `BS > World` | Asks the player to go back to the world's public instance; the answer arrives on On Private Instance Result |
| On Private Instance Result | `Events > BS > World` | A create, join or leave was answered. Outputs `Op` (`create`, `join` or `leave`), `Success`, `Key`, `Join Code` and `Error` |
| On Room Joined | `Events > BS > World` | The player has joined a room (after loading, and after a reconnect). Outputs `Room Kind`, `Is Private Instance`, `Is Event Instance` and `Instance Key` |

`Error` holds the same codes as the tables above, and is empty on success.

The same flow as the JavaScript example: **Create Private Instance**, then on **On Private Instance Result** with `Op` `create` write `Key` and `Join Code` with **Set Space State Property** and run **Join Private Instance**; on the other clients read those two values from space state and run **Join Private Instance** from a button; in the instance, **On Room Joined** with `Is Private Instance` true starts the round; **Leave Private Instance** ends it.
