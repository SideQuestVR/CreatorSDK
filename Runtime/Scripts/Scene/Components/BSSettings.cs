using System;
using System.Collections.Generic;
using UnityEngine;

namespace BS
{
    /*
    #### Banter Settings
    Set the space's settings from the scene instead of from the page's script: which abilities players have,
    the refresh rate and clipping planes, and how player physics feels. They are applied when the space loads
    (and again whenever the page reloads), and whenever a property changes. Use one per scene.

    A page script calling `scene.SetSettings(...)` sends every setting and replaces these; lock the settings
    here (`isSettingsLocked` / `isPhysicsSettingsLocked`) to stop later changes. Settings locked by someone
    else are left alone.

    **Properties**
    - `enableTeleport` - Players can teleport.
    - `enableForceGrab` - Players can grab objects from a distance. On by default; turn it off to forbid it here.
    - `enableSpiderMan` - Players can use the grapple (spider-man) ability.
    - `enableBirdFlight` - Allow controller-driven wing flight, also gated by each player's preference.
    - `avatarSyncMode` - How often players far from each other see each other move: `social` (the default) or `action`.
    - `voiceDistanceMultiplier` - Scales how far voices carry here, on top of each player's own Voice Distance: 2 = twice as far, 0.5 = half (default 1).
    - `enableRun` - Players can run (hold Shift on desktop, click the left stick in VR). On by default; turn it off to keep everyone at walking speed.
    - `teleportMaxRange` - The furthest players can teleport here, in metres. 0 (the default) leaves it to each player's own Teleport Range.
    - `enableBrowserStreaming` - Players can stream their menu browser into the space for everyone to see. On by default; turn it off to forbid it here.
    - `enableUserUICollider` - Pointers land on other players, and clicking one opens their profile. On by default; turn it off so pointers pass through players, e.g. when the world aims at them itself.
    - `enableHandHold` - Players can hold hands.
    - `enableRadar` - Show the radar.
    - `enableNametags` - Show name tags above players.
    - `enablePortals` - Show portals to other spaces.
    - `enableGuests` - Guests (not signed in) can join.
    - `refreshRate` - Display refresh rate for headsets (e.g. 72, 90, 120).
    - `clippingPlane` - Camera near and far clipping planes, in metres.
    - `physicsMoveSpeed` - Walking speed.
    - `physicsMoveAcceleration` - How quickly players reach walking speed.
    - `physicsAirControlSpeed` - Speed players can steer at in the air.
    - `physicsAirControlAcceleration` - How quickly players steer in the air.
    - `physicsDrag` - Drag on the player's body.
    - `physicsFreeFallAngularDrag` - Angular drag while falling.
    - `physicsJumpStrength` - Jump strength multiplier.
    - `physicsHandPositionStrength` - How strongly hands follow the controllers' position.
    - `physicsHandRotationStrength` - How strongly hands follow the controllers' rotation.
    - `physicsHandSpringiness` - Springiness of the hands.
    - `physicsGrappleRange` - Grapple range, in metres.
    - `physicsGrappleReelSpeed` - Grapple reel speed.
    - `physicsGrappleSpringiness` - Grapple springiness.
    - `physicsGorillaMode` - Move by pushing off surfaces with the hands.
    - `isSettingsLocked` - Lock the settings so nothing can change them afterwards.
    - `isPhysicsSettingsLocked` - Lock the physics settings so nothing can change them afterwards.
    - `enableMegaphone` - Anyone in the space can turn their own megaphone on from the radial menu (heard across the room). Off by default: only moderators can megaphone people. Megaphone Volumes work either way.
    - `isMegaphoneLocked` - Lock the megaphone: only moderators can megaphone people, and Megaphone Volumes and `enableMegaphone` stop working. Players can still turn their own megaphone off. Once on it stays on until the space is left or rejoined: nothing can unlock it before then.
    - `megaphoneDistanceMultiplier` - How far megaphones and friends' walkie-talkies carry here, apart from normal voices: full volume to 40 m and silent at 100 m, times this (10 = silent at 1 km). 0 (the default) follows `voiceDistanceMultiplier`, as before.
    - `enableSafetyNet` - Players who fall below `safetyNetHeight` are put back at their spawn point (or wherever the world last moved them). On by default; turn it off to let players fall as far as the world goes (a player flung right out of the world, 100 km or more, is still brought back unless `enableDistanceLimit` is off).
    - `safetyNetHeight` - The height, in metres, below which the safety net catches a falling player (default -250). Keep it below everywhere players can stand or spawn.
    - `enableDistanceLimit` - Players 100 km or more from the world's origin are taken to be lost and put back at their spawn point (or wherever the world last moved them). On by default; turn it off for a world players travel far across (fast vehicles, space flight). Physics gets coarser the further out a player goes (positions 1.6 cm apart at 200 km, 6 cm at 1,000 km). A player whose physics breaks, or who is 10,000 km or more out, is still brought back. In a large world with a floating origin it is measured from the current origin.

    **Code Example**
    ```js
        const gameObject = new BS.GameObject("MySettings");
        const settings = await gameObject.AddComponent(new BS.BSSettings());
        settings.enableTeleport = false;
        settings.physicsJumpStrength = 2;
    ```

    */
    [DefaultExecutionOrder(-1)]
    [RequireComponent(typeof(BSObjectId))]
    [DisallowMultipleComponent]
    [WatchComponent]
    public partial class BSSettings : BSComponentBase
    {
        [Header("Abilities")]
        [Tooltip("Players can teleport.")]
        [See(initial = "true")][SerializeField] internal bool enableTeleport = true;
        // On by default here and in a space with no settings at all (BSSceneSettings, scene.ts SceneSettings). The
        // [See] initial stays "false": it is the default of the page-side `new BS.BSSettings(...)` constructor
        // parameter, which the bridge lock pins for good, so a page that builds this component in script without
        // naming it keeps the distance grab it always had (off).
        [Tooltip("Players can grab objects from a distance. On by default; turn it off to forbid it in this space. Players can also turn it off in Controls.")]
        [See(initial = "false")][SerializeField] internal bool enableForceGrab = true;
        [Tooltip("Players can use the grapple (spider-man) ability.")]
        [See(initial = "false")][SerializeField] internal bool enableSpiderMan = false;
        [Tooltip("Players can hold hands.")]
        [See(initial = "true")][SerializeField] internal bool enableHandHold = true;
        [Tooltip("Show the radar.")]
        [See(initial = "true")][SerializeField] internal bool enableRadar = true;
        [Tooltip("Show name tags above players.")]
        [See(initial = "true")][SerializeField] internal bool enableNametags = true;
        [Tooltip("Show portals to other spaces.")]
        [See(initial = "true")][SerializeField] internal bool enablePortals = true;
        [Tooltip("Guests (not signed in) can join.")]
        [See(initial = "true")][SerializeField] internal bool enableGuests = true;

