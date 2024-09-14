// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Animation;
using Android.Content;
using AndroidX.Core.View;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Card;
using Stratum.Droid.Interface.Adapter;
using Stratum.Droid.Util;

namespace Stratum.Droid.Callback
{
    public class ReorderableListTouchHelperCallback : ItemTouchHelper.Callback
    {
        private const int DragElevationDp = 8;

        private readonly IReorderableListAdapter _adapter;
        private readonly GridLayoutManager _layoutManager;
        private readonly float _dragElevation;

        private int _movementStartPosition;
        private int _movementEndPosition;

        public ReorderableListTouchHelperCallback(Context context, IReorderableListAdapter adapter,
            GridLayoutManager layoutManager)
        {
            _layoutManager = layoutManager;
            _adapter = adapter;
            _movementStartPosition = -1;
            _movementEndPosition = -1;

            _dragElevation = DimenUtil.DpToPx(context, DragElevationDp);
        }

        public override bool IsLongPressDragEnabled => true;
        public override bool IsItemViewSwipeEnabled => false;
        public bool IsLocked { get; set; }

        public override int GetMovementFlags(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            if (IsLocked)
            {
                return MakeMovementFlags(0, 0);
            }

            var dragFlags = ItemTouchHelper.Up | ItemTouchHelper.Down;

            if (_layoutManager.SpanCount > 1)
            {
                dragFlags |= ItemTouchHelper.Left | ItemTouchHelper.Right;
            }

            return MakeMovementFlags(dragFlags, 0);
        }

        public override bool OnMove(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder,
            RecyclerView.ViewHolder target)
        {
            if (_movementEndPosition == -1)
            {
                _adapter.OnMovementStarted();
            }

            _movementEndPosition = target.BindingAdapterPosition;
            _adapter.MoveItemView(viewHolder.BindingAdapterPosition, target.BindingAdapterPosition);
            return true;
        }

        public override void OnSelectedChanged(RecyclerView.ViewHolder viewHolder, int actionState)
        {
            base.OnSelectedChanged(viewHolder, actionState);

            switch (actionState)
            {
                case ItemTouchHelper.ActionStateDrag:
                {
                    _movementStartPosition = viewHolder.BindingAdapterPosition;

                    if (viewHolder.ItemView is MaterialCardView card)
                    {
                        card.Dragged = true;
                    }
                    else
                    {
                        var animator = ObjectAnimator.OfFloat(viewHolder.ItemView, "elevation", _dragElevation);
                        animator.SetDuration(200);
                        animator.Start();
                    }

                    break;
                }

                case ItemTouchHelper.ActionStateIdle:
                {
                    if (viewHolder == null && _movementStartPosition > -1 && _movementEndPosition > -1)
                    {
                        _adapter.OnMovementFinished(_movementStartPosition != _movementEndPosition);
                        _movementStartPosition = -1;
                        _movementEndPosition = -1;
                    }

                    break;
                }
            }
        }

        public override void ClearView(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            base.ClearView(recyclerView, viewHolder);

            if (viewHolder.ItemView is MaterialCardView card)
            {
                card.Dragged = false;
            }
            else
            {
                ViewCompat.SetElevation(viewHolder.ItemView, 0f);
            }
        }

        public override void OnSwiped(RecyclerView.ViewHolder viewHolder, int direction)
        {
        }
    }
}