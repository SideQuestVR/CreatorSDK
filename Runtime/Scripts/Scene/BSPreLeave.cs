using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BS
{
    /// <summary>
    /// The user-leaving hook: just before this client leaves a world (another world, home, a rejoin, quitting), the
    /// world's page gets the user-leaving event and its Visual Scripting graphs On User Leaving, so it can tidy shared
    /// state, above all when it is the last one there. Best-effort: a crash, a dropped connection, closing a Quest app
    /// from the system (rather than its own Exit) or putting it to sleep, and stopping Play mode leave without it.
    ///
    /// Costs nothing for a world that doesn't listen: the page says when its listener count leaves or reaches 0, and
    /// graphs count themselves in and out, so with neither nothing is sent and nothing waits. With a listener the
    /// leave waits for the page's handlers (their waitUntil promises), at most <see cref="TimeoutSeconds"/>, then
    /// for the state writes still queued.
    /// </summary>
    public static class BSPreLeave
    {
        public const string ReasonSwitch = "switch";
        public const string ReasonHome = "home";
        public const string ReasonReload = "reload";
        public const string ReasonQuit = "quit";

        /// <summary>How long the page's handlers may take. The whole hook, queued writes included, takes at most this plus half a second.</summary>
        public static float TimeoutSeconds = 1.5f;

        /// <summary>A graph can't await: when only graphs listen, how long they get before the leave goes on.</summary>
        public static float VisualScriptingGraceSeconds = 0.2f;

        // Beyond the page's own limit: its "done" still has to cross the bridge, and the last writes to go.
        const float SlackSeconds = 0.25f;

        // However quick the page, its queued writes get at least this long.
        const float MinFlushSeconds = 0.25f;

        // ------------------------------------------------------------------ host hooks (all optional)

        /// <summary>The other users in the room. Unset: the scene's users other than the local one.</summary>
        public static Func<int> OtherUserCount;

        /// <summary>Whether the room can still be written to. Unset: true. False skips the hook: writes would fail.</summary>
        public static Func<bool> CanReachRoom;

        /// <summary>Whether a URL is the player's home world, for the "home" reason.</summary>
        public static Func<string, bool> IsHomeUrl;

        /// <summary>True while the hook runs: the host sends state writes without its usual pacing.</summary>
        public static Action<bool> SetUrgent;

        /// <summary>Waits, at most the given seconds, for the state writes the host still has queued.</summary>
        public static Func<float, Task> FlushState;

        // ------------------------------------------------------------------ listeners

        static volatile bool pageListening;
        static int graphListeners;
        static bool firedForThisPage;
        static Task running;
        static int lastToken;
        static volatile int awaitedToken;
        static TaskCompletionSource<bool> pageDone;
        static bool quitApproved;

        /// <summary>Whether anything listens: the page or a graph.</summary>
        public static bool HasListeners => pageListening || Volatile.Read(ref graphListeners) > 0;

        /// <summary>On User Leaving units count themselves in while they listen, and out again.</summary>
        public static void AddGraphListener() => Interlocked.Increment(ref graphListeners);

        public static void RemoveGraphListener()
        {
            if (Interlocked.Decrement(ref graphListeners) < 0) Interlocked.Exchange(ref graphListeners, 0);
        }

        /// <summary>The page's listener count left 0 (true) or reached it (false).</summary>
        internal static void SetPageListening(bool listening) => pageListening = listening;

        /// <summary>A new page is loading: the old one's listening, and the hook having run for it, go with it.</summary>
        internal static void PageLoadStarted()
        {
            pageListening = false;
            firedForThisPage = false;
        }

        /// <summary>The page's handlers have settled (or hit their limit) for the event that carried this token.</summary>
        internal static void PageDone(string token)
        {
            if (!int.TryParse(token, out var value) || value != awaitedToken) return;
            pageDone?.TrySetResult(true);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            pageListening = false;
            graphListeners = 0;
            firedForThisPage = false;
            running = null;
            pageDone = null;
            awaitedToken = 0;
            quitApproved = false;
#if !UNITY_EDITOR
            // Stopping Play mode never asks; only a player's quit does (window close, Alt+F4, Application.Quit).
            Application.wantsToQuit -= OnWantsToQuit;
            Application.wantsToQuit += OnWantsToQuit;
#endif
        }

        /// <summary>The reason for leaving the current page for <paramref name="url"/>.</summary>
        public static string ReasonFor(BSScene scene, string url)
        {
            // The same page, written with or without a trailing slash, is a reload (crawl.example and crawl.example/).
            if (scene != null && !string.IsNullOrEmpty(scene.CurrentUrl) && !string.IsNullOrEmpty(url)
                && string.Equals(url.Trim().TrimEnd('/'), scene.CurrentUrl.Trim().TrimEnd('/'), StringComparison.Ordinal))
                return ReasonReload;
            try
            {
                if (IsHomeUrl != null && IsHomeUrl(url)) return ReasonHome;
            }
            catch (Exception e) { Debug.LogException(e); }
            return ReasonSwitch;
        }

        /// <summary>
        /// Runs the hook for the page loaded now, once: a second leave while it runs gets the same task, and one after
        /// it ran (before the next page loads) gets a finished one. Finished at once when nothing listens.
        /// </summary>
        public static Task RunAsync(BSScene scene, string reason)
        {
            if (running != null && !running.IsCompleted) return running;
            if (firedForThisPage || scene == null) return Task.CompletedTask;
            bool page = pageListening && scene.link != null && scene.isReady;
            bool graphs = Volatile.Read(ref graphListeners) > 0;
            if (!page && !graphs) return Task.CompletedTask;
            try
            {
                if (CanReachRoom != null && !CanReachRoom()) return Task.CompletedTask;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return Task.CompletedTask;
            }
            firedForThisPage = true;
            running = Run(scene, reason, page, graphs);
            return running;
        }

        static async Task Run(BSScene scene, string reason, bool page, bool graphs)
        {
            var watch = Stopwatch.StartNew();
            float timeout = Mathf.Max(0.1f, TimeoutSeconds);
            int peers = CountOthers(scene);
            bool isLastUser = peers == 0;
            bool answered = false;
            try
            {
                SetUrgent?.Invoke(true);
                Task pageWait = Task.CompletedTask;
                TaskCompletionSource<bool> done = null;
                if (page)
                {
                    done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    pageDone = done;
                    int token = ++lastToken;
                    awaitedToken = token;
                    scene.link.OnUserLeaving(token, reason, isLastUser, peers, Mathf.RoundToInt(timeout * 1000));
                    pageWait = Task.WhenAny(done.Task, Task.Delay(TimeSpan.FromSeconds(timeout + SlackSeconds)));
                }
                if (graphs)
                {
                    // A graph that throws must not stop the leave.
                    try { EventBus.Trigger("OnUserLeaving", new CustomEventArgs("", new object[] { reason, isLastUser, peers })); }
                    catch (Exception e) { Debug.LogException(e); }
                }
                var graphWait = graphs && !page ? Task.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, VisualScriptingGraceSeconds))) : Task.CompletedTask;
                await Task.WhenAll(pageWait, graphWait);
                answered = done != null && done.Task.IsCompleted;
                // One budget for the whole hook: the writes get what the page left of it.
                float left = timeout + SlackSeconds - (float)watch.Elapsed.TotalSeconds;
                if (FlushState != null) await FlushState(Mathf.Max(MinFlushSeconds, left));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                pageDone = null;
                awaitedToken = 0;
                try { SetUrgent?.Invoke(false); }
                catch (Exception e) { Debug.LogException(e); }
                LogLine.Do($"[PreLeave] {reason}: page {(page ? (answered ? "done" : "timed out") : "not listening")}, " +
                           $"graphs {(graphs ? "told" : "none")}, isLastUser={isLastUser} peers={peers}, {watch.ElapsedMilliseconds} ms");
            }
        }

        static int CountOthers(BSScene scene)
        {
            try
            {
                if (OtherUserCount != null) return Mathf.Max(0, OtherUserCount());
            }
            catch (Exception e) { Debug.LogException(e); }
            int others = 0;
            if (scene?.users != null)
                foreach (var user in scene.users)
                    if (user != null && !user.isLocal) others++;
            return others;
        }

        // A player's quit: hold it while the hook runs, then quit for real. Never asks twice.
        static bool OnWantsToQuit()
        {
            if (quitApproved) return true;
            Task hook;
            try { hook = RunAsync(BSScene.Current, ReasonQuit); }
            catch (Exception e)
            {
                Debug.LogException(e);
                return true;
            }
            if (hook.IsCompleted) return true;
            _ = QuitAfter(hook);
            return false;
        }

        static async Task QuitAfter(Task hook)
        {
            try { await hook; }
            catch (Exception e) { Debug.LogException(e); }
            quitApproved = true;
            Application.Quit();
        }
    }
}
