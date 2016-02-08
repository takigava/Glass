using System;
using Android.Views;
using Android.Graphics;
using Android.Content;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.Util;
//using Android.Support.V8.Renderscript;
using Android.Renderscripts;

namespace GlassExtensions
{
	public class BlurringView : View
	{
		private int mDownsampleFactor;
		private Color mOverlayColor;

		private View mBlurredView;
		private int mBlurredViewWidth, mBlurredViewHeight;

		private bool mDownsampleFactorChanged;
		private Bitmap mBitmapToBlur, mBlurredBitmap;
		private Canvas mBlurringCanvas;
		private RenderScript mRenderScript;
		private ScriptIntrinsicBlur mBlurScript;
		private Allocation mBlurInput, mBlurOutput;

		public BlurringView(Context context):base(context, null) {
		}

		public BlurringView(Context context, IAttributeSet attrs) :base(context, attrs){
			

			Resources res = Resources;
			int defaultBlurRadius = res.GetInteger(Resource.Integer.default_blur_radius);
			int defaultDownsampleFactor = res.GetInteger(Resource.Integer.default_downsample_factor);
			Color defaultOverlayColor = res.GetColor(Resource.Color.default_overlay_color);

			initializeRenderScript(context);

			TypedArray a = context.ObtainStyledAttributes(attrs, Resource.Styleable.PxBlurringView);
			setBlurRadius(a.GetInt(Resource.Styleable.PxBlurringView_blurRadius, defaultBlurRadius));
			setDownsampleFactor(a.GetInt(Resource.Styleable.PxBlurringView_downsampleFactor,
				defaultDownsampleFactor));
			setOverlayColor(a.GetColor(Resource.Styleable.PxBlurringView_overlayColor, defaultOverlayColor));
			a.Recycle();
		}

		public void setBlurredView(View blurredView) {
			mBlurredView = blurredView;
		}
			
		protected override void OnDraw(Canvas canvas) {
			base.OnDraw (canvas);
			if (mBlurredView != null) {
				if (prepare()) {
					// If the background of the blurred view is a color drawable, we use it to clear
					// the blurring canvas, which ensures that edges of the child views are blurred
					// as well; otherwise we clear the blurring canvas with a transparent color.
					if (mBlurredView.Background != null && mBlurredView.Background is ColorDrawable){
						mBitmapToBlur.EraseColor(((ColorDrawable) mBlurredView.Background).Color);
					}else {
						mBitmapToBlur.EraseColor(Color.Transparent);
					}

					mBlurredView.Draw(mBlurringCanvas);
					blur();

					canvas.Save();
					canvas.Translate(mBlurredView.GetX() - GetX(), mBlurredView.GetY() - GetY());
					canvas.Scale(mDownsampleFactor, mDownsampleFactor);
					canvas.DrawBitmap(mBlurredBitmap, 0, 0, null);
					canvas.Restore();
				}
				canvas.DrawColor(mOverlayColor);
			}
		}

		public void setBlurRadius(int radius) {
			mBlurScript.SetRadius(radius);
		}

		public void setDownsampleFactor(int factor) {
			if (factor <= 0) {
				throw new RSIllegalArgumentException("Downsample factor must be greater than 0.");
			}

			if (mDownsampleFactor != factor) {
				mDownsampleFactor = factor;
				mDownsampleFactorChanged = true;
			}
		}

		public void setOverlayColor(Color color) {
			mOverlayColor = color;
		}

		private void initializeRenderScript(Context context) {
			mRenderScript = RenderScript.Create(context);
			mBlurScript = ScriptIntrinsicBlur.Create(mRenderScript, Element.U8_4(mRenderScript));
		}

		protected bool prepare() {
			int width = mBlurredView.Width;
			int height = mBlurredView.Height;

			if (mBlurringCanvas == null || mDownsampleFactorChanged
				|| mBlurredViewWidth != width || mBlurredViewHeight != height) {
				mDownsampleFactorChanged = false;

				mBlurredViewWidth = width;
				mBlurredViewHeight = height;

				int scaledWidth = width / mDownsampleFactor;
				int scaledHeight = height / mDownsampleFactor;

				// The following manipulation is to avoid some RenderScript artifacts at the edge.
				scaledWidth = scaledWidth - scaledWidth % 4 + 4;
				scaledHeight = scaledHeight - scaledHeight % 4 + 4;

				if (mBlurredBitmap == null
					|| mBlurredBitmap.Width != scaledWidth
					|| mBlurredBitmap.Height != scaledHeight) {
					mBitmapToBlur = Bitmap.CreateBitmap(scaledWidth, scaledHeight,
						Bitmap.Config.Argb8888);
					if (mBitmapToBlur == null) {
						return false;
					}

					mBlurredBitmap = Bitmap.CreateBitmap(scaledWidth, scaledHeight,
						Bitmap.Config.Argb8888);
					if (mBlurredBitmap == null) {
						return false;
					}
				}

				mBlurringCanvas = new Canvas(mBitmapToBlur);
				mBlurringCanvas.Scale(1f / mDownsampleFactor, 1f / mDownsampleFactor);
				mBlurInput = Allocation.CreateFromBitmap(mRenderScript, mBitmapToBlur,
					Allocation.MipmapControl.MipmapNone,AllocationUsage.Script);
				mBlurOutput = Allocation.CreateTyped(mRenderScript, mBlurInput.Type);
			}
			return true;
		}

		protected void blur() {
			mBlurInput.CopyFrom(mBitmapToBlur);
			mBlurScript.SetInput(mBlurInput);
			mBlurScript.ForEach(mBlurOutput);
			mBlurOutput.CopyTo(mBlurredBitmap);
		}
			
		protected override void OnDetachedFromWindow() {
			base.OnDetachedFromWindow();
			if (mRenderScript != null){
				mRenderScript.Destroy();
			}
		}
	}
}

