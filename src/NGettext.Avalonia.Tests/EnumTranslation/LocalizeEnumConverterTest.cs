﻿using Avalonia.Data.Converters;
using NGettext.Avalonia.EnumTranslation;
using NSubstitute;
using Xunit;

namespace NGettext.Avalonia.Tests.EnumTranslation
{
    public class LocalizeEnumConverterTest
    {
        readonly IEnumLocalizer _enumLocalizer = Substitute.For<IEnumLocalizer>();
        readonly IValueConverter _target;

        public LocalizeEnumConverterTest()
        {
            _target = new LocalizeEnumConverter(_enumLocalizer);
        }

        private enum TestEnum
        {
            EnumValue,
        }

        [Fact]
        public void Converts_Enum_Value_To_Localized_Value()
        {
            _enumLocalizer.LocalizeEnum(Arg.Is(TestEnum.EnumValue)).Returns("localized value");

            var actual = _target.Convert(TestEnum.EnumValue, null, null, null);

            Assert.Equal("localized value", actual);
        }

        [Fact]
        public void Converts_Null_To_Null()
        {
            var actual = Assert.IsAssignableFrom<IValueConverter>(_target).Convert(null, null, null, null);
            Assert.Null(actual);
        }

        [Fact]
        public void Nothing_Bad_Happens_When_There_Is_No_Enum_Localizer()
        {
            var target = new LocalizeEnumConverter();

            var actual = target.Convert(TestEnum.EnumValue, null, null, null);

            Assert.Equal(TestEnum.EnumValue, actual);
        }
    }
}