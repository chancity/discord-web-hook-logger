using System;

namespace discord_web_hook_logger.Models
{
    internal class LogMessageItem
    {
        internal string MessageType { get; set; }

        internal string Message { get; set; }
        internal string StackTrace { get; set; }
        internal int Color { get; set; }
        internal DateTime Time { get; set; }

        protected bool Equals(LogMessageItem other)
        {
            return string.Equals(MessageType, other.MessageType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((LogMessageItem) obj);
        }

        public override int GetHashCode()
        {
            return MessageType != null ? MessageType.GetHashCode() : 0;
        }
    }
}