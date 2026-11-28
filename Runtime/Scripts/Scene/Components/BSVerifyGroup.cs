using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BS
{
    /*
    #### Banter Verify Group
    Checks that the signed-in player is a member of a SideQuest group, and runs an outcome for anyone who isn't:
    teleport them to a position in this world, or show a message and send them to another world. The group's owner and
    moderators count as members; a player banned from the group, a guest, or a player who isn't signed in doesn't. When
    the app can't find out (no connection, a timeout), the player counts as not a member.

    The check waits for the space to load, runs once, and again when `groupId` changes or `Recheck()` is called. The
    outcome settings are read when the outcome runs. Turning the object off and on again doesn't repeat a finished
    check. A player's app checks at most 4 different groups for a world's address while it runs; any more count as not
    a member.

    This keeps visitors out of the way, it doesn't lock them out: a modified client can get past it. To close a world to
    everyone else, set its access to the group's members on SideQuest.

    **Properties**
    - `groupId` - The SideQuest group to check: its number, as in the group's link.
    - `outcome` - What happens to a player who isn't a member: 0 teleports them to `teleportPosition`, 1 shows `errorMessage` and sends them to `worldUrl`.
    - `teleportPosition` - Outcome 0: where the player goes, in world space.
    - `worldUrl` - Outcome 1: the world the player is sent to (http or https).
    - `errorMessage` - Outcome 1: the message shown as the player leaves. Empty shows nothing.
    - `isMember` - Read only: true once the check has found the player is a member.
    - `checkState` - Read only: "none", "checking", "member", "not-member", "error" (counted as not a member), "invalid" (not a group id) or "skipped" (the SDK's Play Mode, which can't check).

    **Methods**
    - `Recheck()` - Run the check again, as when the player has just signed in.

    **Events**
    - `group-checked` - The check has finished; `e.detail.isMember` and `e.detail.state` say how.

    **Code Example**
    ```js
        const gate = await new BS.GameObject("Members").AddComponent(new BS.VerifyGroup({
            groupId: "12345", outcome: 0, teleportPosition: new BS.Vector3(0, 0, -20)
        }));
        gate.On("group-checked", e => console.log("member:", e.detail.isMember, e.detail.state));
    ```
    */
    [DefaultExecutionOrder(-1)]
    [RequireComponent(typeof(BSObjectId))]
    [WatchComponent]
    [AddComponentMenu("BS/Verify Group")]
    public partial class BSVerifyGroup : BSComponentBase
    {
        public const string StateNone = "none";
        public const string StateChecking = "checking";
        public const string StateMember = "member";
        public const string StateNotMember = "not-member";
        public const string StateError = "error";
        public const string StateInvalid = "invalid";
        public const string StateSkipped = "skipped";

        [Tooltip("The SideQuest group to check: its number, as in the group's link.")]
        [See(initial = "")][SerializeField] internal string groupId = "";

        [Tooltip("What happens to a player who isn't a member of the group.")]
        [ShowAsEnum(typeof(VerifyHostOutcome))]
        [See(initial = "0")][SerializeField] internal int outcome = (int)VerifyHostOutcome.TeleportToPosition;

        [Tooltip("Teleport To Position: where the player goes, in world space.")]
        [HideWhen(nameof(outcome), VerifyHostOutcome.SendToWorld)]
        [See(initial = "0,0,0")][SerializeField] internal Vector3 teleportPosition = Vector3.zero;

        [Tooltip("Send To World: the world the player is sent to (http or https).")]
        [HideWhen(nameof(outcome), VerifyHostOutcome.TeleportToPosition)]
        [See(initial = "")][SerializeField] internal string worldUrl = "";

        [Tooltip("Send To World: the message shown as the player leaves. Leave empty to show nothing.")]
        [HideWhen(nameof(outcome), VerifyHostOutcome.TeleportToPosition)]
        [See(initial = "\"This world is only open to members of its group.\"")][SerializeField] internal string errorMessage = GroupMembershipCheck.DefaultErrorMessage;

        [Tooltip("Read only: true once the check has found the player is a member.")]
        [See(initial = "false")][SerializeField] internal bool isMember = false;

        [Tooltip("Read only: none, checking, member, not-member, error, invalid or skipped.")]
        [See(initial = "\"none\"")][SerializeField] internal string checkState = StateNone;

        [Tooltip("Raised when the check finds the player is a member.")]
        public UnityEvent onMember = new UnityEvent();

        [Tooltip("Raised when the check finds the player isn't a member (or couldn't tell), after the outcome has run.")]
        public UnityEvent onNotMember = new UnityEvent();

        // The truth about the check. The page cannot write it: a write is put back (see UpdateCallback).
        string _state = StateNone;
        bool _isMember;
        bool _checkStarted;
        string _checkedGroupId;
        Coroutine _check;
        int _generation;

        /// <summary>The outcome as the check uses it: an unknown number (a page can write anything) teleports.</summary>
        public VerifyHostOutcome EffectiveOutcome =>
            outcome == (int)VerifyHostOutcome.SendToWorld ? VerifyHostOutcome.SendToWorld : VerifyHostOutcome.TeleportToPosition;

        internal override void StartStuff()
        {
            // Never holds the space's load: the check itself waits for the load to finish.
            SetLoadedIfNot();
            StartCheck();
        }

        internal override void UpdateStuff() { }

        internal override void DestroyStuff()
        {
            _generation++;
            _check = null;
        }

        void OnEnable()
        {
            if (!_checkStarted)
            {
                // Nothing checked yet: a copy made with Instantiate (or a box ticked in the Inspector) can carry another
                // check's result, and Init would hand it to the page. Only this component's own check sets these.
                isMember = _isMember = false;
                checkState = _state = StateNone;
                return;
            }
            // Turned back on while a check was running: the disable stopped it, so ask again. A finished check stands,
            // so toggling the object doesn't run the outcome again.
            if (alreadyStarted && _state == StateChecking) StartCheck();
        }

        void OnDisable()
        {
            // A coroutine stops with its component; the next enable starts a fresh one.
            _generation++;
            _check = null;
        }

        internal void UpdateCallback(List<PropertyName> changedProperties)
        {
            if (changedProperties.Contains(PropertyName.isMember) || changedProperties.Contains(PropertyName.checkState))
            {
                // Read only: put back what the check knows.
                isMember = _isMember;
                checkState = _state;
                if (alreadyStarted) SyncProperties(true);
            }
            // A new group is a new question. Only once StartStuff has asked the first one: a component made from JS gets
            // its values here before it starts.
            if (changedProperties.Contains(PropertyName.groupId) && _checkStarted
                && GroupMembershipCheck.NormalizeGroupId(groupId) != _checkedGroupId)
                StartCheck();
        }

        [Method]
        public bool _Recheck()
        {
            if (!alreadyStarted || !isActiveAndEnabled) return false;
            StartCheck();
            return true;
        }

        void StartCheck()
        {
            if (!isActiveAndEnabled) return;
            _checkStarted = true;
            if (_check != null) StopCoroutine(_check);
            var generation = ++_generation;
            _checkedGroupId = GroupMembershipCheck.NormalizeGroupId(groupId);
            SetStatus(StateChecking, false);
            // The outcome is read when it runs: a page may set it while the space is still loading.
            _check = StartCoroutine(GroupMembershipCheck.Run(groupId,
                () => new GroupMembershipCheck.Outcome(EffectiveOutcome, teleportPosition, worldUrl, errorMessage),
                result => Finish(generation, result)));
        }

        void Finish(int generation, GroupMembershipResult result)
        {
            if (generation != _generation) return;
            _check = null;
            var member = result == GroupMembershipResult.Member;
            SetStatus(StateOf(result), member);
            if (scene.link != null) scene.link.OnGroupChecked(cid, member, _state);
            if (member) onMember?.Invoke();
            else if (GroupMembershipCheck.RunsOutcome(result)) onNotMember?.Invoke();
        }

        /// <summary>The <see cref="checkState"/> a result reports.</summary>
        internal static string StateOf(GroupMembershipResult result)
        {
            switch (result)
            {
                case GroupMembershipResult.Member: return StateMember;
                case GroupMembershipResult.NotMember: return StateNotMember;
                case GroupMembershipResult.Invalid: return StateInvalid;
                case GroupMembershipResult.Skipped: return StateSkipped;
                default: return StateError;
            }
        }

        void SetStatus(string state, bool member)
        {
            if (_state == state && _isMember == member && checkState == state && isMember == member)
                return;
            _state = checkState = state;
            _isMember = isMember = member;
            if (alreadyStarted) SyncProperties(true);
        }
    }
}
