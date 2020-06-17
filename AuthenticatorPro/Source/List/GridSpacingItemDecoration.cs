using Android.Content;
using Android.Graphics;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Internal;

namespace AuthenticatorPro.List
{
    internal class GridSpacingItemDecoration : RecyclerView.ItemDecoration
    {
        private readonly int _spacing;
        private readonly GridLayoutManager _layoutManager;


        public GridSpacingItemDecoration(Context context, GridLayoutManager layoutManager, int spacingDp)
        {
            _layoutManager = layoutManager;
            _spacing = (int) ViewUtils.DpToPx(context, spacingDp);
        }

        public override void GetItemOffsets(Rect outRect, View view, RecyclerView parent, RecyclerView.State state)
        {
            var position = parent.GetChildAdapterPosition(view);
            var column = position % _layoutManager.SpanCount;

            outRect.Left = column * _spacing / _layoutManager.SpanCount;
            outRect.Right = _spacing - (column + 1) * _spacing / _layoutManager.SpanCount;
        }
    }
}