// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//

using Foundation;

namespace Steepshot.iOS.Views
{
	[Register ("DescriptionViewController")]
	partial class DescriptionViewController
	{
		[Outlet]
		UIKit.NSLayoutConstraint collectionHeight { get; set; }

		[Outlet]
		UIKit.UITextView descriptionTextField { get; set; }

		[Outlet]
		UIKit.UIView loadingView { get; set; }

		[Outlet]
		UIKit.UIImageView photoView { get; set; }

		[Outlet]
		UIKit.UIButton postPhotoButton { get; set; }

		[Outlet]
		UIKit.UIScrollView scrollView { get; set; }

		[Outlet]
		UIKit.UICollectionView tagsCollectionView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (tagsCollectionView != null) {
				tagsCollectionView.Dispose ();
				tagsCollectionView = null;
			}

			if (photoView != null) {
				photoView.Dispose ();
				photoView = null;
			}

			if (postPhotoButton != null) {
				postPhotoButton.Dispose ();
				postPhotoButton = null;
			}

			if (descriptionTextField != null) {
				descriptionTextField.Dispose ();
				descriptionTextField = null;
			}

			if (scrollView != null) {
				scrollView.Dispose ();
				scrollView = null;
			}

			if (collectionHeight != null) {
				collectionHeight.Dispose ();
				collectionHeight = null;
			}

			if (loadingView != null) {
				loadingView.Dispose ();
				loadingView = null;
			}
		}
	}
}