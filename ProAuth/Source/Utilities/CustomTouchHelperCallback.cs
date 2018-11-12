using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;

namespace ProAuth.Utilities
{
    internal class CustomTouchHelperCallback : ItemTouchHelper.Callback
    {
        private readonly IAuthAdapterMovement _movement;
        private readonly bool _isGrid;
        public override bool IsLongPressDragEnabled => true;
        public override bool IsItemViewSwipeEnabled => false;

        public CustomTouchHelperCallback(IAuthAdapterMovement movement, bool isGrid = false)
        {
            _movement = movement;
            _isGrid = isGrid;
        }

        public override int GetMovementFlags(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            int dragFlags = ItemTouchHelper.Up | ItemTouchHelper.Down;

            if(_isGrid)
            {
                dragFlags |= ItemTouchHelper.Left | ItemTouchHelper.Right;
            }

            return MakeMovementFlags(dragFlags, 0);
        }

        public override bool OnMove(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder, RecyclerView.ViewHolder target)
        {
            _movement.OnViewMoved(viewHolder.AdapterPosition, target.AdapterPosition);
            return true;
        }

        public override void OnSwiped(RecyclerView.ViewHolder viewHolder, int direction)
        {

        }
    }
}