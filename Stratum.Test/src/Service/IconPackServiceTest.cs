// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Core.Service;
using Stratum.Core.Service.Impl;
using Moq;
using Xunit;

namespace Stratum.Test.Service
{
    public class IconPackServiceTest
    {
        private readonly Mock<IIconPackRepository> _iconPackRepository;
        private readonly Mock<IIconPackEntryRepository> _iconPackEntryRepository;

        private readonly IIconPackService _iconPackService;

        public IconPackServiceTest()
        {
            _iconPackRepository = new Mock<IIconPackRepository>();
            _iconPackEntryRepository = new Mock<IIconPackEntryRepository>();

            _iconPackService = new IconPackService(
                _iconPackRepository.Object,
                _iconPackEntryRepository.Object);
        }

        [Fact]
        public async Task ImportPackAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _iconPackService.ImportPackAsync(null));
        }

        [Fact]
        public async Task ImportPackAsync_nullIcons()
        {
            var pack = new IconPack();
            await Assert.ThrowsAsync<ArgumentException>(() => _iconPackService.ImportPackAsync(pack));
        }

        [Fact]
        public async Task ImportPackAsync_doesntExist()
        {
            var pack = new IconPack { Name = "test", Icons = [new IconPackEntry { Name = "icon" }] };

            _iconPackRepository.Setup(r => r.GetAsync(pack.Name)).ReturnsAsync((IconPack) null);
            _iconPackRepository.Setup(r => r.CreateAsync(pack)).Verifiable();

            var match = new CaptureMatch<List<IconPackEntry>>(e =>
            {
                foreach (var item in e)
                {
                    Assert.Equal(pack.Name, item.IconPackName);
                }
            });

            _iconPackEntryRepository.Setup(r => r.CreateManyAsync(Capture.With(match)));

            await _iconPackService.ImportPackAsync(pack);

            _iconPackRepository.Verify(r => r.CreateAsync(pack), Times.Once());
            _iconPackEntryRepository.Verify(r => r.CreateManyAsync(It.IsAny<List<IconPackEntry>>()), Times.Once());
        }

        [Fact]
        public async Task ImportPackAsync_exists()
        {
            var pack = new IconPack
            {
                Name = "test",
                Icons = [new IconPackEntry { Name = "icon1" }, new IconPackEntry { Name = "icon2" }]
            };

            _iconPackRepository.Setup(r => r.GetAsync(pack.Name)).ReturnsAsync(pack);
            _iconPackRepository.Setup(r => r.UpdateAsync(pack)).Verifiable();

            _iconPackEntryRepository.Setup(r => r.DeleteAllForPackAsync(pack)).Verifiable();

            var match = new CaptureMatch<List<IconPackEntry>>(e =>
            {
                foreach (var item in e)
                {
                    Assert.Equal(pack.Name, item.IconPackName);
                }
            });

            _iconPackEntryRepository.Setup(r => r.CreateManyAsync(Capture.With(match)));

            await _iconPackService.ImportPackAsync(pack);

            _iconPackRepository.Verify(r => r.UpdateAsync(pack), Times.Once());
            _iconPackEntryRepository.Verify(r => r.DeleteAllForPackAsync(pack), Times.Once());
            _iconPackEntryRepository.Verify(r => r.CreateManyAsync(It.IsAny<List<IconPackEntry>>()), Times.Once());
        }

        [Fact]
        public async Task DeletePackAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _iconPackService.DeletePackAsync(null));
        }

        [Fact]
        public async Task DeletePackAsync_ok()
        {
            var pack = new IconPack();

            _iconPackEntryRepository.Setup(r => r.DeleteAllForPackAsync(pack)).Verifiable();
            _iconPackRepository.Setup(r => r.DeleteAsync(pack)).Verifiable();

            await _iconPackService.DeletePackAsync(pack);

            _iconPackEntryRepository.Verify(r => r.DeleteAllForPackAsync(pack), Times.Once());
            _iconPackRepository.Verify(r => r.DeleteAsync(pack), Times.Once());
        }
    }
}