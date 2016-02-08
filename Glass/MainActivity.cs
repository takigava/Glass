using Android.App;
using Android.Widget;
using Android.OS;
using GlassExtensions;
using Android.Views;
using Java.Util;
using Android.Animation;
using Android.Views.Animations;
using System;
using Android.Webkit;
using Android.Util;

namespace Glass
{
	[Activity (Label = "Glass", MainLauncher = true, Icon = "@mipmap/icon")]
	public class MainActivity : Activity
	{
		private BlurringView mBlurringView;

		private int[] mImageIds = {
			Resource.Drawable.p0, Resource.Drawable.p1, Resource.Drawable.p2, Resource.Drawable.p3, Resource.Drawable.p4,
			Resource.Drawable.p5, Resource.Drawable.p6, Resource.Drawable.p7, Resource.Drawable.p8, Resource.Drawable.p9
		};

		private ImageView[] mImageViews = new ImageView[9];
		private int mStartIndex;

		private Java.Util.Random mRandom = new Java.Util.Random();

		private bool mShifted;

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.activity_main);

			// Get our button from the layout resource,
			// and attach an event to it
			mBlurringView = (BlurringView) FindViewById(Resource.Id.blurring_view);
			View blurredView = FindViewById(Resource.Id.blurred_view);

			// Give the blurring view a reference to the blurred view.
			mBlurringView.setBlurredView(blurredView);

			mImageViews[0] = (ImageView) FindViewById(Resource.Id.image0);
			mImageViews[1] = (ImageView) FindViewById(Resource.Id.image1);
			mImageViews[2] = (ImageView) FindViewById(Resource.Id.image2);
			mImageViews[3] = (ImageView) FindViewById(Resource.Id.image3);
			mImageViews[4] = (ImageView) FindViewById(Resource.Id.image4);
			mImageViews[5] = (ImageView) FindViewById(Resource.Id.image5);
			mImageViews[6] = (ImageView) FindViewById(Resource.Id.image6);
			mImageViews[7] = (ImageView) FindViewById(Resource.Id.image7);
			mImageViews[8] = (ImageView) FindViewById(Resource.Id.image8);

			DisplayMetrics metrics = new DisplayMetrics();

			WindowManager.DefaultDisplay.GetMetrics(metrics);

			double inches = Math.Sqrt((metrics.WidthPixels * metrics.WidthPixels) + (metrics.HeightPixels * metrics.HeightPixels)) / metrics.Density;
		}

		[Java.Interop.Export("shuffle")]
		public void shuffle(View view) {

			// Randomly pick a different start in the array of available images.
			int newStartIndex;
			do {
				newStartIndex = mImageIds[mRandom.NextInt(mImageIds.Length)];
			} while (newStartIndex == mStartIndex);
			mStartIndex = newStartIndex;

			// Update the images for the image views contained in the blurred view.
			for (int i = 0; i < mImageViews.Length; i++) {
				int drawableId = mImageIds[(mStartIndex + i) % mImageIds.Length];
				mImageViews[i].SetImageDrawable(Resources.GetDrawable(drawableId));
			}

			// Invalidates the blurring view when the content of the blurred view changes.
			mBlurringView.Invalidate();
		}

		[Java.Interop.Export("shift")]
		public void shift(View view) {
			if (!mShifted) {
				foreach (ImageView imageView in mImageViews) {
					ObjectAnimator tx = ObjectAnimator.OfFloat(imageView, View.X, (mRandom.NextFloat() - 0.5f) * 500);
					tx.AddUpdateListener(new AnimListener(mBlurringView));
					ObjectAnimator ty = ObjectAnimator.OfFloat(imageView, View.Y, (mRandom.NextFloat() - 0.5f) * 500);
					ty.AddUpdateListener(new AnimListener(mBlurringView));
					AnimatorSet set = new AnimatorSet();
					set.PlayTogether(tx, ty);
					set.SetDuration(3000);
					set.SetInterpolator(new OvershootInterpolator());
					set.AddListener(new AnimationEndListener(imageView));
					set.Start();
				};
				mShifted = true;
			} else {
				foreach (ImageView imageView in mImageViews) {
					ObjectAnimator tx = ObjectAnimator.OfFloat(imageView, View.X, 0);
					tx.AddUpdateListener(new AnimListener(mBlurringView));
					ObjectAnimator ty = ObjectAnimator.OfFloat(imageView, View.Y, 0);
					ty.AddUpdateListener(new AnimListener(mBlurringView));
					AnimatorSet set = new AnimatorSet();
					set.PlayTogether(tx, ty);
					set.SetDuration(3000);
					set.SetInterpolator(new OvershootInterpolator());
					set.AddListener(new AnimationEndListener(imageView));
					set.Start();
				};
				mShifted = false;
			}
		}
	}

	public class AnimationEndListener : Java.Lang.Object, Animator.IAnimatorListener {

		View mView;

		public AnimationEndListener(View v) {
			mView = v;
		}
			
		public void OnAnimationStart(Animator animation) {
			mView.SetLayerType(LayerType.Hardware, null);
		}
			
		public void OnAnimationEnd(Animator animation) {
			mView.SetLayerType(LayerType.None, null);
		}
			
		public void OnAnimationCancel(Animator animation) {
			mView.SetLayerType(LayerType.None, null);
		}
			
		public void OnAnimationRepeat(Animator animation) {

		}
	}

	public class AnimListener : Java.Lang.Object, ValueAnimator.IAnimatorUpdateListener
	{
		BlurringView mBlurringView;

		public AnimListener(BlurringView v) {
			mBlurringView = v;
		}

		public void OnAnimationUpdate(ValueAnimator animation) {
			mBlurringView.Invalidate();
		}
	}
}


