﻿using System;
using System.Linq;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using Sweetshot.Library.Models.Requests;
using Sweetshot.Library.Models.Responses;
using UIKit;

namespace Steepshot.iOS
{
	public partial class CommentsViewController : BaseViewController
	{
		protected CommentsViewController(IntPtr handle) : base(handle)
		{
			// Note: this .ctor should not contain any initialization logic
		}

		private CommentsTableViewSource tableSource = new CommentsTableViewSource();
		public string PostUrl;

		private nfloat scroll_amount = 0.0f;
		private bool moveViewUp = false;

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			NavigationController.SetNavigationBarHidden(false, false);

			commentsTable.Source = tableSource;
			commentsTable.LayoutMargins = UIEdgeInsets.Zero;
			commentsTable.RegisterClassForCellReuse(typeof(CommentTableViewCell), nameof(CommentTableViewCell));
			commentsTable.RegisterNibForCellReuse(UINib.FromName(nameof(CommentTableViewCell), NSBundle.MainBundle), nameof(CommentTableViewCell));

			tableSource.Voted += (vote, url, action)  =>
            {
				Vote(vote, url, action);
            };

			tableSource.GoToProfile += (username)  =>
            {
				var myViewController = Storyboard.InstantiateViewController(nameof(ProfileViewController)) as ProfileViewController;
				myViewController.Username = username;
				NavigationController.PushViewController(myViewController, true);
            };

			commentsTable.RowHeight = UITableView.AutomaticDimension;
			commentsTable.EstimatedRowHeight = 150f;
			commentTextView.Delegate = new TextViewDelegate();

			sendButton.TouchDown += (sender, e) =>
			{
				CreateComment();
			};

			GetComments();
		}

		public async Task GetComments()
		{
			progressBar.StartAnimating();
			try
			{
				var request = new GetCommentsRequest(PostUrl) { SessionId = UserContext.Instanse.Token };
				var result = await Api.GetComments(request);
				tableSource.TableItems.Clear();
				tableSource.TableItems.AddRange(result.Result.Results);
				commentsTable.ReloadData();
				//kostil?
				commentsTable.SetContentOffset(new CGPoint(0, commentsTable.ContentSize.Height - commentsTable.Frame.Height), false);
				await Task.Delay(TimeSpan.FromMilliseconds(10));
				commentsTable.SetContentOffset(new CGPoint(0, commentsTable.ContentSize.Height - commentsTable.Frame.Height), false);
			}
			catch (Exception ex)
			{

			}
			finally
			{
				progressBar.StopAnimating();
			}
		}

		public async Task Vote(bool vote, string postUrl, Action<string, VoteResponse> action)
		{
			/*if (!UserPrincipal.Instance.IsAuthenticated)
			return new OperationResult<VoteResponse> { Errors = new List<string> { "Forbidden" }, Success = false };*/
			try
			{
				int diezid = postUrl.IndexOf('#');
				string posturl = postUrl.Substring(diezid + 1);

				var voteRequest = new VoteRequest(UserContext.Instanse.Token, vote, posturl);
				var response = await Api.Vote(voteRequest);
				if (response.Success)
				{
					tableSource.TableItems.First(p => p.Url == postUrl).Vote = vote;
					action.Invoke(postUrl, response.Result);
				}
			}
			catch (Exception ex)
			{
				
			}
		}

		public async Task CreateComment()
		{
			try
			{
				var reqv = new CreateCommentRequest(UserContext.Instanse.Token, PostUrl, commentTextView.Text, commentTextView.Text);
				var response = await Api.CreateComment(reqv);
				if (response.Success)
				{
					commentTextView.Text = string.Empty;
					await GetComments();
				}
			}
			catch (Exception ex)
			{
				
			}
		}

		protected override void KeyBoardUpNotification(NSNotification notification)
		{
			CGRect r = UIKeyboard.FrameBeginFromNotification(notification);
			scroll_amount = r.Height;
			if (scroll_amount > 0)
			{
				moveViewUp = true;
				ScrollTheView(moveViewUp);
			}
			else
				moveViewUp = false;
		}

		protected override void KeyBoardDownNotification(NSNotification notification)
		{
			if (moveViewUp)
				ScrollTheView(false);
		}

		protected override void ScrollTheView(bool move)
		{
			UIView.BeginAnimations(string.Empty, System.IntPtr.Zero);
			UIView.SetAnimationDuration(0.1);
			CGRect frame = View.Frame;
			if (move)
				frame.Y -= scroll_amount;
			else
			{
				frame.Y += scroll_amount;
				scroll_amount = 0;
			}
			View.Frame = frame;
			UIView.CommitAnimations();
		}

		class TextViewDelegate : UITextViewDelegate
		{
			public override bool ShouldChangeText(UITextView textView, NSRange range, string text)
			{
				if (text == "\n")
				{
					textView.ResignFirstResponder();
					return false;
				}
				return true;
			}
		}
	}
}
