﻿using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.V4.App;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Views.InputMethods;
using Android.Widget;
using System;
using System.Collections.Generic;
using AContRes_O = Android.Content.Res.Orientation;
using AUComplexUnitType = Android.Util.ComplexUnitType;
using AWD_O = Android.Widget.Orientation;
using OSBund = Android.OS.Bundle;
using V4FragmentManager = Android.Support.V4.App.FragmentManager;
using V4FragmentTransaction = Android.Support.V4.App.FragmentTransaction;

namespace Xamarin.ActionSheet
{
    public class ActionSheet : Fragment, View.IOnClickListener
    {
        public enum ThemeType {
            IOS6,IOS7,Matera
        }
        private static readonly int CANCEL_BUTTON_ID = 100;
        private static readonly int BG_VIEW_ID = 10;
        private static readonly int TRANSLATE_DURATION = 200;
        private static readonly int ALPHA_DURATION = 300;

        private static readonly String EXTRA_DISMISSED = "extra_dismissed";

        public string Cancel_Button_Title
        {
            get;
            set;
        } = "取消";

        public List<string> Other_Button_Title
        {
            get;
            set;
        } = new List<string>() { "确定" };
        /// <summary>
        /// 取消按钮是否与其他按钮分隔开
        /// </summary>
        public bool Cancel_On_Touch_Outside
        {
            get;
            set;
        } = true;

        public ThemeType SheetTheme { get; set; } = ThemeType.IOS7;

        private bool dismissed = true;
        public bool Dismissed
        {
            get { return dismissed; }
        }

        private IActionSheetListener mListener;
        private View view;
        private LinearLayout panel;
        private ViewGroup viewGroup;
        private View viewBg;
        private Attributes attrs;
        private bool isCancel = true;

        public ActionSheet() : base() { }

        public override void OnSaveInstanceState(OSBund outState)
        {
            outState.PutBoolean(EXTRA_DISMISSED, dismissed);
        }
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (savedInstanceState != null)
            {
                dismissed = savedInstanceState.GetBoolean(EXTRA_DISMISSED);
            }
            SetTheme();
        }
        /// <summary>
        /// 设置主题为IOS6样式
        /// </summary>
        public void SetTheme() {
            switch (this.SheetTheme)
            {
                case ThemeType.IOS6:
                    Activity.SetTheme(Resource.Style.ActionSheetStyleiOS6);
                    break;
                case ThemeType.IOS7:
                    Activity.SetTheme(Resource.Style.ActionSheetStyleiOS7);
                    break;
                case ThemeType.Matera:
                    Activity.SetTheme(Resource.Style.ActionSheetStyleMatera);
                    break;
                default:
                    Activity.SetTheme(Resource.Style.ActionSheetStyleiOS7);
                    break;
            }

        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, OSBund savedInstanceState)
        {
            InputMethodManager imm = Activity.GetSystemService(Context.InputMethodService) as InputMethodManager;
            if (imm!=null&&imm.IsActive)
            {
                View focusView = Activity.CurrentFocus;
                if (focusView != null)
                {
                    imm.HideSoftInputFromWindow(focusView.WindowToken, 0);
                }
            }

            attrs = ReadAttribute();

            view = CreateView();
            viewGroup = (ViewGroup)Activity.Window.DecorView;

            CreateItems();

            viewGroup.AddView(view);
            viewBg.StartAnimation(CreateAlphaInAnimation());
            panel.StartAnimation(CreateTranslationInAnimation());
            return base.OnCreateView(inflater, container, savedInstanceState);
        }

        public override void OnDestroyView()
        {
            panel.StartAnimation(CreateTranslationOutAnimation());
            viewBg.PostDelayed(() => {
                viewGroup.RemoveView(view);
            }, ALPHA_DURATION);
            if (mListener != null)
            {
                mListener.onDismiss(this, isCancel);
            }
            base.OnDestroyView();
        }

        public void Show(V4FragmentManager manager)
        {
            if (!dismissed || manager.IsDestroyed)
            {
                return;
            }
            dismissed = false;
            new Handler().Post(() => {
                V4FragmentTransaction ft = manager.BeginTransaction();
                ft.Add(this, "actionSheet");
                ft.AddToBackStack(null);
                ft.CommitAllowingStateLoss();
            });

        }

