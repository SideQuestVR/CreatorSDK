# Scene Settings

A space's settings decide what players can do in it (teleport, the grapple, grabbing from a distance), how they move (walking speed, jumping, the grapple's rope), and a few things about the space itself: where players arrive, the refresh rate and the clipping planes. Set them from your page with `scene.SetSettings`, or place a **Scene Settings** object in Unity.

```js
window.addEventListener("bs-loaded", async () => {
    const scene = BS.Scene.GetInstance();

    const settings = new BS.SceneSettings();
    settings.EnableTeleport = false;
    settings.EnableSpiderMan = true;                      // the grapple
    settings.SpawnPoint = new BS.Vector4(0, 10, 0, 90);   // x, y, z, and the Y rotation in degrees
    settings.PhysicsJumpStrength = 1.5;
    await scene.SetSettings(settings);
});
```

## How settings are applied

- **`SetSettings` sends every setting.** Anything you didn't set goes out at its default from the tables below, so a second call that only changes `EnableTeleport` also puts everything else back to the defaults. Keep one `BS.SceneSettings` object, change it, and send it again.
- **Call it early**, as above, before the space finishes loading. `EnableGuests` and `SpawnPoint` are acted on while the space loads.
- **Every space starts from the defaults.** The settings are reset when the player leaves, so nothing carries over to the next space.
- **A [Scene Settings](../components/player-setup.md#settings) object in the scene** (see [Easy Prefabs](../building-in-unity/easy-prefabs.md#scene-settings-bssettings)) sets the same values when it starts, again whenever the page reloads, and whenever one of its properties changes. Whichever runs last wins: a `SetSettings` call after it replaces all of its values, and it replaces yours. Use one or the other, or lock them.
- **Locks.** Once `SettingsLocked` is `true`, nothing can change the general settings (the first table) until the player leaves: later `SetSettings` calls and Scene Settings objects are ignored for them. `PhysicsSettingsLocked` does the same for the physics settings. The call that sets a lock still applies its own values first, and a lock can't be undone. A Scene Settings object that set a lock can still change its own values.
- **The megaphone lock.** `MegaphoneLocked` works on its own: `true` keeps the megaphone to moderators until the player leaves or rejoins, and nothing can unlock it before then (`false` is ignored). `SettingsLocked` doesn't stop a page setting it. See [Megaphone](#megaphone).
- **In SDK Play Mode** only `ClippingPlane` has an effect. Everything else needs the app.

## General Settings

| Setting | Type | Default | What it does |
|---------|------|---------|--------------|
| `EnableTeleport` | boolean | true | Players can teleport themselves. Teleports you make (`TeleportTo`, spawns, BS Teleporters) work either way. |
| `EnableSpiderMan` | boolean | false | Players can use the grapple: fire a rope with the trigger (Q or E on desktop) and reel in with grip. |
| `EnableBirdFlight` | boolean | true | Space permission for bird flight with tracked controllers. Missing values allow flight; explicitly set `false` to forbid it. The player's **Controls > Bird Flight** preference must also be enabled. Draw both hands forward from beside the shoulders while holding both triggers to deploy wings, then release the triggers and flap or glide. In A-Frame, use `sq-enablebirdflight="false"` or `sq-disablebirdflight` to forbid flight. |
| `AvatarSyncMode` | string | "social" | How often players far from each other see each other move. `"social"`: full rate within 15 m, thinning to once a second past 70 m, which keeps big gatherings cheap. `"action"`: full rate within 10 m, then 24, 15 and 8 updates a second out to 150 m, and 2 past that, for games and flight where distant players must move smoothly; it costs more bandwidth per distant player. Any other value acts as `"social"`. |
| `VoiceDistanceMultiplier` | number | 1 | Scales how far voices carry in this space, on top of each player's own Voice Distance setting (default 30 m, 1-50 m): 2 = twice as far, 0.5 = half. The app clamps it to 0.1-10. The fade applies at once, and how far away voices are received at all follows within about a second. A large multiplier in a crowded space costs players bandwidth. |
| `MegaphoneDistanceMultiplier` | number | 0 | Scales how far megaphones and friends' walkie-talkies carry in this space, apart from normal voices: they're at full volume to 40 m and silent at 100 m, times this. 10 = silent at 1 km, 25 = full volume to 1 km, while normal voices stay as `VoiceDistanceMultiplier` sets them. 0 (the default) uses `VoiceDistanceMultiplier` for them too, as before. The app clamps it to 0.1-100, and a megaphone never goes quiet nearer than a normal voice would. They're received at any distance anyway, so a large value costs players no bandwidth. |
| `EnableForceGrab` | boolean | true | Players can grab objects from a distance. On unless you set `false` (it was off unless set until November 2026). Each player can also switch their own distance grab off in **Controls > Distance Grab**. A-Frame pages: `sq-disableforcegrab` on the scene. |
| `EnableRun` | boolean | true | Players can run: hold Shift on desktop, or click the left stick while moving in VR (it lasts until the stick is let go). Running is 1.4 times `PhysicsMoveSpeed`. Set `false` to keep everyone at walking speed. A-Frame pages: `sq-disablerun` on the scene. |
| `TeleportMaxRange` | number | 0 | The furthest players can teleport in this space, in metres along the ground. `0` sets no limit: each player's own **Controls > Teleport Range** applies (8 m unless they change it, 1 to 100 m). Above `0` it caps every player's range at that distance; it never lengthens one. The teleport arc is thrown slower for a shorter range and stops at the limit; a straight teleport line reaches the same distance along the ground. A-Frame pages: `sq-teleportmaxrange="3"` on the scene. |
| `EnableSafetyNet` | boolean | true | The fall safety net: a player who falls below `SafetyNetHeight` is put back at their spawn point (or wherever the world last moved them). Set `false` to let players fall as far as the world goes (for a bottomless pit, a long drop or a world far below the origin). A player flung right out of the world (100 km or more away, or broken physics) is still brought back. Works whether or not teleport is on. A-Frame pages: `sq-disablesafetynet` on the scene. |
| `SafetyNetHeight` | number | -250 | The height (world Y, in metres) below which the safety net catches a falling player. Keep it below everywhere players can stand or spawn: a player put back below it is caught again at once. Objects synced across the network keep their own net at -250. A-Frame pages: `sq-safetynetheight="-50"` on the scene. |
| `EnableBrowserStreaming` | boolean | true | Players can stream their menu browser into the space for everyone to see and hear. Set `false` to forbid it: the stream controls are hidden, a running stream stops, and streams shared by other players are not shown. Browsers you place in the space yourself are not affected. A-Frame pages: `sq-disablebrowserstreaming` on the scene. |
| `EnableUserUICollider` | boolean | true | Pointers land on other players, and clicking one opens their profile. Set `false` so pointers pass through players, for a world that aims at them itself (a gun, a game of tag): no laser or hover on them, and their profiles open only from the People list. A-Frame pages: `sq-disableuseruicollider` on the scene. |
| `EnableNametags` | boolean | true | The name tags over other players. `false` hides them, whatever each player's own Nametags option says. |
| `EnableAvatars` | boolean | true | Other players' avatars. `false` shows everyone else as a plain placeholder body, and their avatars aren't downloaded. Your own avatar isn't affected. |
| `EnableGuests` | boolean | true | `false` turns away players who aren't signed in: their load is stopped with a message asking them to sign in. Signed-in players are never turned away. See the note below. |
| `RefreshRate` | number | 72 | The display refresh rate, on headsets that let apps change it (Quest). The headset's highest supported rate at or below this is used; elsewhere it's ignored. |
| `ClippingPlane` | Vector2 | (0.02, 1500) | The player's camera near (`x`) and far (`y`) clipping planes, in metres. |
| `SpawnPoint` | Vector4 | (0, 0, 0, 1) | Where players arrive: `x`, `y`, `z`, and in `w` the Y rotation in degrees. They're placed here as the loading screen opens, and the menu's **Respawn** brings them back. The default (0, 0, 0, 1) means "none": then a [Spawn](../components/player-setup.md#spawn) in the scene picks the spot, or players start at the origin. A spawn point you set wins over Spawn objects. |
| `EnableMegaphone` | boolean | false | Anyone in the space can turn their own megaphone on from the radial menu, so they're heard across the room. Off by default: only moderators can megaphone people. [Megaphone Volumes](../components/special.md#megaphonevolume) work either way. A-Frame pages: `sq-enablemegaphone` on the scene. |
| `SettingsLocked` | boolean | false | Locks the settings in this table (not `MegaphoneLocked`). See [Locks](#how-settings-are-applied). |
| `MegaphoneLocked` | boolean | false | Locks the megaphone: only moderators can megaphone people, and Megaphone Volumes and `EnableMegaphone` stop working. Players can still turn their own megaphone off. Once `true` it stays on until the player leaves or rejoins the space: nothing can unlock it before then. A-Frame pages: `sq-megaphonelocked` on the scene. |

`EnableGuests` is a check in the app, made as your space loads. To really keep guests out, use your world's settings on altvr.app: **Edit world > Who is turned away > Guests**. That stops guests at the door, before they get in.

## Megaphone

A megaphoned player is heard across the room. Who can turn it on:

- **Moderators** can megaphone anyone in the world, in any instance, always. A moderator turning someone's megaphone off turns it off now, a Megaphone Volume's included; where the space lets them (`EnableMegaphone`, or standing in a volume, and not locked) they can turn their own on again.
- **Anyone, for themselves**, from the radial menu, when `EnableMegaphone` is `true`: on and off as they like.
- **Anyone standing in a [Megaphone Volume](../components/special.md#megaphonevolume)** (a stage, a podium), whatever `EnableMegaphone` says.

`MegaphoneLocked` turns the last two off: only moderators are left. Players can always turn their own megaphone off.

- **What it sounds like**: mostly flat, so the whole room hears it, but a quarter of it still comes from where the player stands. It is at full volume to 40 m and fades out by 100 m, both scaled by `MegaphoneDistanceMultiplier`, or by `VoiceDistanceMultiplier` when the space doesn't set that. A large world can let megaphones carry far while normal conversations stay local.
- **It ends when the player leaves or rejoins.** Nobody arrives megaphoned.
- **Walkie-talkies carry the same way.** A player can turn on walkie-talkie for a friend in the same instance (from the friend's profile): that friend then hears them like a megaphone, and nobody else hears any difference. The world can't turn it on or off; `MegaphoneDistanceMultiplier` sets its range too.
- **It is not the mirror.** A moderator can also *mirror* someone: show them, and let them be heard, in every instance of the world. A mirrored player who is also megaphoned is megaphoned in every instance they're mirrored into.

## Physics Settings

How the local player moves. Each player's app applies them to that player.

| Setting | Type | Default | What it does |
|---------|------|---------|--------------|
| `PhysicsMoveSpeed` | number | 3.25 | Walking speed, in metres per second. |
| `PhysicsMoveAcceleration` | number | 4.6 | How hard players push off to reach walking speed. It's also what holds them on slopes and carries them up steps, so values below 2.2 are raised to 2.2. |
| `PhysicsAirControlSpeed` | number | 2.4 | How fast players can steer themselves in the air, in metres per second. Moving faster than this, steering can still turn or slow them, but not speed them up. |
| `PhysicsAirControlAcceleration` | number | 6 | How quickly they steer in the air, in metres per second per second. |
| `PhysicsDrag` | number | 0 | Air resistance on the player's body. It slows walking a little too. |
| `PhysicsJumpStrength` | number | 1 | Multiplies the jump: 2 jumps harder, 0.5 jumps lower. How firmly players stand doesn't change. To turn jumping off, use `SetCanJump(false)`. |
| `PhysicsHandPositionStrength` | number | 1 | Multiplies how strongly the hands are pulled to the controllers: lower is floppier, higher is stiffer. |
| `PhysicsHandRotationStrength` | number | 1 | The same for the hands' rotation. |
| `PhysicsHandSpringiness` | number | 10 | Higher makes the hands bouncier, lower makes them settle sooner. |
| `PhysicsGrappleRange` | number | 512 | How far the grapple reaches, in metres. |
| `PhysicsGrappleReelSpeed` | number | 1 | Multiplies how fast holding grip reels the grapple in. |
| `PhysicsGrappleSpringiness` | number | 10 | Higher makes the grapple's rope bouncier. It applies from the next rope fired. |
| `PhysicsGorillaMode` | boolean | false | VR only: players get about by pushing their hands against the floor and walls, Gorilla Tag style. Their hands collide with the world, the sticks no longer walk or steer them in the air (turning still works), and a push carries them a short way before they stop. Jumping and teleporting still work unless you turn them off. Desktop and mobile players move as usual. |
| `PhysicsSettingsLocked` | boolean | false | Locks the settings in this table. See [Locks](#how-settings-are-applied). |

## Visual Scripting acceleration

The app can run Visual Scripting graphs through faster paths. Each player chooses whether that is on by default, and can change it for the world they are in. Your space can ask for it on or off; a player's own choice for your world still wins over your request.

```js
await scene.SetVisualScriptingAcceleration("on");   // "on", "off", or "default" for no preference
```

- **It is a request for the whole set.** There is no per-feature request.
- **Ask early**, before your graphs start. Some of the switches only take effect the next time the world loads, so a request made late may not fully apply until then.
- **It isn't one of the settings `SetSettings` resets.** `new BS.SceneSettings()` has no `VisualScriptingAcceleration`, so sending your other settings leaves the request alone, and `SettingsLocked` doesn't block it. The last request made wins. It goes back to no preference when the player leaves.
- **Older apps** have no `scene.SetVisualScriptingAcceleration`. This form works there too (an app that doesn't know the setting ignores it), and sends nothing else:

```js
const request = new BS.SceneSettings();
for (const key of Object.keys(request)) delete request[key];
request.VisualScriptingAcceleration = { Serialize: () => "on" };
await scene.SetSettings(request);
```

Don't set `VisualScriptingAcceleration` to a plain string on an older app: it sends it as a yes/no, so `"off"` would arrive as on.

## Player count

How many players share an instance of your world isn't a page setting: `MaxOccupancy` does nothing. The server decides it when someone joins, from:

1. Your world's **Capacity** on altvr.app (**Edit world**). This wins when it's set.
2. Otherwise, a tag in your page's `<head>`: `<meta name="sq-maxoccupancy" content="8">`, or `<meta name="sq-singleuser">` for one player per instance. The server reads it from your published page and remembers it for about an hour, so a change can take that long to apply.
3. Otherwise, 20.

## Not working yet

`BS.SceneSettings` has these, and `SetSettings` sends them, but the app doesn't act on them yet. They're planned:

- `EnableHandHold`: players will be able to hold other players' hands.
- `EnableRadar`: a marker over other players, so they can be found through walls.
- `EnablePortals`: players will be able to drop portals to other spaces from the menu. Portals you place already work.
- `EnableDevTools`: whether players can open your page's developer tools in the app.
- `PhysicsFreeFallAngularDrag`: how quickly a tumbling player's spin slows. The app applies it, but players don't tumble yet.
- `EnableQuaternionPose`, `EnableControllerExtras`: extra pose data for A-Frame pages.

## Scene Physics Methods

```js
await scene.Gravity(new BS.Vector3(0, -9.8, 0));   // the default
await scene.TimeScale(0.5);                         // half speed; 1 is normal

// Cast a ray: origin, direction, distance, and an optional layer mask (all layers by default)
const hit = await scene.Raycast(new BS.Vector3(0, 1, 0), new BS.Vector3(0, -1, 0), 100);
if (hit) {
    const [, id, px, py, pz, nx, ny, nz] = hit.split("¶");
    const point = new BS.Vector3(Number(px), Number(py), Number(pz));
    const normal = new BS.Vector3(Number(nx), Number(ny), Number(nz));
    const object = scene.objects[id];   // the GameObject that was hit, if your page knows it
}
```

Gravity and the time scale go back to normal, (0, -9.8, 0) and 1, when the player leaves the space.

`Raycast` resolves with `null` when the ray hits nothing. On a hit it resolves with a string of `¶`-separated values: a command code, the id of the object hit, then the hit point's x, y, z and the surface normal's x, y, z.

## Player Control Methods

To make the player drop something they're holding, see [Letting Go of Held Objects](scene-api.md#letting-go-of-held-objects).

Turn the local player's abilities off and on:

```js
await scene.SetCanMove(false);   // pass true to allow it again
```

| Method | With `false`, players can't | In SDK Play Mode |
|---|---|---|
| `SetCanMove` | Move with the stick (WASD on desktop) | Yes |
| `SetCanRotate` | Turn with the stick, or with the mouse on desktop. Desktop players can still look up to 80° either side, as a VR player can turn their head | Yes |
| `SetCanCrouch` | Crouch with the stick (Ctrl or Shift on desktop) | No |
| `SetCanTeleport` | Teleport | No |
| `SetCanGrapple` | Use the grapple | No |
| `SetCanJump` | Jump | No |
| `SetCanGrab` | Grab things | Yes |

They stay as you set them, so switch them back on when your space no longer needs them.

```js
// Teleport the local player: position, Y rotation in degrees, stop their movement
await scene.TeleportTo(new BS.Vector3(0, 5, 0), 90, true);

// Launch them upwards at 5 metres per second
await scene.AddPlayerForce(new BS.Vector3(0, 5, 0), BS.ForceMode.VelocityChange);

// Buzz the left controller: strength 0 to 1, for 0.1 seconds
await scene.SendHapticImpulse(0.5, 0.1, BS.HandSide.LEFT);
```

- **`TeleportTo`** works in the app and in SDK Play Mode. It takes a fourth argument, `isSpawn`, which marks the teleport as a respawn for the `player-teleported` event. In the app, if your space has no `SpawnPoint`, the first `TeleportTo` while it's still loading becomes its spawn point.
- **`AddPlayerForce`** only moves players in the app. With `VelocityChange` or `Acceleration` every part of the player gets the same push. `Impulse` and `Force` are shared out over the whole player by weight, 76 kg in all, so they need much bigger values.
- **`SendHapticImpulse`** buzzes a VR controller. Nothing happens on desktop or in SDK Play Mode.

### Teleport Event

Your page hears every time the local player is teleported, once they have moved: by `TeleportTo`, a portal or BS Teleporter, a seat, arriving, respawning from the menu, or the app catching a fall out of the world. It fires in the app and in SDK Play Mode.

```js
scene.On("player-teleported", (e) => {
    if (e.detail.isSpawn) resetGame(); // arrived, respawned, or caught falling
    console.log(e.detail.position, e.detail.rotation);
});
```

| `detail` | |
|---|---|
| `position` | Where they landed, a `BS.Vector3`. |
| `rotation` | Which way they face, Euler degrees as a `BS.Vector3`; only `y` is used. |
| `isSpawn` | True when they arrived, respawned from the menu or were caught falling out of the world, and for a `TeleportTo` with `isSpawn`. |

## Input Blocking & Controller Events

Stop the app using a control, so you can use it yourself:

```js
await scene.SetBlockLeftThumbstick(true);   // false to unblock
```

| Method | What it stops in the app, on VR controllers |
|---|---|
| `SetBlockLeftThumbstick` | Moving (on desktop too, and in SDK Play Mode) |
| `SetBlockRightThumbstick` | Turning and crouching with the right stick |
| `SetBlockRightPrimary` | Jumping (the A button) |
| `SetBlockRightSecondary` | Teleporting (the B button) |
| `SetBlockLeftTrigger`, `SetBlockRightTrigger` | That hand's trigger, for held objects, and its grapple (on desktop too) |
| `SetBlockLeftPrimary` | Nothing (on a gamepad, teleporting) |
| `SetBlockLeftSecondary`, `SetBlockLeftThumbstickClick`, `SetBlockRightThumbstickClick` | Nothing yet |

Like the abilities above, blocks stay until you lift them. Blocking a control only stops the app using it: your page still gets the events below.

### Controller Input Events

The app sends your page the local player's VR controller input. They don't fire on desktop or in SDK Play Mode.

```js
// A button went down or up
scene.On("button-pressed", (e) => {
    console.log(e.detail.button, e.detail.side);
});
scene.On("button-released", (e) => {
    console.log(e.detail.button, e.detail.side);
});

// A thumbstick moved
scene.On("controller-axis-update", (e) => {
    console.log(e.detail.hand, e.detail.x, e.detail.y);
});

// A trigger moved
scene.On("trigger-axis-update", (e) => {
    console.log(e.detail.hand, e.detail.value);
});
```

| Event | `detail` |
|---|---|
| `button-pressed`, `button-released` | `button`: a `BS.ButtonType` (`TRIGGER`, `GRIP`, `PRIMARY` (A or X), `SECONDARY` (B or Y), `THUMBSTICKCLICK`). `side`: a `BS.HandSide` (`LEFT`, `RIGHT`). |
| `controller-axis-update` | `hand`: a `BS.HandSide`. `x` and `y`: the thumbstick, -1 to 1 (left/right, down/up). Sent when the stick moves by more than 0.01. |
| `trigger-axis-update` | `hand`: a `BS.HandSide`. `value`: how far the trigger is pressed, 0 to 1. Sent when it changes by more than 0.01. |

### Pointer Over UI

Unlike the controller events, this one fires on desktop and in SDK Play Mode too. It tells your page when one of the local player's pointers moves onto UI (a canvas or UI Toolkit panel, the app's or your space's, the radial menu included) or off it. UI doesn't stop your own input, so use it to pause a tool that reads the trigger or mouse while the player uses a panel.

```js
scene.On("pointer-over-ui", (e) => {
    console.log(e.detail.isOverUI, e.detail.side);
});
```

| `detail` | |
|---|---|
| `isOverUI` | True when the pointer moved onto UI, false when it left. |
| `side` | A `BS.HandSide`. The mouse is `RIGHT`. |
