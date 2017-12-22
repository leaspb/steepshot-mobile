﻿using System;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Text.Method;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Refractored.Controls;
using Square.Picasso;
using Steepshot.Core;
using Steepshot.Core.Extensions;
using Steepshot.Core.Models.Common;
using Steepshot.Core.Models.Requests;
using Steepshot.Core.Presenters;
using Steepshot.Utils;

namespace Steepshot.Adapter
{
    public class CommentAdapter : RecyclerView.Adapter
    {
        private readonly CommentsPresenter _presenter;
        private readonly Context _context;
        private readonly Post _post;
        public Action<Post> LikeAction, UserAction, FlagAction, HideAction, ReplyAction;
        public Action RootClickAction;
        public Action<Post, VotersType> VotersClick;
        public Action<string> TagAction;

        public override int ItemCount => _presenter.Count + 1;

        public CommentAdapter(Context context, CommentsPresenter presenter, Post post)
        {
            _context = context;
            _presenter = presenter;
            _post = post;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var post = position == 0 ? _post : _presenter[position - 1];
            if (post == null)
                return;
            if (position == 0)
                (holder as PostDescriptionViewHolder)?.UpdateData(post, _context);
            else
                (holder as CommentViewHolder)?.UpdateData(post, _context);
        }

        public override int GetItemViewType(int position)
        {
            return position == 0 ? (int)ViewType.Post : (int)ViewType.Comment;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            switch ((ViewType)viewType)
            {
                case ViewType.Post:
                    {
                        var itemView = LayoutInflater.From(parent.Context)
                            .Inflate(Resource.Layout.lyt_description_item, parent, false);
                        var vh = new PostDescriptionViewHolder(itemView, UserAction, TagAction);
                        return vh;
                    }
                default:
                    {
                        var itemView = LayoutInflater.From(parent.Context)
                            .Inflate(Resource.Layout.lyt_comment_item, parent, false);
                        var vh = new CommentViewHolder(itemView, LikeAction, UserAction, VotersClick, FlagAction,
                            HideAction, ReplyAction, RootClickAction);
                        return vh;
                    }
            }
        }
    }

    public class PostDescriptionViewHolder : RecyclerView.ViewHolder
    {
        private readonly Action<string> _tagAction;
        private readonly Action<Post> _userAction;
        private readonly PostCustomTextView _title;
        private readonly ImageView _avatar;
        private readonly TextView _time;
        private readonly TextView _author;

        private Post _post;
        private Context _context;
        private const string _tagFormat = " #{0}";
        private const string tagToExclude = "steepshot";
        private const int _maxLines = 5;
        public PostDescriptionViewHolder(View itemView, Action<Post> userAction, Action<string> tagAction) : base(itemView)
        {
            _context = itemView.Context;
            _avatar = itemView.FindViewById<CircleImageView>(Resource.Id.avatar);
            _title = itemView.FindViewById<PostCustomTextView>(Resource.Id.first_comment);
            _time = itemView.FindViewById<TextView>(Resource.Id.time);
            _author = itemView.FindViewById<TextView>(Resource.Id.sender_name);

            _author.Typeface = Style.Semibold;
            _time.Typeface = Style.Regular;
            _title.Typeface = Style.Regular;

            _userAction = userAction;
            _tagAction = tagAction;

            _avatar.Click += AvatarOnClick;
            _title.MovementMethod = new LinkMovementMethod();
            _title.SetHighlightColor(Color.Transparent);
            _title.Click += TitleOnClick;
            _title.TagAction = tagAction;
            if (_title.OnMeasureInvoked == null)
            {
                _title.OnMeasureInvoked += OnTitleOnMeasureInvoked;
            }
        }

        private void AvatarOnClick(object sender, EventArgs eventArgs)
        {
            _userAction?.Invoke(_post);
        }

        private void TitleOnClick(object sender, EventArgs eventArgs)
        {
            _post.IsExpanded = true;
            _tagAction?.Invoke(null);
        }

        private void OnTitleOnMeasureInvoked()
        {
            _title.UpdateText(_post, tagToExclude, _tagFormat, _maxLines);
        }

        public void UpdateData(Post post, Context context)
        {
            _post = post;
            if (!string.IsNullOrEmpty(_post.Avatar))
                Picasso.With(context).Load(_post.Avatar).Placeholder(Resource.Drawable.ic_holder).Resize(300, 0).Priority(Picasso.Priority.Low).Into(_avatar, null, OnPicassoError);
            else
                Picasso.With(context).Load(Resource.Drawable.ic_holder).Into(_avatar);

            _author.Text = post.Author;
            _time.Text = post.Created.ToPostTime();
            _title.UpdateText(_post, tagToExclude, _tagFormat, _maxLines);
        }

        private void OnPicassoError()
        {
            Picasso.With(_context).Load(_post.Avatar).Placeholder(Resource.Drawable.ic_holder).NoFade().Into(_avatar);
        }
    }

