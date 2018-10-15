﻿using System;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using GitHub.App.Models.Drafts;
using GitHub.Models;
using GitHub.Services;
using GitHub.ViewModels;
using NSubstitute;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace GitHub.App.UnitTests.ViewModels
{
    public class CommentThreadViewModelTests
    {
        [Test]
        public async Task SavesDraftForEditingComment()
        {
            using (TestUtils.WithScheduler(Scheduler.CurrentThread))
            {
                var scheduler = new HistoricalScheduler();
                var drafts = Substitute.For<IMessageDraftStore>();
                var target = CreateTarget(drafts: drafts, scheduler: scheduler);

                await target.AddPlaceholder(true);
                target.Comments[0].Body = "Edited comment.";

                await drafts.DidNotReceiveWithAnyArgs().UpdateDraft<CommentDraft>(null, null, null);

                scheduler.AdvanceBy(TimeSpan.FromSeconds(1));

                await drafts.Received().UpdateDraft(
                    "file.cs",
                    "10",
                    Arg.Is<CommentDraft>(x => x.Body == "Edited comment."));
            }
        }

        [Test]
        public async Task DoesntSaveDraftForNonEditingComment()
        {
            using (TestUtils.WithScheduler(Scheduler.CurrentThread))
            {
                var scheduler = new HistoricalScheduler();
                var drafts = Substitute.For<IMessageDraftStore>();
                var target = CreateTarget(drafts: drafts, scheduler: scheduler);

                await target.AddPlaceholder(false);
                target.Comments[0].Body = "Edited comment.";

                scheduler.AdvanceBy(TimeSpan.FromSeconds(1));

                await drafts.DidNotReceiveWithAnyArgs().UpdateDraft<CommentDraft>(null, null, null);
            }
        }

        static Target CreateTarget(
            IMessageDraftStore drafts = null,
            IScheduler scheduler = null)
        {
            drafts = drafts ?? Substitute.For<IMessageDraftStore>();
            scheduler = scheduler ?? DefaultScheduler.Instance;

            return new Target(drafts, scheduler);
        }

        class Target : CommentThreadViewModel
        {
            public Target(IMessageDraftStore drafts, IScheduler scheduler)
                : base(drafts, scheduler)
            {
            }

            public async Task AddPlaceholder(bool isEditing)
            {
                var c = new TestComment();
                await c.InitializeAsPlaceholderAsync(this, isEditing);
                Comments.Add(c);
            }

            public override Task DeleteComment(int pullRequestId, int commentId) => throw new NotImplementedException();
            public override Task EditComment(string id, string body) => throw new NotImplementedException();
            public override Task PostComment(string body) => throw new NotImplementedException();
            protected override (string key, string secondaryKey) GetDraftKeys(ICommentViewModel comment) => ("file.cs", "10");
        }

        class TestComment : CommentViewModel
        {
            public TestComment()
                : base(Substitute.For<ICommentService>())
            {
            }

            /// <inheritdoc/>
            public async Task InitializeAsPlaceholderAsync(
                ICommentThreadViewModel thread,
                bool isEditing)
            {
                await InitializeAsync(
                    thread,
                    new ActorModel(),
                    null,
                    isEditing ? CommentEditState.Editing : CommentEditState.Placeholder).ConfigureAwait(true);
            }
        }
    }
}