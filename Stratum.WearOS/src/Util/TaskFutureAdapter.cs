// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using AndroidX.Concurrent.Futures;
using Google.Common.Util.Concurrent;
using Object = Java.Lang.Object;

namespace Stratum.WearOS.Util
{
    public class TaskFutureAdapter<T> : Object, CallbackToFutureAdapter.IResolver where T : Object
    {
        private readonly Func<Task<T>> _executor;

        public TaskFutureAdapter(Func<Task<T>> executor)
        {
            _executor = executor;
        }

        public Object AttachCompleter(CallbackToFutureAdapter.Completer completer)
        {
            Task.Run(async delegate
            {
                try
                {
                    completer.Set(await _executor());
                }
                catch (Exception e)
                {
                    completer.SetException(new Java.Lang.Exception(e.Message));
                }
            });

            return null;
        }

        public IListenableFuture GetFuture()
        {
            return CallbackToFutureAdapter.GetFuture(this);
        }
    }
}