        [Header("Display")]
        [Tooltip("Display refresh rate for headsets, e.g. 72, 90 or 120.")]
        [See(initial = "72")][SerializeField] internal float refreshRate = 72f;
        [Tooltip("Camera near (x) and far (y) clipping planes, in metres.")]
        [See(initial = "0.02,1500")][SerializeField] internal Vector2 clippingPlane = new Vector2(0.02f, 1500f);

        [Header("Player physics")]
        [Tooltip("Walking speed.")]
        [See(initial = "3.25")][SerializeField] internal float physicsMoveSpeed = 3.25f;
        [Tooltip("How quickly players reach walking speed.")]
        [See(initial = "4.6")][SerializeField] internal float physicsMoveAcceleration = 4.6f;
        [Tooltip("Speed players can steer at in the air.")]
        [See(initial = "2.4")][SerializeField] internal float physicsAirControlSpeed = 2.4f;
        [Tooltip("How quickly players steer in the air.")]
        [See(initial = "6")][SerializeField] internal float physicsAirControlAcceleration = 6f;
        [Tooltip("Drag on the player's body.")]
        [See(initial = "0")][SerializeField] internal float physicsDrag = 0f;
        [Tooltip("Angular drag while falling.")]
        [See(initial = "6")][SerializeField] internal float physicsFreeFallAngularDrag = 6f;
        [Tooltip("Jump strength multiplier.")]
        [See(initial = "1")][SerializeField] internal float physicsJumpStrength = 1f;
        [Tooltip("How strongly hands follow the controllers' position.")]
        [See(initial = "1")][SerializeField] internal float physicsHandPositionStrength = 1f;
        [Tooltip("How strongly hands follow the controllers' rotation.")]
        [See(initial = "1")][SerializeField] internal float physicsHandRotationStrength = 1f;
        [Tooltip("Springiness of the hands.")]
        [See(initial = "10")][SerializeField] internal float physicsHandSpringiness = 10f;
        [Tooltip("Grapple range, in metres.")]
        [See(initial = "512")][SerializeField] internal float physicsGrappleRange = 512f;
        [Tooltip("Grapple reel speed.")]
        [See(initial = "1")][SerializeField] internal float physicsGrappleReelSpeed = 1f;
        [Tooltip("Grapple springiness.")]
        [See(initial = "10")][SerializeField] internal float physicsGrappleSpringiness = 10f;
        [Tooltip("Move by pushing off surfaces with the hands.")]
        [See(initial = "false")][SerializeField] internal bool physicsGorillaMode = false;