    public class CommentViewHolder : RecyclerView.ViewHolder, ITarget
    {
        private readonly ImageView _avatar;
        private readonly TextView _author;
        private readonly TextView _comment;
        private readonly TextView _likes;
        private readonly TextView _flags;
        private readonly TextView _cost;
        private readonly TextView _reply;
        private readonly TextView _time;
        private readonly ImageButton _likeOrFlag;
        private readonly ImageButton _more;
        private readonly Action<Post> _likeAction;
        private readonly Action<Post> _userAction;
        private readonly Action<Post> _flagAction;
        private readonly Action<Post> _hideAction;
        private readonly Action<Post> _replyAction;
        private readonly Action<Post, VotersType> _votersAction;
        private readonly Action _rootAction;
        private readonly Animation _likeSetAnimation;
        private readonly Animation _likeWaitAnimation;
        private readonly BottomSheetDialog _moreActionsDialog;
        private readonly Context _context;
        private readonly RelativeLayout _rootView;

        private Post _post;

        public CommentViewHolder(View itemView, Action<Post> likeAction, Action<Post> userAction, Action<Post, VotersType> votersAction, Action<Post> flagAction, Action<Post> hideAction, Action<Post> replyAction, Action rootClickAction) : base(itemView)
        {
            _avatar = itemView.FindViewById<Refractored.Controls.CircleImageView>(Resource.Id.avatar);
            _author = itemView.FindViewById<TextView>(Resource.Id.sender_name);
            _comment = itemView.FindViewById<TextView>(Resource.Id.comment_text);
            _likes = itemView.FindViewById<TextView>(Resource.Id.likes);
            _flags = itemView.FindViewById<TextView>(Resource.Id.flags);
            _cost = itemView.FindViewById<TextView>(Resource.Id.cost);
            _likeOrFlag = itemView.FindViewById<ImageButton>(Resource.Id.like_btn);
            _reply = itemView.FindViewById<TextView>(Resource.Id.reply_btn);
            _time = itemView.FindViewById<TextView>(Resource.Id.time);
            _more = itemView.FindViewById<ImageButton>(Resource.Id.more);
            _rootView = itemView.FindViewById<RelativeLayout>(Resource.Id.root_view);

            _author.Typeface = Style.Semibold;
            _comment.Typeface = _likes.Typeface = _cost.Typeface = _reply.Typeface = Style.Regular;

            _likeAction = likeAction;
            _userAction = userAction;
            _flagAction = flagAction;
            _hideAction = hideAction;
            _replyAction = replyAction;
            _rootAction = rootClickAction;
            _votersAction = votersAction;

            _likeOrFlag.Click += Like_Click;
            _avatar.Click += UserAction;
            _author.Click += UserAction;
            _cost.Click += UserAction;
            _more.Click += DoMoreAction;
            _reply.Click += ReplyAction;
            _rootView.Click += Root_Click;
            _likes.Click += DoLikersAction;
            _flags.Click += DoFlagersAction;

            _context = itemView.RootView.Context;
            _likeSetAnimation = AnimationUtils.LoadAnimation(_context, Resource.Animation.like_set);
            _likeSetAnimation.AnimationStart += LikeAnimationStart;
            _likeSetAnimation.AnimationEnd += LikeAnimationEnd;
            _likeWaitAnimation = AnimationUtils.LoadAnimation(_context, Resource.Animation.like_wait);

            _moreActionsDialog = new BottomSheetDialog(_context);
            _moreActionsDialog.Window.RequestFeature(WindowFeatures.NoTitle);

            _more.Visibility = BasePresenter.User.IsAuthenticated ? ViewStates.Visible : ViewStates.Invisible;
            _reply.Visibility = BasePresenter.User.IsAuthenticated ? ViewStates.Visible : ViewStates.Gone;
        }

        private void DoMoreAction(object sender, EventArgs e)
        {
            var inflater = (LayoutInflater)_context.GetSystemService(Context.LayoutInflaterService);
            using (var dialogView = inflater.Inflate(Resource.Layout.lyt_feed_popup, null))
            {
                dialogView.SetMinimumWidth((int)(ItemView.Width * 0.8));
                var flag = dialogView.FindViewById<Button>(Resource.Id.flag);
                flag.Text = _post.Flag ? Localization.Texts.UnFlagComment : Localization.Texts.FlagComment;
                flag.Typeface = Style.Semibold;
                var hide = dialogView.FindViewById<Button>(Resource.Id.hide);
                hide.Text = Localization.Texts.HidePost;
                hide.Typeface = Style.Semibold;
                if (_post.Author == BasePresenter.User.Login)
                    flag.Visibility = hide.Visibility = ViewStates.Gone;
                var cancel = dialogView.FindViewById<Button>(Resource.Id.cancel);
                cancel.Text = Localization.Texts.Cancel;
                cancel.Typeface = Style.Semibold;

                flag.Click -= DoFlagAction;
                flag.Click += DoFlagAction;

                hide.Click -= DoHideAction;
                hide.Click += DoHideAction;

                cancel.Click -= DoDialogCancelAction;
                cancel.Click += DoDialogCancelAction;

                _moreActionsDialog.SetContentView(dialogView);
                dialogView.SetBackgroundColor(Color.Transparent);
                _moreActionsDialog.Window.FindViewById(Resource.Id.design_bottom_sheet).SetBackgroundColor(Color.Transparent);
                _moreActionsDialog.Show();
            }
        }

