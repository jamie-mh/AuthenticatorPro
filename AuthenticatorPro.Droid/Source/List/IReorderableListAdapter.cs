// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace AuthenticatorPro.Droid.List
{
    internal interface IReorderableListAdapter
    {
        public void MoveItemView(int oldPosition, int newPosition);
        public void NotifyMovementStarted();
        public void NotifyMovementFinished();
    }
}