        public void Dismiss()
        {
            if (dismissed)
            {
                return;
            }
            dismissed = true;
            new Handler().Post(() => {
                FragmentManager.PopBackStack();
                V4FragmentTransaction ft = FragmentManager.BeginTransaction();
                ft.Remove(this);
                ft.CommitAllowingStateLoss();
            });
        }


        #region 创建动画效果

        private Animation CreateTranslationInAnimation()
        {
            var type = Dimension.RelativeToSelf;
            var an = new TranslateAnimation(type, 0, type, 0, type,
                         1, type, 0);
            an.Duration = TRANSLATE_DURATION;
            return an;
        }

        private Animation CreateAlphaInAnimation()
        {
            var an = new AlphaAnimation(0, 1);
            an.Duration = ALPHA_DURATION;
            return an;
        }

        private Animation CreateTranslationOutAnimation()
        {
            var type = Dimension.RelativeToSelf;
            var an = new TranslateAnimation(type, 0, type, 0, type,
                         0, type, 1);
            an.Duration = TRANSLATE_DURATION;
            an.FillAfter = true;
            return an;
        }

        private Animation CreateAlphaOutAnimation()
        {
            var an = new AlphaAnimation(1, 0);
            an.Duration = ALPHA_DURATION;
            an.FillAfter = true;
            return an;
        }

        #endregion


