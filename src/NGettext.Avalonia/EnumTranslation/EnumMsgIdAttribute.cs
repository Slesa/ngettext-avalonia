namespace NGettext.Avalonia.EnumTranslation
{
    public class EnumMsgIdAttribute : Attribute
    {
        public EnumMsgIdAttribute(string msgId)
        {
            MsgId = msgId;
        }

        public string MsgId { get; }
    }
}