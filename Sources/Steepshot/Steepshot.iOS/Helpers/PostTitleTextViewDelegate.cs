﻿using System;
using Foundation;
using UIKit;

namespace Steepshot.iOS.Helpers
{
    public class BaseTextViewDelegate : UITextViewDelegate
    {
        public UILabel Placeholder;
        public Action EditingStartedAction;

        public override void EditingStarted(UITextView textView)
        {
            EditingStartedAction?.Invoke();
            Placeholder.Hidden = true;
        }

        public override void EditingEnded(UITextView textView)
        {
            if (textView.Text.Length > 0)
                Placeholder.Hidden = true;
            else
                Placeholder.Hidden = false;
        }
    }

    public class PostTitleTextViewDelegate : BaseTextViewDelegate
    {
        public override bool ShouldChangeText(UITextView textView, NSRange range, string text)
        {
            /*
            if (text == "\n")
            {
                textView.ResignFirstResponder();
                return false;
            }*/
            if ((textView.Text + text).Length > 255)
                return false;
            return true;
        }
    }
}
