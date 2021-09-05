// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace AuthenticatorPro.Shared.View
{
    public interface IReorderableView<out T> : IView<T>
    {
        public void Swap(int oldPosition, int newPosition);
    }
}