        [Header("Locks")]
        [Tooltip("Lock the settings once applied, so nothing (a page script included) can change them afterwards.")]
        [See(initial = "false")][SerializeField] internal bool isSettingsLocked = false;
        [Tooltip("Lock the physics settings once applied, so nothing can change them afterwards.")]
        [See(initial = "false")][SerializeField] internal bool isPhysicsSettingsLocked = false;

        [Header("Flight")]
        [Tooltip("Allow bird flight with tracked controllers. Allowed by default; disable to forbid it in this space. Players can also disable it in Controls.")]
        [See(initial = "true")][SerializeField] internal bool enableBirdFlight = true;

        [Header("Networking")]
        [Tooltip("How often players far from each other see each other move. \"social\" (the default): full rate " +
                 "within 15 m, thinning to once a second past 70 m, which keeps big gatherings cheap. \"action\": full " +
                 "rate within 10 m, then 24, 15 and 8 updates a second out to 150 m, and 2 past that, for games and " +
                 "flight where distant players must move smoothly. Costs more bandwidth per distant player.")]
        [See(initial = "\"social\"")][SerializeField] internal string avatarSyncMode = "social";

        // Declared last on purpose: the JS constructor takes these fields in declaration order, and only a trailing
        // new parameter keeps existing pages' positional calls working (the bridge lock enforces it).
        [Header("Voice")]
        [Tooltip("Scales how far voices carry in this space, on top of each player's own Voice Distance setting: " +
                 "2 = twice as far (a stage or an auditorium), 0.5 = half (a quiet, close-up space). 1 = unchanged. " +
                 "The app clamps it to 0.1-10. Voices further away than this aren't received at all, so a big " +
                 "multiplier in a crowded space costs players bandwidth.")]
        [See(initial = "1")][SerializeField] internal float voiceDistanceMultiplier = 1f;

        // New fields go here, after everything else (see the note above voiceDistanceMultiplier).
        [Header("Running")]
        [Tooltip("Players can run: hold Shift on desktop, click the left stick in VR. On by default; turn it off " +
                 "to keep everyone at this space's walking speed.")]
        [See(initial = "true")][SerializeField] internal bool enableRun = true;

        [Header("Teleport range")]
        [Tooltip("The furthest players can teleport in this space, in metres along the ground. 0 = no limit from the " +
                 "space: each player's own Teleport Range applies (8 m unless they change it, up to 100 m). Set it to " +
                 "cap everyone's teleports at that distance. It only ever shortens a player's range.")]
        [See(initial = "0")][SerializeField] internal float teleportMaxRange = 0f;

        [Header("Browser")]
        [Tooltip("Players can stream their menu browser into this space, for everyone to see and hear. On by default; " +
                 "turn it off to forbid it here. Browsers you place in the scene yourself are not affected.")]
        [See(initial = "true")][SerializeField] internal bool enableBrowserStreaming = true;

        [Header("Players")]
        [Tooltip("Pointers land on other players, and clicking one opens their profile. On by default; turn it off so " +
                 "pointers pass through players, for a world that aims at them itself (a gun, a tagger). Their " +
                 "profiles still open from the People list.")]
        [See(initial = "true")][SerializeField] internal bool enableUserUICollider = true;

