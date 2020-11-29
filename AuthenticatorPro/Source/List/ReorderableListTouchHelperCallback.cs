using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.List
{
    internal class ReorderableListTouchHelperCallback : ItemTouchHelper.Callback
    {
        private readonly IReorderableListAdapter _adapter;
        private readonly GridLayoutManager _layoutManager;

        public override bool IsLongPressDragEnabled => true;
        public override bool IsItemViewSwipeEnabled => false;

        private int _movementStartPosition;
        private int _movementEndPosition;

        public ReorderableListTouchHelperCallback(IReorderableListAdapter adapter, GridLayoutManager layoutManager)
        {
            _layoutManager = layoutManager;
            _adapter = adapter;
            _movementStartPosition = -1;
            _movementEndPosition = -1;
        }

        public override int GetMovementFlags(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            var dragFlags = ItemTouchHelper.Up | ItemTouchHelper.Down;

            if(_layoutManager.SpanCount > 1)
                dragFlags |= ItemTouchHelper.Left | ItemTouchHelper.Right;

            return MakeMovementFlags(dragFlags, 0);
        }

        public override bool OnMove(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder, RecyclerView.ViewHolder target)
        {
            if(_movementEndPosition == -1)
                _adapter.NotifyMovementStarted();

            _movementEndPosition = target.AdapterPosition;
            _adapter.MoveItemView(viewHolder.AdapterPosition, target.AdapterPosition);
            return true;
        }

        public override bool CanDropOver(RecyclerView recyclerView, RecyclerView.ViewHolder current, RecyclerView.ViewHolder target)
        {
            return current.ItemViewType == target.ItemViewType;
        }

        public override void OnSelectedChanged(RecyclerView.ViewHolder viewHolder, int actionState)
        {
            base.OnSelectedChanged(viewHolder, actionState);

            switch(actionState)
            {
                case ItemTouchHelper.ActionStateDrag:
                    _movementStartPosition = viewHolder.AdapterPosition;
                    break;
                
                case ItemTouchHelper.ActionStateIdle:
                    if(viewHolder == null && _movementStartPosition > -1 &&
                       _movementEndPosition > -1 && _movementStartPosition != _movementEndPosition)
                    {
                        _adapter.NotifyMovementFinished(_movementStartPosition, _movementEndPosition);
                        _movementStartPosition = -1;
                        _movementEndPosition = -1;
                    }
                    break;
            }
        }

        public override void OnSwiped(RecyclerView.ViewHolder viewHolder, int direction)
        {

        }
    }
}