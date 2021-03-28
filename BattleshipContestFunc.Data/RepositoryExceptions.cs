using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Data
{
    public class RepositoryException : Exception
    {
        public RepositoryException()
        {
        }

        public RepositoryException(string? message) : base(message)
        {
        }

        public RepositoryException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected RepositoryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    public class InvalidPartitionKeyException : RepositoryException
    {
        public InvalidPartitionKeyException()
        {
        }

        public InvalidPartitionKeyException(string? message) : base(message)
        {
        }

        public InvalidPartitionKeyException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected InvalidPartitionKeyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    public class ItemNotFoundException : RepositoryException
    {
        public ItemNotFoundException()
        {
        }

        public ItemNotFoundException(string? message) : base(message)
        {
        }

        public ItemNotFoundException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ItemNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
