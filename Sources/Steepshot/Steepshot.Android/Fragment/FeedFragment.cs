using System;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Com.Lilarcor.Cheeseknife;
using Steepshot.Activity;
using Steepshot.Adapter;
using Steepshot.Base;
using Steepshot.Core.Localization;
using Steepshot.Core.Models.Common;
using Steepshot.Core.Presenters;
using Steepshot.Utils;
using Steepshot.Core.Models;
using Steepshot.Core.Models.Enums;
using Steepshot.Interfaces;
using Steepshot.Core.Utils;

namespace Steepshot.Fragment
{
    public sealed class FeedFragment : BaseFragmentWithPresenter<FeedPresenter>, ICanOpenPost
    {
        public const string PostUrlExtraPath = "url";
        public const string PostNetVotesExtraPath = "count";

        private FeedAdapter<FeedPresenter> _adapter;
        private PostPagerAdapter<FeedPresenter> _postPagerAdapter;
        private ScrollListener _scrollListner;

#pragma warning disable 0649, 4014
        [InjectView(Resource.Id.feed_list)] private RecyclerView _feedList;
        [InjectView(Resource.Id.loading_spinner)] private ProgressBar _bar;
        [InjectView(Resource.Id.feed_refresher)] private SwipeRefreshLayout _refresher;
        [InjectView(Resource.Id.logo)] private ImageView _logo;
        [InjectView(Resource.Id.app_bar)] private AppBarLayout _toolbar;
        [InjectView(Resource.Id.empty_query_label)] private TextView _emptyQueryLabel;
        [InjectView(Resource.Id.post_prev_pager)] private ViewPager _postPager;
#pragma warning restore 0649


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (!IsInitialized)
            {
                InflatedView = inflater.Inflate(Resource.Layout.lyt_feed, null);
                Cheeseknife.Inject(this, InflatedView);
            }
            ToggleTabBar();
            return InflatedView;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            if (!IsInitialized)
            {
                base.OnViewCreated(view, savedInstanceState);

                Presenter.SourceChanged += PresenterSourceChanged;
                _adapter = new FeedAdapter<FeedPresenter>(Context, Presenter);
                _adapter.PostAction += PostAction;
                _adapter.TagAction += TagAction;

                _postPagerAdapter = new PostPagerAdapter<FeedPresenter>(Context, Presenter);
                _postPagerAdapter.PostAction += PostAction;
                _postPagerAdapter.TagAction += TagAction;
                _postPagerAdapter.CloseAction += CloseAction;

                _logo.Click += OnLogoClick;
                _toolbar.OffsetChanged += OnToolbarOffsetChanged;

                _scrollListner = new ScrollListener();
                _scrollListner.ScrolledToBottom += LoadPosts;

                _refresher.Refresh += OnRefresh;

                _feedList.SetAdapter(_adapter);
                _feedList.SetLayoutManager(new LinearLayoutManager(Android.App.Application.Context));
                _feedList.AddOnScrollListener(_scrollListner);

                _postPager.SetClipToPadding(false);
                var pagePadding = (int)BitmapUtils.DpToPixel(20, Resources);
                _postPager.SetPadding(pagePadding, 0, pagePadding, 0);
                _postPager.PageMargin = pagePadding / 2;
                _postPager.PageScrollStateChanged += PostPagerOnPageScrollStateChanged;
                _postPager.PageScrolled += PostPagerOnPageScrolled;
                _postPager.Adapter = _postPagerAdapter;
                _postPager.SetPageTransformer(false, _postPagerAdapter, (int)LayerType.None);

                _emptyQueryLabel.Typeface = Style.Light;
                _emptyQueryLabel.Text = AppSettings.LocalizationManager.GetText(LocalizationKeys.EmptyCategory);

                LoadPosts();
            }

