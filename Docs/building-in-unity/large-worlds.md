# Large Worlds (BSLargeWorld)

Unity stores positions as 32-bit floats, which get coarser the further you go from (0,0,0): at 10 km from zero a position can only move in steps of about a millimetre, and physics, animation and cameras start to shake. A large world avoids that by keeping the players near Unity's zero however far they travel, and moving the world around them instead.

Add **Add Component > BS > Large World** to one GameObject in your world (or `BS.LargeWorld` from a page script). It gives your world a floating origin and the tools to work with it from C#, page JavaScript and Visual Scripting.

## The Origin

Every player's app keeps its own **origin**: the global position, in metres, of Unity's (0,0,0). A **global position** is the origin plus a Unity position, kept in doubles (about 15 digits), so it stays exact to well under a millimetre even millions of kilometres out.

```
global position = origin + Unity position
```

Each player has their own origin, near wherever they are. Two players far apart see the same world, each with their own part of it near their Unity zero. Outside large worlds the origin is zero and nothing changes.

## Who Moves What

When the origin **shifts** by a vector `delta` (always "move everything by `delta`", so the origin itself moves by `-delta`), it happens at the start of a physics step:

1. **The app** moves the local player by `delta`, and what follows them (the hand camera, floating panels).
2. **The app** places other players and synced objects for the new origin. It sends and receives their positions as global positions, so everyone sees everyone in the right place whatever their origins.
3. **Your world** moves its own content by `delta`, in the `origin-shifted` hook, in that same physics step.

The app never moves your world's content. Only your world knows what it holds (a terrain made of tiles, procedurally generated chunks, a handful of roots), so it moves it itself. Moved in the hook, before physics runs, Rigidbodies and interpolated objects stay smooth and the shift is invisible.

Setting the origin outright (`SetOrigin`) moves nothing: it changes the meaning of every Unity position. Use it before anyone sees your content, for example to start a world at a chosen global position.

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `autoShift` | bool | `false` | Shift the origin by itself whenever the local player is more than `shiftThreshold` from zero on X or Z. |
| `shiftThreshold` | float | `128` | How far from zero (metres, 32 to 1024) the player may go before `autoShift` shifts the origin. |

With `autoShift` on, the component asks for a shift that brings the player back near zero, in whole 64 m steps, and never has more than one shift waiting. Leave it off to decide yourself when to shift (`ShiftOrigin`, `ShiftOriginToPlayer`), for example only while the player is not near something delicate.

Vertical position is never shifted automatically. A world that goes kilometres up or down can ask for a vertical shift with `ShiftOrigin`.

## Methods

Global positions cross to pages and graphs as text, `"x,y,z"` (invariant culture: `.` decimals, every digit of a double), so nothing is rounded to a float on the way.

| Method | Returns | Description |
|--------|---------|-------------|
| `ShiftOrigin(delta)` | bool | Ask for the player and the world to move by `delta` (a Vector3) at the next physics step. Requests before that step add up. False when refused (not finite, or over 10,000 km on an axis). |
| `ShiftOriginToPlayer()` | Vector3 | Shift so the player is back within 32 m of zero on X and Z. Returns the delta asked for (zero when none was needed). |
| `SuggestShift()` | Vector3 | The shift `autoShift` would ask for now, or zero. Asks for nothing. |
| `SetOrigin(globalXyz)` | bool | Set the origin outright, `"x,y,z"`. Nothing moves. False when the text is not three numbers. |
| `GetOrigin()` | string | The origin, `"x,y,z"`. |
| `GetPlayerGlobalPosition()` | string | The local player's (head's) global position, `"x,y,z"`. |
| `LocalToGlobal(local)` | string | The global position of a Unity position. |
| `GlobalToLocal(globalXyz)` | Vector3 | The Unity position of a global position. |

## Moving Your Content

### C#

`BS.WorldOrigin` is the origin itself. Subscribe to `WorldOrigin.Shifted` and move your roots by the delta:

```csharp
using BS;
using UnityEngine;

public class MoveWithOrigin : MonoBehaviour
{
    [SerializeField] Transform[] roots; // the top of each piece of your world

    void OnEnable() => WorldOrigin.Shifted += Move;
    void OnDisable() => WorldOrigin.Shifted -= Move;

    void Move(Vector3 delta)
    {
        foreach (var root in roots)
            root.position += delta;
        // Any Unity position you keep (a target, a path, a spawn point) needs delta added too.
    }
}
```