        [Header("Megaphone")]
        [Tooltip("Anyone in the space can turn their own megaphone on from the radial menu (heard across the room). " +
                 "Off by default: only moderators can megaphone people. Megaphone Volumes work either way.")]
        [See(initial = "false")][SerializeField] internal bool enableMegaphone = false;
        [Tooltip("Lock the megaphone: only moderators can megaphone people, and Megaphone Volumes and Enable Megaphone " +
                 "stop working. Players can still turn their own megaphone off. Once on it stays on until the space is " +
                 "left or rejoined: nothing can unlock it before then.")]
        [See(initial = "false")][SerializeField] internal bool isMegaphoneLocked = false;
        [Tooltip("How far megaphones and friends' walkie-talkies carry in this space, apart from normal voices: full " +
                 "volume to 40 m and silent at 100 m, times this. 10 = silent at 1 km, 25 = full volume to 1 km. " +
                 "Normal voices keep following Voice Distance Multiplier. 0 (the default) uses Voice Distance " +
                 "Multiplier for these too, as before. The app clamps it to 0.1-100, and never lets a megaphone go " +
                 "quiet nearer than a normal voice would. They are received at any " +
                 "distance anyway, so a large value costs players no bandwidth.")]
        [See(initial = "0")][SerializeField] internal float megaphoneDistanceMultiplier = 0f;

        [Header("Safety net")]
        [Tooltip("Players who fall below Safety Net Height are put back at their spawn point (or wherever the world last moved them). On by " +
                 "default; turn it off to let players fall as far as the world goes. A player flung right out of the " +
                 "world (100 km or more away) is still brought back, unless Enable Distance Limit is off.")]
        [See(initial = "true")][SerializeField] internal bool enableSafetyNet = true;
        [Tooltip("The height, in metres, below which the safety net catches a falling player. -250 by default. Keep it " +
                 "below everywhere players can stand or spawn: a player put back below it is caught again at once.")]
        [See(initial = "-250")][SerializeField] internal float safetyNetHeight = -250f;
        [Tooltip("Players 100 km or more from the world's origin are taken to be lost and put back at their spawn " +
                 "point (or wherever the world last moved them). On by default; turn it off for a world players travel far across (fast vehicles, space " +
                 "flight). Physics gets coarser the further out a player goes (1.6 cm steps at 200 km, 6 cm at " +
                 "1,000 km). A player whose physics breaks, or who is 10,000 km or more out, is still brought back.")]
        [See(initial = "true")][SerializeField] internal bool enableDistanceLimit = true;

        bool started;
        // The settings object (it is recreated on every page load) that this component locked, if any.
        // Its own locks don't stop it; locks set by anything else do.
        BSSceneSettings lockedSettings;
        BSSceneSettings lockedPhysicsSettings;
#if !GREENFIELD_PROJECT
        static bool loggedSdkNote;
#endif

        internal override void StartStuff()
        {
            started = true;
            scene.events.OnLoad.AddListener(ApplyAll);
            ApplyAll();
            SetLoadedIfNot();
        }

        internal override void UpdateStuff()
        {

        }

        internal override void DestroyStuff()
        {
            scene.events.OnLoad.RemoveListener(ApplyAll);
        }

        internal void UpdateCallback(List<PropertyName> changedProperties)
        {
            if (started)
            {
                Apply(changedProperties);
            }
        }

        void ApplyAll()
        {
            Apply(null);
        }

