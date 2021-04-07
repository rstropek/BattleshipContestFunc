using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;

namespace BattleshipContestFunc
{
    public static class ExceptionExtensions
    {
        public static string GetFullDescription(this Exception ex)
        {
            var message = new StringBuilder();

            message.Append(ex.GetType().FullName);
            message.Append('\n');

            if (!string.IsNullOrEmpty(ex.Message))
            {
                message.Append(ex.Message);
                message.Append('\n');
            }

            foreach (var detail in ex.Data.Keys)
            {
                message.AppendFormat("{0}: {1}", detail, ex.Data[detail]);
                message.Append('\n');
            }

            var messageString = message.ToString();
            return messageString;
        }
    }

    public class PlayerCommunicationException : ApplicationException
    {
        public PlayerCommunicationException()
        {
        }

        public PlayerCommunicationException(string? message = null) : base(message)
        {
        }

        public PlayerCommunicationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public PlayerCommunicationException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }

    public class InvalidStatusCodeException : PlayerCommunicationException
    {
        public InvalidStatusCodeException()
        {
        }

        public InvalidStatusCodeException(HttpResponseMessage res, string? message = null) : this(res, message, null)
        {
        }

        public InvalidStatusCodeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public InvalidStatusCodeException(HttpResponseMessage res, string? message, Exception? innerException)
            : base(message, innerException)
        {
            StatusCode = res.StatusCode;
            Data.Add(nameof(StatusCode), StatusCode);
            try
            {
                Data.Add(nameof(Content), res.Content.ReadAsStringAsync().Result);
            }
            catch { /* Ignore errors */ }
        }

        public HttpStatusCode? StatusCode { get; set; }

        public string? Content { get; set; }
    }

    public class TimeoutException : PlayerCommunicationException
    {
        public TimeoutException()
        {
        }

        public TimeoutException(string? message) : base(message)
        {
        }

        public TimeoutException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public TimeoutException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }

    public class InvalidShotException : PlayerCommunicationException
    {
        public InvalidShotException()
        {
        }

        public InvalidShotException(string? shot, string? message) : base(message)
        {
            Shot = shot;
            Data.Add(nameof(Shot), shot);
        }

        public InvalidShotException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public InvalidShotException(string? shot, string? message, Exception? innerException) : base(message, innerException)
        {
            Shot = shot;
            Data.Add(nameof(Shot), shot);
        }

        public string? Shot { get; set; }
    }
}