The component's **On Origin Shifted** event in the Inspector is the same hook, for wiring to a method that takes a `Vector3`. To go between global and Unity positions: `WorldOrigin.ToGlobal(position)`, `WorldOrigin.ToLocal(global)`, `WorldOrigin.Origin`, and `GlobalPosition` (doubles, with `ToString()` / `TryParse` for the `"x,y,z"` text).

### JavaScript

The page hears the `origin-shifted` event on the scene. `e.detail.delta` is the vector to move by and `e.detail.origin` the new origin as `"x,y,z"`:

```js
const scene = BS.Scene.GetInstance();
const world = await new BS.GameObject("Large World").AddComponent(new BS.LargeWorld());
world.autoShift = true;
world.shiftThreshold = 256;

// Where the page put its objects, as Unity positions.
const placed = new Map(); // BS.GameObject -> BS.Vector3

scene.On("origin-shifted", async (e) => {
    const d = e.detail.delta;
    for (const [obj, p] of placed) {
        const moved = new BS.Vector3(p.x + d.x, p.y + d.y, p.z + d.z);
        placed.set(obj, moved);
        await obj.SetPosition(moved);
    }
});

// Global positions are text: parse them yourself when you need the numbers.
const origin = (await world.GetOrigin()).split(",").map(Number);
const here = await world.GetPlayerGlobalPosition();
const local = await world.GlobalToLocal("1048576,0,-2097152");
```

The page's event arrives after the physics step, a frame or more after the shift, so objects a page moves this way jump for a moment. Move anything big, or anything the player can see up close, from C# or Visual Scripting, which run inside the step.

### Visual Scripting

The nodes are under **BS > Large World** and **Events > BS > Large World**:

| Node | Description |
|------|-------------|
| **On Origin Shifted** | Fires inside the physics step of every shift. Output: **Delta**. Move your content here. |
| **Request Origin Shift** | Ask for a shift by **Delta**. Output: **Accepted**. |
| **Shift Origin To Player** | Shift so the player is back within 32 m of zero. Output: the **Delta** asked for. |
| **Suggest Origin Shift** | The shift that would bring the player back near zero, for a **Threshold** (32 to 1024 m), or zero. Asks for nothing. |
| **Set Origin** | Set the origin outright from **Origin** (`"x,y,z"`). Output: **Accepted**. |
| **Get Origin** | The origin, `"x,y,z"`. |
| **Local To Global** | A Unity position as a global `"x,y,z"`. |
| **Global To Local** | A global `"x,y,z"` as a Unity position, and whether the text was valid. |

A graph that moves a root: **On Origin Shifted** → the root's Transform **Get Position**, add **Delta** → Transform **Set Position**.

## Rules of Thumb

- **Keep your content near zero.** Build each piece of your world around its own root, near Unity's (0,0,0), and place it with your own global coordinates. A mesh whose vertices are kilometres from its pivot shakes however the origin moves.
- **Shift in multiples of 64 m.** Shifts by whole powers of two keep the origin and every shifted position exact, so nothing drifts after many shifts. `autoShift`, `ShiftOriginToPlayer` and `SuggestShift` always do.
- **Move content in the hook, not in `Update`.** The hook runs in the physics step of the shift, after the player moved and before physics. Moved a frame later, content visibly jumps, and for that frame the player is no longer where the ground is.
- **Shift by whole pieces.** Move a few roots rather than thousands of objects, and keep what the player stands on in one piece.

## Limits

- **Pages and graphs see Unity positions.** Positions in the JavaScript API, in the bridge and in Visual Scripting nodes are Unity positions for this player's origin, which differs from other players'. Convert with `LocalToGlobal` / `GlobalToLocal` before you store or send a position (in space state, user state or a one-shot message), and back when you receive one.
- **Some things Unity builds or bakes at fixed positions do not move with your content:** baked occlusion culling data, meshes combined by build-time static batching (do not mark content that moves **Batching Static**), and baked light probes. Leave them out of large worlds, or keep that content where it never needs to move.
- **The app executes shifts.** In SDK Play Mode nothing takes the request, so no shift happens there (a request just waits); test shifting in the app.
- **Each player shifts on their own.** Origins are per player: never send an origin or a Unity position to other players as if it were global.
