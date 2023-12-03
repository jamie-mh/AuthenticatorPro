// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;
using Android.App;
using Android.Graphics;
using AndroidX.Wear.Tiles;
using AndroidX.Wear.Tiles.Material;
using AndroidX.Wear.Tiles.Material.Layouts;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Generator;
using AuthenticatorPro.Core.Util;
using AuthenticatorPro.Droid.Shared;
using AuthenticatorPro.Droid.Shared.Wear;
using AuthenticatorPro.WearOS.Activity;
using AuthenticatorPro.WearOS.Cache;
using AuthenticatorPro.WearOS.Util;
using Google.Common.Util.Concurrent;
using Java.Lang;
using Java.Nio;

namespace AuthenticatorPro.WearOS
{
    [Service(Exported = true, Permission = "com.google.android.wearable.permission.BIND_TILE_PROVIDER",
        Label = "@string/displayName")]
    [IntentFilter(new[] { ActionBindTileProvider })]
    [MetaData(MetadataPreviewKey, Resource = "@drawable/tile_preview")]
    public class AuthTileService : TileService
    {
        private const string AuthenticatorCacheName = "authenticators";
        private const string EmptyResourcesVersion = "EMPTY";

        private PreferenceWrapper _preferences;
        private ListCache<WearAuthenticator> _authenticatorCache;
        private CustomIconCache _customIconCache;

        private WearAuthenticator _authenticator;
        private IGenerator _generator;

        public override void OnCreate()
        {
            base.OnCreate();

            _preferences = new PreferenceWrapper(this);
            _authenticatorCache = new ListCache<WearAuthenticator>(AuthenticatorCacheName, this);
            _customIconCache = new CustomIconCache(this);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            _customIconCache?.Dispose();
        }

        private async Task<ResourceBuilders.InlineImageResource> GetCustomIconResourceAsync(string id)
        {
            var bitmap = await _customIconCache.GetFreshBitmapAsync(id);
            Bitmap copy = null;

            try
            {
                // Use RGB565 because other formats such as ARGB8888 don't work
                copy = bitmap.Copy(Bitmap.Config.Rgb565, false);

                var buffer = ByteBuffer.Allocate(copy.AllocationByteCount);
                await copy.CopyPixelsToBufferAsync(buffer);

                buffer.Rewind();
                var data = new byte[buffer.Remaining()];
                buffer.Get(data);

                return new ResourceBuilders.InlineImageResource.Builder()
                    .SetData(data)
                    .SetFormat(ResourceBuilders.ImageFormatRgb565)
                    .SetWidthPx(copy.Width)
                    .SetHeightPx(copy.Height)
                    .Build();
            }
            finally
            {
                copy?.Recycle();
            }
        }

        protected override IListenableFuture OnResourcesRequest(RequestBuilders.ResourcesRequest request)
        {
            Logger.Debug($"Tile resources {request.Version} requested");

            var adapter = new TaskFutureAdapter<ResourceBuilders.Resources>(async delegate
            {
                var builder = new ResourceBuilders.Resources.Builder();

                if (request.Version == EmptyResourcesVersion)
                {
                    return builder
                        .SetVersion(EmptyResourcesVersion)
                        .Build();
                }

                var imageBuilder = new ResourceBuilders.ImageResource.Builder();

                if (request.Version.StartsWith(CustomIcon.Prefix))
                {
                    var image = await GetCustomIconResourceAsync(request.Version[1..]);
                    imageBuilder.SetInlineResource(image);
                }
                else
                {
                    var image = new ResourceBuilders.AndroidImageResourceByResId.Builder()
                        .SetResourceId(IconResolver.GetService(request.Version, false))
                        .Build();

                    imageBuilder.SetAndroidResourceByResId(image);
                }

                builder.AddIdToImageMapping("icon", imageBuilder.Build());
                builder.SetVersion(request.Version);

                return builder.Build();
            });

            return adapter.GetFuture();
        }

        protected override void OnTileEnterEvent(EventBuilders.TileEnterEvent requestParams)
        {
            Logger.Debug("Tile entered view");
            var clazz = Class.FromType(typeof(AuthTileService));
            GetUpdater(this).RequestUpdate(clazz);
        }

        private static LayoutElementBuilders.FontStyle BuildFontStyle(float size, int colour)
        {
            var sizeProp = new DimensionBuilders.SpProp.Builder()
                .SetValue(size)
                .Build();

            var colourProp = new ColorBuilders.ColorProp.Builder()
                .SetArgb(colour)
                .Build();

            return new LayoutElementBuilders.FontStyle.Builder()
                .SetSize(sizeProp)
                .SetColor(colourProp)
                .Build();
        }

        private static DimensionBuilders.DpProp BuildDpProp(float value)
        {
            return (DimensionBuilders.DpProp) new DimensionBuilders.DpProp.Builder()
                .SetValue(value)
                .Build();
        }

        private static LayoutElementBuilders.ILayoutElement BuildSpacer(float horizontal, float vertical)
        {
            return new LayoutElementBuilders.Spacer.Builder()
                .SetWidth(BuildDpProp(horizontal))
                .SetHeight(BuildDpProp(vertical))
                .Build();
        }

        private ModifiersBuilders.Clickable BuildAppOpenClickable()
        {
            var clazz = Class.FromType(typeof(MainActivity));

            var activity = new ActionBuilders.AndroidActivity.Builder()
                .SetClassName(clazz.Name)
                .SetPackageName(PackageName)
                .Build();

            var action = new ActionBuilders.LaunchAction.Builder()
                .SetAndroidActivity(activity)
                .Build();

            return new ModifiersBuilders.Clickable.Builder()
                .SetOnClick(action)
                .Build();
        }

