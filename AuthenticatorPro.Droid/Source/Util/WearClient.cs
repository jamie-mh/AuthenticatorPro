// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Wearable;

namespace AuthenticatorPro.Droid.Util
{
    internal class WearClient : Java.Lang.Object, CapabilityClient.IOnCapabilityChangedListener
    {
        private const string RefreshCapability = "refresh";
        
        private bool _areGoogleAPIsAvailable;
        private bool _hasWearAPIs;
        private bool _hasWearCompanion;

        private readonly Context _context;

        public WearClient(Context context)
        {
            _context = context;
        }

        public async Task DetectCapability()
        {
            DetectGoogleAPIsAvailability();
            await DetectWearOSCapability();
        }

        public async Task StartListening()
        {
            if(_hasWearAPIs)
                await WearableClass.GetCapabilityClient(_context).AddListenerAsync(this, RefreshCapability);
        }

        public async Task StopListening()
        {
            if(!_hasWearAPIs)
                return;

            try
            {
                await WearableClass.GetCapabilityClient(_context).RemoveListenerAsync(this, RefreshCapability);
            }
            catch(ApiException e)
            {
                Logger.Error(e);
            }
        }

        private void DetectGoogleAPIsAvailability()
        {
            _areGoogleAPIsAvailable = 
                GoogleApiAvailabilityLight.Instance.IsGooglePlayServicesAvailable(_context) == ConnectionResult.Success;
        }

        private async Task DetectWearOSCapability()
        {
            if(!_areGoogleAPIsAvailable)
            {
                _hasWearAPIs = false;
                _hasWearCompanion = false;
                return;
            }

            try
            {
                var capabiltyInfo = await WearableClass.GetCapabilityClient(_context)
                    .GetCapabilityAsync(RefreshCapability, CapabilityClient.FilterReachable);

                _hasWearAPIs = true;
                _hasWearCompanion = capabiltyInfo.Nodes.Count > 0;
            }
            catch(ApiException)
            {
                _hasWearAPIs = false;
                _hasWearCompanion = false;
            }
        }

        public async Task NotifyChange()
        {
            if(!_hasWearCompanion)
                return;

            ICollection<INode> nodes;

            try
            {
                nodes = (await WearableClass.GetCapabilityClient(_context)
                    .GetCapabilityAsync(RefreshCapability, CapabilityClient.FilterReachable)).Nodes;
            }
            catch(ApiException e)
            {
                Logger.Error(e);
                return;
            }

            var client = WearableClass.GetMessageClient(_context);

            foreach(var node in nodes)
            {
                try
                {
                    await client.SendMessageAsync(node.Id, RefreshCapability, new byte[] { });
                }
                catch(ApiException e)
                {
                    Logger.Error(e); 
                }
            }
        }
        
        public async void OnCapabilityChanged(ICapabilityInfo capabilityInfo)
        {
            await DetectWearOSCapability();
        }
    }
}