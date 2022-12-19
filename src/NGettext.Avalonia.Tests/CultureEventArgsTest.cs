﻿using System.Globalization;
using Xunit;

namespace NGettext.Avalonia.Tests
{
    public class CultureEventArgsTest
    {
        readonly CultureEventArgs _target;

        public CultureEventArgsTest()
        {
            _target = new CultureEventArgs(CultureInfo.CurrentCulture);
        }

        [Fact]
        public void Depends_On_CultureInfo()
        {
            AssertExt.DependsOn("cultureInfo", () => new CultureEventArgs(null));
        }

        [Fact]
        public void CultureInfo_Is_Injected()
        {
            var cultureInfo = new CultureInfo("en-GB");
            var target = new CultureEventArgs(cultureInfo);
            Assert.Same(cultureInfo, target.CultureInfo);
        }

        [Fact]
        public void Is_An_EventArgs()
        {
            Assert.IsAssignableFrom<EventArgs>(_target);
        }
    }
}