            var postUrl = Activity?.Intent?.GetStringExtra(CommentsFragment.ResultString);
            if (!string.IsNullOrWhiteSpace(postUrl))
            {
                var count = Activity.Intent.GetIntExtra(CommentsFragment.CountString, 0);
                Activity.Intent.RemoveExtra(CommentsFragment.ResultString);
                Activity.Intent.RemoveExtra(CommentsFragment.CountString);
                var post = Presenter.FirstOrDefault(p => p.Url == postUrl);
                post.Children += count;
                _adapter.NotifyDataSetChanged();
            }
        }

        public override void OnResume()
        {
            base.OnResume();
            if (_postPager.Visibility == ViewStates.Visible)
                if (Activity is RootActivity activity)
                    activity._tabLayout.Visibility = ViewStates.Invisible;
        }

        private void PostPagerOnPageScrolled(object sender, ViewPager.PageScrolledEventArgs pageScrolledEventArgs)
        {
            if (pageScrolledEventArgs.Position == Presenter.Count)
            {
                if (!Presenter.IsLastReaded)
                    LoadPosts();
                else
                    _postPagerAdapter.NotifyDataSetChanged();
            }
        }

        private void PostPagerOnPageScrollStateChanged(object sender, ViewPager.PageScrollStateChangedEventArgs pageScrollStateChangedEventArgs)
        {
            if (pageScrollStateChangedEventArgs.State == 0)
            {
                _postPagerAdapter.CurrentItem = _postPager.CurrentItem;
                _feedList.ScrollToPosition(_postPager.CurrentItem);
                if (_feedList.GetLayoutManager() is GridLayoutManager manager)
                {
                    var positionToScroll = _postPager.CurrentItem + (_postPager.CurrentItem - manager.FindFirstVisibleItemPosition()) / 2;
                    _feedList.ScrollToPosition(positionToScroll < Presenter.Count
                        ? positionToScroll
                        : Presenter.Count);
                }
            }
        }

        private void OnToolbarOffsetChanged(object sender, AppBarLayout.OffsetChangedEventArgs e)
        {
            ViewCompat.SetElevation(_toolbar, BitmapUtils.DpToPixel(2, Resources));
        }

        public override void OnDetach()
        {
            base.OnDetach();
            Cheeseknife.Reset(this);
        }

        private void OnLogoClick(object sender, EventArgs e)
        {
            _feedList.ScrollToPosition(0);
        }

        private void PresenterSourceChanged(Status status)
        {
            if (!IsInitialized)
                return;

            Activity.RunOnUiThread(() =>
            {
                _adapter.NotifyDataSetChanged();
                _postPagerAdapter.NotifyDataSetChanged();
            });
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            _scrollListner.ClearPosition();
            Presenter.Clear();
            LoadPosts();
        }

        private async void LoadPosts()
        {
            var error = await Presenter.TryLoadNextTopPosts();
            if (!IsInitialized)
                return;

            Context.ShowAlert(error);

            _bar.Visibility = ViewStates.Gone;
            _refresher.Refreshing = false;

            _emptyQueryLabel.Visibility = Presenter.Count > 0 ? ViewStates.Invisible : ViewStates.Visible;
        }

        private void PhotoClick(Post post)
        {
            if (post == null)
                return;

            var intent = new Intent(Context, typeof(PostPreviewActivity));
            intent.PutExtra(PostPreviewActivity.PhotoExtraPath, post.Media[0].Url);
            StartActivity(intent);
        }

        private void FeedPhotoClick(Post post)
        {
            if (post == null)
                return;

            OpenPost(post);
        }

        public void OpenPost(Post post)
        {
            if (Activity is RootActivity activity)
                activity._tabLayout.Visibility = ViewStates.Gone;
            _postPager.SetCurrentItem(Presenter.IndexOf(post), false);
            _postPagerAdapter.CurrentItem = _postPager.CurrentItem;
            _postPagerAdapter.NotifyDataSetChanged();
            _postPager.Visibility = ViewStates.Visible;
            _feedList.Visibility = ViewStates.Gone;
        }