        private void DoFlagAction(object sender, EventArgs e)
        {
            _moreActionsDialog.Dismiss();
            if (!BasePostPresenter.IsEnableVote)
                return;

            _flagAction.Invoke(_post);
        }

        private void DoHideAction(object sender, EventArgs e)
        {
            _moreActionsDialog.Dismiss();
            _hideAction.Invoke(_post);
        }

        private void DoDialogCancelAction(object sender, EventArgs e)
        {
            _moreActionsDialog.Dismiss();
        }

        private void LikeAnimationStart(object sender, Animation.AnimationStartEventArgs e)
        {
            _likeOrFlag.SetImageResource(Resource.Drawable.ic_new_like_filled);
        }

        private void LikeAnimationEnd(object sender, Animation.AnimationEndEventArgs e)
        {
            _likeOrFlag.StartAnimation(_likeWaitAnimation);
        }

        private void UserAction(object sender, EventArgs e)
        {
            _userAction?.Invoke(_post);
        }

        private void ReplyAction(object sender, EventArgs e)
        {
            _replyAction?.Invoke(_post);
        }

        private void DoLikersAction(object sender, EventArgs e)
        {
            _votersAction?.Invoke(_post, VotersType.Likes);
        }

        private void DoFlagersAction(object sender, EventArgs e)
        {
            _votersAction?.Invoke(_post, VotersType.Flags);
        }

        private void Like_Click(object sender, EventArgs e)
        {
            if (!BasePostPresenter.IsEnableVote)
                return;
            if (_post.Flag)
                _flagAction?.Invoke(_post);
            else
                _likeAction?.Invoke(_post);
        }

        private void Root_Click(object sender, EventArgs e)
        {
            _rootAction?.Invoke();
        }

        public void UpdateData(Post post, Context context)
        {
            _post = post;
            _author.Text = post.Author;
            _comment.Text = post.Body.CensorText();

            if (_post.Author == BasePresenter.User.Login)
                _more.Visibility = ViewStates.Gone;

            if (!string.IsNullOrEmpty(_post.Avatar))
                Picasso.With(_context).Load(_post.Avatar)
                       .Placeholder(Resource.Drawable.ic_holder)
                       .NoFade()
                       .Resize(300, 0)
                       .Priority(Picasso.Priority.Normal)
                       .Into(_avatar, OnSuccess, OnError);
            else
                Picasso.With(context).Load(Resource.Drawable.ic_holder).Into(_avatar);

            _likeOrFlag.ClearAnimation();
            if (!BasePostPresenter.IsEnableVote)
            {
                if (post.VoteChanging)
                    _likeOrFlag.StartAnimation(_likeSetAnimation);
                else if (post.FlagChanging)
                    _likeOrFlag.SetImageResource(Resource.Drawable.ic_flag);
            }
            else
            {
                if (post.Vote || !post.Flag)
                {
                    _likeOrFlag.SetImageResource(post.Vote
                        ? Resource.Drawable.ic_new_like_filled
                        : Resource.Drawable.ic_new_like_selected);
                }
                else
                {
                    _likeOrFlag.SetImageResource(Resource.Drawable.ic_flag_active);
                }
            }

            _likes.Text = $"{post.NetLikes} {(_post.NetLikes == 1 ? Localization.Messages.Like : Localization.Messages.Likes)}";
            if (post.NetFlags > 0)
            {
                _flags.Visibility = ViewStates.Visible;
                _flags.Text = $"{post.NetFlags} {(_post.NetFlags == 1 ? Localization.Messages.Flag : Localization.Messages.Flags)}";
            }
            else
                _flags.Visibility = ViewStates.Gone;
            _cost.Text = BasePresenter.ToFormatedCurrencyString(post.TotalPayoutReward);
            _time.Text = post.Created.ToPostTime();
        }

        private void OnSuccess()
        {
        }

        private void OnError()
        {
            Picasso.With(_context).Load(_post.Avatar).NoFade().Into(this);
        }

        public void OnBitmapFailed(Drawable p0)
        {
        }

        public void OnBitmapLoaded(Bitmap p0, Picasso.LoadedFrom p1)
        {
            _avatar.SetImageBitmap(p0);
        }

        public void OnPrepareLoad(Drawable p0)
        {
        }
    }


}
