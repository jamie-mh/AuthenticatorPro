// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Linq;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Stratum.Core.Entity;
using Stratum.Droid.Shared;
using Google.Android.Material.Button;
using Google.Android.Material.Chip;
using Google.Android.Material.TextView;
using Stratum.Droid.Persistence.View;

namespace Stratum.Droid.Interface.Fragment
{
    public class AssignCategoryEntriesBottomSheet : BottomSheet
    {
        private readonly IAuthenticatorView _authenticatorView;
        private readonly ICustomIconView _customIconview;

        private string _id;
        private string[] _assignedAuthenticatorSecrets;

        public AssignCategoryEntriesBottomSheet() : base(Resource.Layout.sheetAssignCategoryEntries,
            Resource.String.assignEntries)
        {
            _authenticatorView = Dependencies.Resolve<IAuthenticatorView>();
            _customIconview = Dependencies.Resolve<ICustomIconView>();
        }

        public event EventHandler<AuthenticatorClickedEventArgs> AuthenticatorClicked;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _id = Arguments.GetString("id");
            _assignedAuthenticatorSecrets = Arguments.GetStringArray("assignedAuthenticatorSecrets");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            var okButton = view.FindViewById<MaterialButton>(Resource.Id.buttonOK);
            okButton.Click += delegate { Dismiss(); };

            return view;
        }

        public override async void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            await _authenticatorView.LoadFromPersistenceAsync();
            await _customIconview.LoadFromPersistenceAsync();

            var emptyText = View.FindViewById<MaterialTextView>(Resource.Id.textEmpty);
            var chipGroup = View.FindViewById<ChipGroup>(Resource.Id.chipGroup);

            if (!_authenticatorView.Any())
            {
                emptyText.Visibility = ViewStates.Visible;
                chipGroup.Visibility = ViewStates.Gone;
            }

            foreach (var auth in _authenticatorView)
            {
                var uniqueIssuer = _authenticatorView.Count(a => a.Issuer == auth.Issuer) == 1;
                var displayUsername = !uniqueIssuer && !string.IsNullOrEmpty(auth.Username);

                var chip = (Chip) LayoutInflater.Inflate(Resource.Layout.chipChoice, chipGroup, false);
                chip.Text = displayUsername ? $"{auth.Issuer} ({auth.Username})" : auth.Issuer;
                chip.Checkable = true;
                chip.Clickable = true;

                if (auth.Icon != null && auth.Icon.StartsWith(CustomIcon.Prefix))
                {
                    var bitmap = _customIconview.GetOrDefault(auth.Icon[1..]);
                    chip.ChipIcon = new BitmapDrawable(Resources, bitmap);
                }
                else
                {
                    var iconRes = IconResolver.GetService(auth.Icon, IsDark);
                    chip.ChipIcon = RequireContext().GetDrawable(iconRes);
                }

                chip.ChipIconVisible = true;

                if (_assignedAuthenticatorSecrets.Contains(auth.Secret))
                {
                    chip.Checked = true;
                }

                chip.Click += (sender, _) =>
                {
                    AuthenticatorClicked?.Invoke(sender,
                        new AuthenticatorClickedEventArgs(auth, _id, chip.Checked));
                };

                chipGroup.AddView(chip);
            }
        }

        public class AuthenticatorClickedEventArgs : EventArgs
        {
            public readonly Authenticator Authenticator;
            public readonly string CategoryId;
            public readonly bool IsChecked;

            public AuthenticatorClickedEventArgs(Authenticator authenticator, string categoryId, bool isChecked)
            {
                Authenticator = authenticator;
                CategoryId = categoryId;
                IsChecked = isChecked;
            }
        }
    }
}