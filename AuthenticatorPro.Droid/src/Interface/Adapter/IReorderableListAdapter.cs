// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace AuthenticatorPro.Droid.Interface.Adapter
{
    public interface IReorderableListAdapter
    {
        public void MoveItemView(int oldPosition, int newPosition);
        public void OnMovementStarted();
        public void OnMovementFinished(bool orderChanged);
    }
}