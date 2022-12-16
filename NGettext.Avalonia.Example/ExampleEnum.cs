using NGettext.Avalonia.EnumTranslation;

namespace NGettext.Avalonia.Example
{
    public enum ExampleEnum
    {
        [EnumMsgId("Some value")]
        SomeValue,

        [EnumMsgId("Some other value")]
        SomeOtherValue,

        [EnumMsgId("Some third value")]
        SomeThirdValue,

        [EnumMsgId("EnumMsgId example|Some fourth value")]
        SomeFourthValue,

        SomeValueWithoutEnumMsgId,
    }
}