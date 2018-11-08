using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;

namespace ProAuth.Utilities
{
    internal class AuthTouchHelperCallback : ItemTouchHelper.Callback
    {
        private readonly IAuthAdapterMovement _movement;
        public override bool IsLongPressDragEnabled => true;
        public override bool IsItemViewSwipeEnabled => false;

        public AuthTouchHelperCallback(IAuthAdapterMovement movement)
        {
            _movement = movement;
        }

        public override int GetMovementFlags(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            int dragFlags = ItemTouchHelper.Up | ItemTouchHelper.Down | ItemTouchHelper.Left | ItemTouchHelper.Right;
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