        private View CreateView()
        {
            FrameLayout parent = new FrameLayout(Activity);
            parent.LayoutParameters = new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MatchParent,
                FrameLayout.LayoutParams.MatchParent);
            viewBg = new View(Activity);
            viewBg.LayoutParameters = new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MatchParent,
                FrameLayout.LayoutParams.MatchParent);
            viewBg.SetBackgroundColor(Color.Argb(136, 0, 0, 0));
            viewBg.Id = ActionSheet.BG_VIEW_ID;
            viewBg.SetOnClickListener(this);

            panel = new LinearLayout(Activity);
            FrameLayout.LayoutParams layoutParams = 
                new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MatchParent, FrameLayout.LayoutParams.WrapContent);
            layoutParams.Gravity = GravityFlags.Bottom;
            panel.LayoutParameters = layoutParams;
            panel.Orientation = AWD_O.Vertical;
            parent.SetPadding(0, 0, 0, GetNavBarHeight(Activity));
            parent.AddView(viewBg);
            parent.AddView(panel);
            return parent;
        }

        public int GetNavBarHeight(Context c)
        {
            int result = 0;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich)
            {
                bool hasMenuKey = ViewConfiguration.Get(c).HasPermanentMenuKey;
                bool hasBackKey = KeyCharacterMap.DeviceHasKey(Keycode.Back);

                if (!hasMenuKey && !hasBackKey)
                {
                    //The device has a navigation bar
                    Resources resources = c.Resources;
                    var orientation = Resources.Configuration.Orientation;
                    int resourceId;
                    if (IsTablet(c))
                    {
                        resourceId = resources.GetIdentifier(orientation == AContRes_O.Portrait ? "navigation_bar_height" : "navigation_bar_height_landscape", "dimen", "android");
                    }
                    else
                    {
                        resourceId = resources.GetIdentifier(orientation == AContRes_O.Portrait ? "navigation_bar_height" : "navigation_bar_width", "dimen", "android");
                    }

                    if (resourceId > 0)
                    {
                        return Resources.GetDimensionPixelSize(resourceId);
                    }
                }
            }
            return result;
        }

        private bool IsTablet(Context c)
        {
            return (c.Resources.Configuration.ScreenLayout
            & ScreenLayout.SizeMask)
            >= ScreenLayout.SizeLarge;
        }

        private void CreateItems()
        {
            var titles = Other_Button_Title;
            if (titles != null)
            {
                for (int i = 0; i < titles.Count; i++)
                {
                    Button _bt = new Button(Activity);
                    _bt.Id = CANCEL_BUTTON_ID + i + 1;
                    _bt.SetOnClickListener(this);
                    _bt.SetBackgroundDrawable(GetOtherButtonBg(titles.ToArray(), i));
                    _bt.Text = titles[i];
                    _bt.SetTextColor(attrs.otherButtonTextColor);
                    _bt.SetTextSize(AUComplexUnitType.Px, attrs.actionSheetTextSize);
                    if (i > 0)
                    {
                        LinearLayout.LayoutParams _params = CreateButtonLayoutParams();
                        _params.TopMargin = attrs.otherButtonSpacing;
                        panel.AddView(_bt, _params);
                    }
                    else
                    {
                        panel.AddView(_bt);
                    }
                }
            }
            Button bt = new Button(Activity);
            bt.Paint.FakeBoldText = true;
            bt.SetTextSize(AUComplexUnitType.Px, attrs.actionSheetTextSize);
            bt.Id = ActionSheet.CANCEL_BUTTON_ID;
            bt.SetBackgroundDrawable(attrs.cancelButtonBackground);
            bt.Text = Cancel_Button_Title;
            bt.SetTextColor(attrs.cancelButtonTextColor);
            bt.SetOnClickListener(this);
            LinearLayout.LayoutParams layoutParams = CreateButtonLayoutParams();
            layoutParams.TopMargin = attrs.cancelButtonMarginTop;
            panel.AddView(bt, layoutParams);

            panel.SetBackgroundDrawable(attrs.background);
            panel.SetPadding(attrs.padding, attrs.padding, attrs.padding,
                attrs.padding);
        }

        public LinearLayout.LayoutParams CreateButtonLayoutParams()
        {
            LinearLayout.LayoutParams layoutParams = new LinearLayout.LayoutParams(
                                                         FrameLayout.LayoutParams.MatchParent, FrameLayout.LayoutParams.WrapContent);
            return layoutParams;
        }


        private Drawable GetOtherButtonBg(String[] titles, int i)
        {
            if (titles.Length == 1)
            {
                return attrs.otherButtonSingleBackground;
            }
            if (titles.Length == 2)
            {
                switch (i)
                {
                    case 0:
                        return attrs.otherButtonTopBackground;
                    case 1:
                        return attrs.otherButtonBottomBackground;
                }
            }
            if (titles.Length > 2)
            {
                if (i == 0)
                {
                    return attrs.otherButtonTopBackground;
                }
                if (i == (titles.Length - 1))
                {
                    return attrs.otherButtonBottomBackground;
                }
                return attrs.getOtherButtonMiddleBackground();
            }
            return null;
        }

        private Attributes ReadAttribute()
        {
            Attributes attrs = new Attributes(Activity);
            TypedArray a = Activity.Theme.ObtainStyledAttributes(null,
                               Resource.Styleable.ActionSheet, Resource.Attribute.actionSheetStyle, 0);
            Drawable background = a
                .GetDrawable(Resource.Styleable.ActionSheet_actionSheetBackground);
            if (background != null)
            {
                attrs.background = background;
            }
            Drawable cancelButtonBackground = a
                .GetDrawable(Resource.Styleable.ActionSheet_cancelButtonBackground);
            if (cancelButtonBackground != null)
            {
                attrs.cancelButtonBackground = cancelButtonBackground;
            }
            Drawable otherButtonTopBackground = a
                .GetDrawable(Resource.Styleable.ActionSheet_otherButtonTopBackground);
            if (otherButtonTopBackground != null)
            {
                attrs.otherButtonTopBackground = otherButtonTopBackground;
            }
            Drawable otherButtonMiddleBackground = a
                .GetDrawable(Resource.Styleable.ActionSheet_otherButtonMiddleBackground);
            if (otherButtonMiddleBackground != null)
            {
                attrs.otherButtonMiddleBackground = otherButtonMiddleBackground;
            }
            Drawable otherButtonBottomBackground = a
                .GetDrawable(Resource.Styleable.ActionSheet_otherButtonBottomBackground);
            if (otherButtonBottomBackground != null)
            {
                attrs.otherButtonBottomBackground = otherButtonBottomBackground;
            }
            Drawable otherButtonSingleBackground = a
                .GetDrawable(Resource.Styleable.ActionSheet_otherButtonSingleBackground);
            if (otherButtonSingleBackground != null)
            {
                attrs.otherButtonSingleBackground = otherButtonSingleBackground;
            }
            attrs.cancelButtonTextColor = a.GetColor(
                Resource.Styleable.ActionSheet_cancelButtonTextColor,
                attrs.cancelButtonTextColor);
            attrs.otherButtonTextColor = a.GetColor(
                Resource.Styleable.ActionSheet_otherButtonTextColor,
                attrs.otherButtonTextColor);
            attrs.padding = (int)a.GetDimension(
                Resource.Styleable.ActionSheet_actionSheetPadding, attrs.padding);
            attrs.otherButtonSpacing = (int)a.GetDimension(
                Resource.Styleable.ActionSheet_otherButtonSpacing,
                attrs.otherButtonSpacing);
            attrs.cancelButtonMarginTop = (int)a.GetDimension(
                Resource.Styleable.ActionSheet_cancelButtonMarginTop,
                attrs.cancelButtonMarginTop);
            attrs.actionSheetTextSize = a.GetDimensionPixelSize(Resource.Styleable.ActionSheet_actionSheetTextSize, (int)attrs.actionSheetTextSize);

            a.Recycle();
            return attrs;
        }

        public void SetActionSheetListener(IActionSheetListener listener)
        {
            mListener = listener;
        }

        public void OnClick(View v)
        {
            if (v.Id == ActionSheet.BG_VIEW_ID && !Cancel_On_Touch_Outside)
            {
                return;
            }
            Dismiss();
            if (v.Id != ActionSheet.CANCEL_BUTTON_ID && v.Id != ActionSheet.BG_VIEW_ID)
            {
                if (mListener != null)
                {
                    mListener.onOtherButtonClick(this, v.Id - CANCEL_BUTTON_ID
                    - 1);
                }
                isCancel = false;
            }
        }

        private class Attributes
        {
            private Context mContext;

            public Attributes(Context context)
            {
                mContext = context;
                this.background = new ColorDrawable(Color.Transparent);
                this.cancelButtonBackground = new ColorDrawable(Color.Black);
                ColorDrawable gray = new ColorDrawable(Color.Gray);
                this.otherButtonTopBackground = gray;
                this.otherButtonMiddleBackground = gray;
                this.otherButtonBottomBackground = gray;
                this.otherButtonSingleBackground = gray;
                this.cancelButtonTextColor = Color.White;
                this.otherButtonTextColor = Color.Black;
                this.padding = dp2px(10);
                this.otherButtonSpacing = dp2px(2);
                this.cancelButtonMarginTop = dp2px(5);
                this.actionSheetTextSize = dp2px(16);
            }

            private int dp2px(int dp)
            {
                return (int)TypedValue.ApplyDimension(ComplexUnitType.Dip,
                    dp, mContext.Resources.DisplayMetrics);
            }

            public Drawable getOtherButtonMiddleBackground()
            {
                if (otherButtonMiddleBackground is StateListDrawable)
                {
                    TypedArray a = mContext.Theme.ObtainStyledAttributes(null,
                                       Resource.Styleable.ActionSheet, Resource.Attribute.actionSheetStyle, 0);
                    otherButtonMiddleBackground = a
                        .GetDrawable(Resource.Styleable.ActionSheet_otherButtonMiddleBackground);
                    a.Recycle();
                }
                return otherButtonMiddleBackground;
            }

            public Drawable background;
            public Drawable cancelButtonBackground;
            public Drawable otherButtonTopBackground;
            public Drawable otherButtonMiddleBackground;
            public Drawable otherButtonBottomBackground;
            public Drawable otherButtonSingleBackground;
            public Color cancelButtonTextColor;
            public Color otherButtonTextColor;
            public int padding;
            public int otherButtonSpacing;
            public int cancelButtonMarginTop;
            public float actionSheetTextSize;
        }

        public interface IActionSheetListener
        {
            /// <summary>
            /// 关闭菜单事件
            /// </summary>
            /// <param name="actionSheet"></param>
            /// <param name="isCancel"></param>
            void onDismiss(ActionSheet actionSheet, bool isCancel);
            /// <summary>
            /// 点击按钮事件
            /// </summary>
            /// <param name="actionSheet"></param>
            /// <param name="index"></param>
            void onOtherButtonClick(ActionSheet actionSheet, int index);
        }

    }
}

