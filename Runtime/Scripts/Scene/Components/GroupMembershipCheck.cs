using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace BS
{
    /// <summary>What a <see cref="GroupMembershipCheck"/> found out about the local player and a group.</summary>
    public enum GroupMembershipResult
    {
        /// <summary>The signed-in player owns, moderates or is a member of the group.</summary>
        Member = 0,
        /// <summary>Not in the group, banned from it, not signed in, or a guest.</summary>
        NotMember = 1,
        /// <summary>The app couldn't find out (no connection, a timeout, a refused lookup). Counts as not a member.</summary>
        Error = 2,
        /// <summary>The group id isn't a group id. A creator's mistake, so nothing happens to the player.</summary>
        Invalid = 3,
        /// <summary>Nothing in this app can answer (a creator's own Play Mode), so nothing happens to the player.</summary>
        Skipped = 4,
    }

    /// <summary>
    /// The check behind <see cref="BSVerifyGroup"/> and the Verify Group node. Asks the app whether the signed-in player
    /// is in a SideQuest group and, when they aren't, runs the chosen outcome for them, as Verify Host does for a world
    /// at the wrong address. When the app can't tell, the player counts as not a member.
    ///
    /// This keeps visitors out of the way, it doesn't lock them out: a modified client can get past it. A world that
    /// must be closed to everyone else sets its access to the group's members on SideQuest instead.
    /// </summary>
    public static class GroupMembershipCheck
    {
        /// <summary>The error a player sees on Send To World, until the creator writes their own.</summary>
        public const string DefaultErrorMessage = "This world is only open to members of its group.";

        /// <summary>
        /// Set by an app that knows who is signed in (Greenfield's client does at start-up). It is given a group id as
        /// <see cref="NormalizeGroupId"/> spells it and answers for the signed-in player: Member, NotMember, Error or
        /// Invalid. Without one (a creator's own Play Mode) every check is Skipped.
        /// </summary>
        public static Func<string, Task<GroupMembershipResult>> Provider { get; set; }

        /// <summary>How long a check waits for the app's answer before it counts as Error.</summary>
        internal static readonly TimeSpan AnswerTimeout = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Runs the check as a coroutine. Start it on something that goes when the world does (the component, a graph's
        /// machine), so a late answer can't act on the next world. It waits for the space to load, as Verify Host does.
        /// <paramref name="done"/> gets the result, after the outcome has run.
        /// </summary>
        public static IEnumerator Run(string groupId, VerifyHostOutcome outcome, Vector3 teleportPosition, string worldUrl,
                                      string errorMessage, Action<GroupMembershipResult> done = null)
        {
            return Run(groupId, () => new Outcome(outcome, teleportPosition, worldUrl, errorMessage), done);
        }

        /// <summary>What a failed check does, as it stands when the check fails.</summary>
        public readonly struct Outcome
        {
            public readonly VerifyHostOutcome kind;
            public readonly Vector3 teleportPosition;
            public readonly string worldUrl;
            public readonly string errorMessage;

            public Outcome(VerifyHostOutcome kind, Vector3 teleportPosition, string worldUrl, string errorMessage)
            {
                this.kind = kind;
                this.teleportPosition = teleportPosition;
                this.worldUrl = worldUrl;
                this.errorMessage = errorMessage;
            }
        }

        /// <summary>
        /// As <see cref="Run(string, VerifyHostOutcome, Vector3, string, string, Action{GroupMembershipResult})"/>, with
        /// the outcome read only when it runs, so a setting changed while the space loads (a page's, say) counts.
        /// </summary>
        public static IEnumerator Run(string groupId, Func<Outcome> outcomeNow, Action<GroupMembershipResult> done = null)
        {
            var scene = BSScene.Instance();
            yield return new WaitUntil(() => VerifyHostCheck.CanRun(scene));

            var id = NormalizeGroupId(groupId);
            if (id == null)
            {
                Debug.LogWarning($"[BS] Verify Group needs a group id (a number), got '{Shown(groupId)}'.");
                done?.Invoke(GroupMembershipResult.Invalid);
                yield break;
            }
            var provider = Provider;
            Task<GroupMembershipResult> answer = null;
            if (provider == null)
            {
#if UNITY_EDITOR && !GREENFIELD_PROJECT
                // SDK Play Mode: nothing here knows who is signed in. Only here: an app without a provider fails closed.
                Debug.Log($"[BS] Verify Group is skipped in Play Mode, which can't tell which groups you're in (group {id}).");
                done?.Invoke(GroupMembershipResult.Skipped);
                yield break;
#else
                Debug.LogWarning($"[BS] Verify Group: this app can't tell which groups the player is in (group {id}).");
#endif
            }
            else
            {
                try
                {
                    answer = provider(id);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[BS] Verify Group couldn't ask about group {id}: {e.GetType().Name}.");
                }
                var giveUpAt = Time.realtimeSinceStartup + (float)AnswerTimeout.TotalSeconds;
                yield return new WaitUntil(() => answer == null || answer.IsCompleted || Time.realtimeSinceStartup >= giveUpAt);
            }

            var result = ResultOf(answer);
            if (result == GroupMembershipResult.Invalid)
            {
                Debug.LogWarning($"[BS] Verify Group: group {id} doesn't exist or can't be checked.");
                done?.Invoke(result);
                yield break;
            }
            if (!RunsOutcome(result))
            {
                done?.Invoke(result);
                yield break;
            }

            Debug.LogWarning(result == GroupMembershipResult.Error
                ? $"[BS] Verify Group couldn't find out whether the player is in group {id}, so they count as not a member."
                : $"[BS] Verify Group: the player isn't a member of group {id}.");
            var outcome = outcomeNow();
            VerifyHostCheck.RunOutcome("Verify Group", scene, outcome.kind, outcome.teleportPosition, outcome.worldUrl,
                outcome.errorMessage, VerifyHostCheck.HostOf(scene.CurrentUrl));
            done?.Invoke(result);
        }

        /// <summary>True for the results that move the player: not a member, and couldn't tell.</summary>
        internal static bool RunsOutcome(GroupMembershipResult result) =>
            result == GroupMembershipResult.NotMember || result == GroupMembershipResult.Error;

        /// <summary>
        /// The app's answer as a result: Error when there is none, it isn't finished (the wait timed out), it failed, or
        /// it is a value the app may not give (Skipped is the check's own).
        /// </summary>
        internal static GroupMembershipResult ResultOf(Task<GroupMembershipResult> answer)
        {
            if (answer == null || !answer.IsCompleted || answer.IsFaulted || answer.IsCanceled)
                return GroupMembershipResult.Error;
            var result = answer.Result;
            return result == GroupMembershipResult.Member || result == GroupMembershipResult.NotMember
                   || result == GroupMembershipResult.Invalid
                ? result
                : GroupMembershipResult.Error;
        }

        /// <summary>
        /// A group id as the API knows it: a positive whole number, without spaces or leading zeros. Null when it isn't
        /// one.
        /// </summary>
        public static string NormalizeGroupId(string groupId)
        {
            var text = groupId?.Trim() ?? "";
            if (text.Length == 0 || text.Length > 18) return null;
            for (int i = 0; i < text.Length; i++)
                if (text[i] < '0' || text[i] > '9') return null;
            var value = long.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
            return value > 0 ? value.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
        }

        // Capped: these come from world content, and Player.log can be uploaded.
        static string Shown(string text) => text == null ? "" : text.Length > 200 ? text.Substring(0, 200) + "…" : text;
    }
}
