using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BS.SDKEditor.Tests
{
    /// <summary>
    /// GroupMembershipCheck's decisions: which group ids it asks about, how it reads the app's answer, and which answers
    /// move the player. Fail closed: anything but a clear "member" (or a creator's own mistake) runs the outcome.
    /// </summary>
    public class VerifyGroupTests
    {
        [TestCase("12345", "12345")]
        [TestCase("  12345 ", "12345")]
        [TestCase("007", "7")]
        [TestCase("1", "1")]
        [TestCase("999999999999999999", "999999999999999999")]
        public void NormalizeGroupId_SpellsAGroupIdAsTheApiDoes(string groupId, string expected)
        {
            Assert.AreEqual(expected, GroupMembershipCheck.NormalizeGroupId(groupId));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("0")]
        [TestCase("000")]
        [TestCase("-5")]
        [TestCase("+5")]
        [TestCase("12.5")]
        [TestCase("1e5")]
        [TestCase("12 345")]
        [TestCase("abc")]
        [TestCase("https://sidequestvr.com/community/12345")]
        [TestCase("1234567890123456789")] // 19 digits: past what the API's ids reach, and long's range is near
        [TestCase("١٢٣")] // Arabic-Indic digits: char.IsDigit says yes, the API says no
        public void NormalizeGroupId_IsNullForAnythingThatIsNotAGroupId(string groupId)
        {
            Assert.IsNull(GroupMembershipCheck.NormalizeGroupId(groupId));
        }

        [TestCase(GroupMembershipResult.NotMember, true)]
        [TestCase(GroupMembershipResult.Error, true)]
        [TestCase(GroupMembershipResult.Member, false)]
        [TestCase(GroupMembershipResult.Invalid, false)]
        [TestCase(GroupMembershipResult.Skipped, false)]
        public void RunsOutcome_ForNotAMemberAndForCouldNotTell(GroupMembershipResult result, bool runs)
        {
            Assert.AreEqual(runs, GroupMembershipCheck.RunsOutcome(result));
        }

        [TestCase(GroupMembershipResult.Member)]
        [TestCase(GroupMembershipResult.NotMember)]
        [TestCase(GroupMembershipResult.Invalid)]
        [TestCase(GroupMembershipResult.Error)]
        public void ResultOf_TakesTheAppsAnswer(GroupMembershipResult answer)
        {
            Assert.AreEqual(answer, GroupMembershipCheck.ResultOf(Task.FromResult(answer)));
        }

        [Test]
        public void ResultOf_NoAnswerIsAnError()
        {
            Assert.AreEqual(GroupMembershipResult.Error, GroupMembershipCheck.ResultOf(null));
        }

        [Test]
        public void ResultOf_AnUnfinishedAnswerIsAnError()
        {
            // What the check sees when its wait for the app times out.
            var pending = new TaskCompletionSource<GroupMembershipResult>();
            Assert.AreEqual(GroupMembershipResult.Error, GroupMembershipCheck.ResultOf(pending.Task));
        }

        [Test]
        public void ResultOf_AFailedOrCancelledAnswerIsAnError()
        {
            var failed = Task.FromException<GroupMembershipResult>(new InvalidOperationException("offline"));
            Assert.AreEqual(GroupMembershipResult.Error, GroupMembershipCheck.ResultOf(failed));

            var cancelled = Task.FromCanceled<GroupMembershipResult>(new System.Threading.CancellationToken(true));
            Assert.AreEqual(GroupMembershipResult.Error, GroupMembershipCheck.ResultOf(cancelled));
        }

        [TestCase(GroupMembershipResult.Skipped)]
        [TestCase((GroupMembershipResult)42)]
        public void ResultOf_AnAnswerTheAppMayNotGiveIsAnError(GroupMembershipResult answer)
        {
            // Skipped is the check's own (no app to ask); an app that says it can't fall through to "let them in".
            Assert.AreEqual(GroupMembershipResult.Error, GroupMembershipCheck.ResultOf(Task.FromResult(answer)));
        }

        [TestCase(GroupMembershipResult.Member, "member")]
        [TestCase(GroupMembershipResult.NotMember, "not-member")]
        [TestCase(GroupMembershipResult.Error, "error")]
        [TestCase(GroupMembershipResult.Invalid, "invalid")]
        [TestCase(GroupMembershipResult.Skipped, "skipped")]
        [TestCase((GroupMembershipResult)42, "error")]
        public void StateOf_NamesEachResultForThePage(GroupMembershipResult result, string state)
        {
            Assert.AreEqual(state, BSVerifyGroup.StateOf(result));
        }
    }
}