        private TileBuilders.Tile BuildEmptyTile(DeviceParametersBuilders.DeviceParameters deviceParameters)
        {
            var titleText = new LayoutElementBuilders.Text.Builder()
                .SetText(GetString(Resource.String.noFavourite))
                .SetFontStyle(BuildFontStyle(16f, Colors.Default.Primary))
                .Build();

            var overflow = new LayoutElementBuilders.TextOverflowProp.Builder()
                .SetValue(LayoutElementBuilders.TextOverflowEllipsizeEnd)
                .Build();

            var messageText = new LayoutElementBuilders.Text.Builder()
                .SetText(GetString(Resource.String.noFavouriteMessage))
                .SetMaxLines(3)
                .SetOverflow(overflow)
                .SetFontStyle(BuildFontStyle(16f, GetColor(Resource.Color.colorLighter)))
                .Build();

            var compactChip =
                new CompactChip.Builder(this, GetString(Resource.String.open), BuildAppOpenClickable(),
                        deviceParameters)
                    .Build();

            var primaryLayout = new PrimaryLayout.Builder(deviceParameters)
                .SetPrimaryLabelTextContent(titleText)
                .SetContent(messageText)
                .SetPrimaryChipContent(compactChip)
                .Build();

            var layout = new LayoutElementBuilders.Layout.Builder()
                .SetRoot(primaryLayout)
                .Build();

            var entry = new TimelineBuilders.TimelineEntry.Builder()
                .SetLayout(layout)
                .Build();

            var timeline = new TimelineBuilders.Timeline.Builder()
                .AddTimelineEntry(entry)
                .Build();

            return new TileBuilders.Tile.Builder()
                .SetResourcesVersion(EmptyResourcesVersion)
                .SetTimeline(timeline)
                .Build();
        }

        private TileBuilders.Tile BuildCodeTile()
        {
            var clickableModifier = new ModifiersBuilders.Modifiers.Builder()
                .SetClickable(BuildAppOpenClickable())
                .Build();

            var column = new LayoutElementBuilders.Column.Builder();
            column.SetModifiers(clickableModifier);

            var iconSize = BuildDpProp(24f);

            var icon = new LayoutElementBuilders.Image.Builder()
                .SetResourceId("icon")
                .SetWidth(iconSize)
                .SetHeight(iconSize)
                .Build();

            column.AddContent(icon);
            column.AddContent(BuildSpacer(0, 8f));

            var issuerText = new LayoutElementBuilders.Text.Builder()
                .SetText(_authenticator.Issuer)
                .SetFontStyle(BuildFontStyle(14f, GetColor(Resource.Color.colorLighter)))
                .Build();

            column.AddContent(issuerText);
            column.AddContent(BuildSpacer(0, 2f));

            if (!string.IsNullOrEmpty(_authenticator.Username))
            {
                var usernameText = new LayoutElementBuilders.Text.Builder()
                    .SetText(_authenticator.Username)
                    .SetFontStyle(BuildFontStyle(12f, GetColor(Resource.Color.colorLight)))
                    .Build();

                column.AddContent(usernameText);
                column.AddContent(BuildSpacer(0, 4f));
            }

            var (code, secondsRemaining) =
                AuthenticatorUtil.GetCodeAndRemainingSeconds(_generator, _authenticator.Period);

            var codeText = new LayoutElementBuilders.Text.Builder()
                .SetText(CodeUtil.PadCode(code, _authenticator.Digits, _preferences.CodeGroupSize))
                .SetFontStyle(BuildFontStyle(28f, GetColor(Resource.Color.colorLightest)))
                .Build();

            column.AddContent(codeText);

            var layout = new LayoutElementBuilders.Layout.Builder()
                .SetRoot(column.Build())
                .Build();

            var entry = new TimelineBuilders.TimelineEntry.Builder()
                .SetLayout(layout)
                .Build();

            var timeline = new TimelineBuilders.Timeline.Builder()
                .AddTimelineEntry(entry)
                .Build();

            return new TileBuilders.Tile.Builder()
                .SetResourcesVersion(_authenticator.Icon ?? IconResolver.Default)
                .SetTimeline(timeline)
                .SetFreshnessIntervalMillis(secondsRemaining * 1000)
                .Build();
        }

        private async Task FetchAuthenticatorAsync()
        {
            var defaultAuth = _preferences.DefaultAuth;

            if (defaultAuth == null)
            {
                _authenticator = null;
                _generator = null;
                return;
            }

            if (_authenticator != null && defaultAuth == HashUtil.Sha1(_authenticator.Secret))
            {
                return;
            }

            await _authenticatorCache.InitAsync();
            var index = _authenticatorCache.FindIndex(a => HashUtil.Sha1(a.Secret) == defaultAuth);

            if (index > -1)
            {
                _authenticator = _authenticatorCache[index];
                _generator = AuthenticatorUtil.GetGenerator(_authenticator);
            }
            else
            {
                _authenticator = null;
                _generator = null;
            }
        }

        protected override IListenableFuture OnTileRequest(RequestBuilders.TileRequest request)
        {
            Logger.Debug("Tile requested");

            var adapter = new TaskFutureAdapter<TileBuilders.Tile>(async delegate
            {
                await FetchAuthenticatorAsync();
                Logger.Debug($"Rendering tile for authenticator {_authenticator?.Issuer}");
                return _authenticator != null ? BuildCodeTile() : BuildEmptyTile(request.DeviceParameters);
            });

            return adapter.GetFuture();
        }
    }
}