        public bool ClosePost()
        {
            if (_postPager.Visibility == ViewStates.Visible)
            {
                if (Activity is RootActivity activity)
                    activity._tabLayout.Visibility = ViewStates.Visible;
                _feedList.ScrollToPosition(_postPager.CurrentItem);
                _postPager.Visibility = ViewStates.Gone;
                _feedList.Visibility = ViewStates.Visible;
                _feedList.GetAdapter().NotifyDataSetChanged();
                return true;
            }
            return false;
        }

        private async void PostAction(ActionType type, Post post)
        {
            switch (type)
            {
                case ActionType.Like:
                    {
                        if (!BasePresenter.User.IsAuthenticated)
                            return;

                        var error = await Presenter.TryVote(post);
                        if (!IsInitialized)
                            return;

                        if (error == null && Activity is RootActivity root)
                            root.TryUpdateProfile();

                        Context.ShowAlert(error);
                        break;
                    }
                case ActionType.VotersLikes:
                case ActionType.VotersFlags:
                    {
                        if (post == null)
                            return;

                        var isLikers = type == ActionType.VotersLikes;
                        Activity.Intent.PutExtra(PostUrlExtraPath, post.Url);
                        Activity.Intent.PutExtra(PostNetVotesExtraPath, isLikers ? post.NetLikes : post.NetFlags);
                        Activity.Intent.PutExtra(VotersFragment.VotersType, isLikers);
                        ((BaseActivity)Activity).OpenNewContentFragment(new VotersFragment());
                        break;
                    }
                case ActionType.Comments:
                    {
                        if (post == null)
                            return;

                        ((BaseActivity)Activity).OpenNewContentFragment(new CommentsFragment(post, post.Children == 0));
                        break;
                    }
                case ActionType.Profile:
                    {
                        if (post == null)
                            return;

                        ((BaseActivity)Activity).OpenNewContentFragment(new ProfileFragment(post.Author));
                        break;
                    }
                case ActionType.Flag:
                    {
                        if (!BasePresenter.User.IsAuthenticated)
                            return;

                        var error = await Presenter.TryFlag(post);
                        if (!IsInitialized)
                            return;

                        if (error == null && Activity is RootActivity root)
                            root.TryUpdateProfile();

                        Context.ShowAlert(error);
                        break;
                    }
                case ActionType.Hide:
                    {
                        Presenter.HidePost(post);
                        break;
                    }
                case ActionType.Delete:
                    {
                        var error = await Presenter.TryDeletePost(post);
                        if (!IsInitialized)
                            return;

                        Context.ShowAlert(error);
                        break;
                    }
                case ActionType.Share:
                    {
                        var shareIntent = new Intent(Intent.ActionSend);
                        shareIntent.SetType("text/plain");
                        shareIntent.PutExtra(Intent.ExtraSubject, post.Title);
                        shareIntent.PutExtra(Intent.ExtraText, AppSettings.LocalizationManager.GetText(LocalizationKeys.PostLink, post.Url));
                        StartActivity(Intent.CreateChooser(shareIntent, AppSettings.LocalizationManager.GetText(LocalizationKeys.Sharepost)));
                        break;
                    }
                case ActionType.Photo:
                    {
                        OpenPost(post);
                        break;
                    }
                case ActionType.Preview:
                    {
                        if (post == null)
                            return;

                        var intent = new Intent(Context, typeof(PostPreviewActivity));
                        intent.PutExtra(PostPreviewActivity.PhotoExtraPath, post.Media[0].Url);
                        StartActivity(intent);
                        break;
                    }
            }
        }

        private void TagAction(string tag)
        {
            if (tag != null)
            {
                Activity.Intent.PutExtra(SearchFragment.SearchExtra, tag);
                ((BaseActivity)Activity).OpenNewContentFragment(new PreSearchFragment());
            }
            else
                _adapter.NotifyDataSetChanged();
        }

        private void CloseAction()
        {
            ClosePost();
        }
    }
}
