using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro
{
    internal class ReorderableListTouchHelperCallback : ItemTouchHelper.Callback
    {
        private readonly bool _isGrid;
        private readonly IReorderableListAdapter _adapter;

        public ReorderableListTouchHelperCallback(IReorderableListAdapter adapter, bool isGrid = false)
        {
            _adapter = adapter;
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
            _adapter.MoveItem(viewHolder.AdapterPosition, target.AdapterPosition);
            return true;
        }

        public override bool CanDropOver(RecyclerView recyclerView, RecyclerView.ViewHolder current, RecyclerView.ViewHolder target)
        {
            return current.ItemViewType == target.ItemViewType;
        }

        public override void ClearView(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            base.ClearView(recyclerView, viewHolder);
            _adapter.NotifyMovementFinished();
        }

        public override void OnSwiped(RecyclerView.ViewHolder viewHolder, int direction)
        {
        }
    }
}