        /// <summary>Writes these settings to the space's settings: all of them, or just the changed ones.</summary>
        void Apply(List<PropertyName> changed)
        {
            var settings = scene.settings;
            if (settings == null)
            {
                // Not loaded yet; OnLoad applies them.
                return;
            }
            bool Has(PropertyName name) => changed == null || changed.Contains(name);

            if (!settings.IsSettingsLocked || lockedSettings == settings)
            {
                if (Has(PropertyName.enableTeleport)) settings.EnableTeleport = enableTeleport;
                if (Has(PropertyName.enableForceGrab)) settings.EnableForceGrab = enableForceGrab;
                if (Has(PropertyName.enableSpiderMan)) settings.EnableSpiderMan = enableSpiderMan;
                if (Has(PropertyName.enableBirdFlight)) settings.EnableBirdFlight = enableBirdFlight;
                if (Has(PropertyName.avatarSyncMode)) settings.AvatarSyncMode = avatarSyncMode;
                if (Has(PropertyName.voiceDistanceMultiplier)) settings.VoiceDistanceMultiplier = voiceDistanceMultiplier;
                if (Has(PropertyName.enableRun)) settings.EnableRun = enableRun;
                if (Has(PropertyName.teleportMaxRange)) settings.TeleportMaxRange = teleportMaxRange;
                if (Has(PropertyName.enableBrowserStreaming)) settings.EnableBrowserStreaming = enableBrowserStreaming;
                if (Has(PropertyName.enableUserUICollider)) settings.EnableUserUICollider = enableUserUICollider;
                if (Has(PropertyName.enableMegaphone)) settings.EnableMegaphone = enableMegaphone;
                if (Has(PropertyName.megaphoneDistanceMultiplier)) settings.MegaphoneDistanceMultiplier = megaphoneDistanceMultiplier;
                if (Has(PropertyName.enableSafetyNet)) settings.EnableSafetyNet = enableSafetyNet;
                if (Has(PropertyName.safetyNetHeight)) settings.SafetyNetHeight = safetyNetHeight;
                if (Has(PropertyName.enableDistanceLimit)) settings.EnableDistanceLimit = enableDistanceLimit;
                if (Has(PropertyName.enableHandHold)) settings.EnableHandHold = enableHandHold;
                if (Has(PropertyName.enableRadar)) settings.EnableRadar = enableRadar;
                if (Has(PropertyName.enableNametags)) settings.EnableNametags = enableNametags;
                if (Has(PropertyName.enablePortals)) settings.EnablePortals = enablePortals;
                if (Has(PropertyName.enableGuests)) settings.EnableGuests = enableGuests;
                if (Has(PropertyName.refreshRate)) settings.RefreshRate = refreshRate;
                if (Has(PropertyName.clippingPlane)) settings.ClippingPlane = clippingPlane;
            }
            if (!settings.IsPhysicsSettingsLocked || lockedPhysicsSettings == settings)
            {
                if (Has(PropertyName.physicsMoveSpeed)) settings.PhysicsMoveSpeed = physicsMoveSpeed;
                if (Has(PropertyName.physicsMoveAcceleration)) settings.PhysicsMoveAcceleration = physicsMoveAcceleration;
                if (Has(PropertyName.physicsAirControlSpeed)) settings.PhysicsAirControlSpeed = physicsAirControlSpeed;
                if (Has(PropertyName.physicsAirControlAcceleration)) settings.PhysicsAirControlAcceleration = physicsAirControlAcceleration;
                if (Has(PropertyName.physicsDrag)) settings.PhysicsDrag = physicsDrag;
                if (Has(PropertyName.physicsFreeFallAngularDrag)) settings.PhysicsFreeFallAngularDrag = physicsFreeFallAngularDrag;
                if (Has(PropertyName.physicsJumpStrength)) settings.PhysicsJumpStrength = physicsJumpStrength;
                if (Has(PropertyName.physicsHandPositionStrength)) settings.PhysicsHandPositionStrength = physicsHandPositionStrength;
                if (Has(PropertyName.physicsHandRotationStrength)) settings.PhysicsHandRotationStrength = physicsHandRotationStrength;
                if (Has(PropertyName.physicsHandSpringiness)) settings.PhysicsHandSpringiness = physicsHandSpringiness;
                if (Has(PropertyName.physicsGrappleRange)) settings.PhysicsGrappleRange = physicsGrappleRange;
                if (Has(PropertyName.physicsGrappleReelSpeed)) settings.PhysicsGrappleReelSpeed = physicsGrappleReelSpeed;
                if (Has(PropertyName.physicsGrappleSpringiness)) settings.PhysicsGrappleSpringiness = physicsGrappleSpringiness;
                if (Has(PropertyName.physicsGorillaMode)) settings.PhysicsGorillaMode = physicsGorillaMode;
            }

            // Locks go last, so this component's own values get in first. They only ever lock.
            if (isSettingsLocked && !settings.IsSettingsLocked)
            {
                settings.IsSettingsLocked = true;
                lockedSettings = settings;
            }
            if (isPhysicsSettingsLocked && !settings.IsPhysicsSettingsLocked)
            {
                settings.IsPhysicsSettingsLocked = true;
                lockedPhysicsSettings = settings;
            }
            // A latch: nothing unlocks it before the space is left or rejoined (BSSceneSettings.IsMegaphoneLocked).
            if (isMegaphoneLocked && !settings.IsMegaphoneLocked) settings.IsMegaphoneLocked = true;

#if !GREENFIELD_PROJECT
            if (!loggedSdkNote)
            {
                loggedSdkNote = true;
                LogLine.Do($"[BSSettings] Applied the scene settings from '{name}'. The SDK's desktop player only uses the clipping planes; the rest take effect in the client.");
            }
#endif
        }
    }
}