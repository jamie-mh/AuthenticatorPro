using Android.Animation;
using Android.Content;
using AndroidX.Core.View;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Internal;

namespace AuthenticatorPro.List
{
    internal class ReorderableListTouchHelperCallback : ItemTouchHelper.Callback
    {
        private const int DragElevationDp = 10;
        
        private readonly IReorderableListAdapter _adapter;
        private readonly GridLayoutManager _layoutManager;
        private readonly float _dragElevation;

        public override bool IsLongPressDragEnabled => true;
        public override bool IsItemViewSwipeEnabled => false;

        private int _movementStartPosition;
        private int _movementEndPosition;

        public ReorderableListTouchHelperCallback(Context context, IReorderableListAdapter adapter, GridLayoutManager layoutManager)
        {
            _layoutManager = layoutManager;
            _adapter = adapter;
            _movementStartPosition = -1;
            _movementEndPosition = -1;

            _dragElevation = ViewUtils.DpToPx(context, DragElevationDp);
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

        public override void OnSelectedChanged(RecyclerView.ViewHolder viewHolder, int actionState)
        {
            base.OnSelectedChanged(viewHolder, actionState);

            switch(actionState)
            {
                case ItemTouchHelper.ActionStateDrag:
                    _movementStartPosition = viewHolder.AdapterPosition;
                    var animator = ObjectAnimator.OfFloat(viewHolder.ItemView, "elevation", _dragElevation);
                    animator.SetDuration(200);
                    animator.Start();
                    break;
                
                case ItemTouchHelper.ActionStateIdle:
                    if(viewHolder == null && _movementStartPosition > -1 && _movementEndPosition > -1 && _movementStartPosition != _movementEndPosition)
                    {
                        _adapter.NotifyMovementFinished(_movementStartPosition, _movementEndPosition);
                        _movementStartPosition = -1;
                        _movementEndPosition = -1;
                    }
                    break;
            }
        }

        public override void ClearView(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            base.ClearView(recyclerView, viewHolder);
            ViewCompat.SetElevation(viewHolder.ItemView, 0f);
        }

        public override void OnSwiped(RecyclerView.ViewHolder viewHolder, int direction)
        {

        }
    }
}