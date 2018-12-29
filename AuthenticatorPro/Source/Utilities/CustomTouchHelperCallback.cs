using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;

namespace AuthenticatorPro.Utilities
{
    internal class CustomTouchHelperCallback : ItemTouchHelper.Callback
    {
        private readonly bool _isGrid;
        private readonly IAuthAdapterMovement _movement;

        public CustomTouchHelperCallback(IAuthAdapterMovement movement, bool isGrid = false)
        {
            _movement = movement;
            _isGrid = isGrid;
        }

        public override bool IsLongPressDragEnabled => true;
        public override bool IsItemViewSwipeEnabled => false;

        public override int GetMovementFlags(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            var dragFlags = ItemTouchHelper.Up | ItemTouchHelper.Down;

            if(_isGrid) dragFlags |= ItemTouchHelper.Left | ItemTouchHelper.Right;

            return MakeMovementFlags(dragFlags, 0);
        }

        public override bool OnMove(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder,
            RecyclerView.ViewHolder target)
        {
            _movement.OnViewMoved(viewHolder.AdapterPosition, target.AdapterPosition);
            return true;
        }

        public override void OnSwiped(RecyclerView.ViewHolder viewHolder, int direction)
        {
        }
    }
}