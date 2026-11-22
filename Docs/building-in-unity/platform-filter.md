# Platform Filter (BSPlatformFilter)

Shows a GameObject, and everything under it, only to the players you choose: on PC or on Android, in VR or on a flat screen, on older or newer Android headsets, or any combination. Add it with **Add Component > BS > Platform Filter**, or search for *Platform Filter* (one per GameObject). Scripts can add it too, as `BS.PlatformFilter`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `includeOnMobile` | bool | `true` | Show on Android (Quest) |
| `includeOnDesktop` | bool | `true` | Show on PC (Windows) |
| `includeInVR` | bool | `true` | Show to players in a headset |
| `includeInNonVR` | bool | `true` | Show to players on a flat screen |
| `includeOnLowEndMobile` | bool | `true` | Show on older Android headsets: Quest 2, Quest Pro, Quest 1, and any the app does not recognise |
| `includeOnHighEndMobile` | bool | `true` | Show on Quest 3, Quest 3S and newer Android headsets |

## Combining the Halves

The filter has three halves: the platform (`includeOnMobile`, `includeOnDesktop`), the mode (`includeInVR`, `includeInNonVR`) and the headset (`includeOnLowEndMobile`, `includeOnHighEndMobile`). An object shows only when **all** of them let it through, and within a half any ticked box is enough. The headset half only applies on Android: on PC it lets everything through.

| Wanted | Mobile | Desktop | VR | Non-VR |
|--------|:------:|:-------:|:--:|:------:|
| Everyone (the default) | ✓ | ✓ | ✓ | ✓ |
| PC only | | ✓ | ✓ | ✓ |
| Android (Quest) only | ✓ | | ✓ | ✓ |
| VR only, any platform | ✓ | ✓ | ✓ | |
| Flat screens only | ✓ | ✓ | | ✓ |
| Android + VR only | ✓ | | ✓ | |
| PC + flat screen only | | ✓ | | ✓ |

Unticking both boxes in one half hides the object from everyone (both headset boxes: everyone on Android).

## Older and Newer Headsets

A Quest 3 has about twice the GPU of a Quest 2. Use the headset half to give each its own version of something heavy:

| Wanted | Mobile | Desktop | Low end | High end |
|--------|:------:|:-------:|:-------:|:--------:|
| Quest 3 and newer only | ✓ | | | ✓ |
| Quest 2 / Quest Pro only | ✓ | | ✓ | |
| Everyone but older headsets | ✓ | ✓ | | ✓ |

Low end is the Quest 2, Quest Pro, Quest 1, Pico 4 and Neo 3, and any Android headset the app does not recognise yet (so a new one gets the lighter version until the app knows it). High end is the Quest 3, Quest 3S, Steam Frame and Pico 4 Ultra. Each player's app decides this on their own device, so an object restricted by headset keeps its filter in the bundle and hides itself when it starts, like the VR half. An app from before these boxes existed ignores them and shows the object on every Android headset.

## When It Is Applied

- **Building a space bundle:** each platform is built separately, so the platform half is settled there. An object excluded from a platform is left out of that platform's bundle with its whole subtree, and a nested filter cannot bring a child of an excluded ancestor back.
- **On the player's device:** whether a player is in VR, and which headset they have, is only known on their device, so a filter that restricts VR, Non-VR or a headset ships with the object and hides it when it starts. A filter that only restricts the platform has done its job in the build and is stripped from it.
- **Objects made by scripts:** the filter is checked when it is added, and again whenever one of its properties changes.

Hiding switches the GameObject off (`SetActive(false)`). The filter only switches back on what it switched off itself; an object you deactivated stays deactivated.

In the Unity editor's Play mode the filter judges you as a PC player (the editor never counts as Android, whatever the build target), and as a VR player only while a headset is active. Outside Play mode you see everything, so filtered objects can still be edited.

## Example: Platform-Specific Detail

Keep an expensive decoration on PC and a cheap stand-in on Quest with two sibling GameObjects at the same position:

1. `FountainHighPoly`: the full model. Add a Platform Filter, untick **includeOnMobile**.
2. `FountainMobile`: a low-poly mesh with a baked texture. Add a Platform Filter, untick **includeOnDesktop**.

Windows builds ship only `FountainHighPoly`; Android (Quest) builds ship only `FountainMobile`.

## Example: VR-Only Hint From a Script

```js
// A "grab the lever" hint, only for Quest players in a headset.
const hint = new BS.GameObject("LeverHint");
await hint.AddComponent(new BS.PlatformFilter({ includeOnDesktop: false, includeInNonVR: false }));

// Later, show it to everyone in VR on any platform:
const filter = hint.GetComponent(BS.CT.PlatformFilter);
filter.includeOnDesktop = true;
```

## Checking the Platform Yourself

A filter only shows or hides an object. To do anything else per platform, such as turning down a particle count on Quest, ask in a script or a graph:

| Graph node (`BS > Utils`) | Script | Answer |
|---------------------------|--------|--------|
| **Is Android** | `await scene.IsAndroid()` | True on Quest (Android). False in the Unity editor, whatever the build target |
| **Is Windows** | `await scene.IsWindows()` | True on Windows, the Windows editor included |
| **Is VR** | `await scene.IsVR()` | True in a headset, false on a flat screen. In Play mode, true only when an XR headset is active |
| **Is Low End Mobile** | `await scene.IsLowEndMobile()` | True on an Android headset older than the Quest 3 generation (the headset half's low end). False on PC and in the Unity editor |
| **Get Platform** | `await scene.GetPlatform()` | The platform as comma-separated text, starting with `VR` or `2D` and ending with the operating system. Empty in Play mode |

The filter goes by **Is Android** and **Is VR**, so anything that isn't Android counts as PC. [Utility Methods](../javascript-api/scene-api.md#utility-methods) has more on the script calls.

## Filters in a Graph

Platform Filter has [member nodes](../visual-scripting/overview.md#codebase-member-nodes), like other BS components. Type `BSPlatformFilter` in the fuzzy finder to find them:

| Node | Use |
|------|-----|
| **Set Include On Mobile**, **Set Include On Desktop**, **Set Include In VR**, **Set Include In Non VR**, **Set Include On Low End Mobile**, **Set Include On High End Mobile** | Change a filter. Each has a **Get** node too |
| **Refresh** | Apply the filter again |
| **Get Needs Runtime Check** | True when the filter restricts VR, Non-VR or a headset |
| **Should Show** | Whether an object with these settings shows, leaving the headset half aside: give it whether the player is on Android and in VR, then the four include values |
| **Should Show On Device** | The same with the headset half: also whether the player has a low-end headset, and the two headset values |
| **Is Headset Included** | The headset half on its own |
| **Is Platform Included** | The platform half of that on its own (mobile means Android) |
| **Is Mode Included** | The VR half of that on its own |

**Should Show**, **Should Show On Device**, **Is Platform Included**, **Is Mode Included** and **Is Headset Included** only work out an answer from their inputs: they don't read a filter or change one.

Three things to watch for:

- **Follow a Set with Refresh.** Every Get and Set name appears twice in the fuzzy finder, once for the property and once for the field, and only the property's Set applies the change at once. **Refresh** applies it whichever one you picked.
- **Only some filters reach the published world.** The Builder removes a filter when VR and Non-VR are both ticked and so are both headset boxes, so a graph finds that filter in Play mode but not in the published world.
- **A hidden object's own graph stops.** Hiding switches the object off, so change its filter from a graph on another object.
