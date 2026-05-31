using Eventix.Common.Models;
using System.Net;

namespace Eventix.Common.Exceptions
{
    public class BadRequestException : ApiException
    {
        public BadRequestException(string message)
            : base(message, HttpStatusCode.BadRequest, false, "400")
        {
        }

        public BadRequestException(SystemMessage systemMessage)
            : base(systemMessage, HttpStatusCode.BadRequest)
        {
        }
    }

    public class NotFoundException : ApiException
    {
        public NotFoundException(string message)
            : base(message, HttpStatusCode.NotFound, false, "404")
        {
        }

        public NotFoundException(SystemMessage systemMessage)
            : base(systemMessage, HttpStatusCode.NotFound)
        {
        }
    }

    public class UnauthorizedException : ApiException
    {
        public UnauthorizedException(string message)
            : base(message, HttpStatusCode.Unauthorized, false, "401")
        {
        }

        public UnauthorizedException(SystemMessage systemMessage)
            : base(systemMessage, HttpStatusCode.Unauthorized)
        {
        }
    }

    public class ForbiddenException : ApiException
    {
        public ForbiddenException(string message)
            : base(message, HttpStatusCode.Forbidden, false, "403")
        {
        }

        public ForbiddenException(SystemMessage systemMessage)
            : base(systemMessage, HttpStatusCode.Forbidden)
        {
        }
    }

    public class ConflictException : ApiException
    {
        public ConflictException(string message)
            : base(message, HttpStatusCode.Conflict, false, "409")
        {
        }

        public ConflictException(SystemMessage systemMessage)
            : base(systemMessage, HttpStatusCode.Conflict)
        {
        }
    }

    public class InternalServerErrorException : ApiException
    {
        public InternalServerErrorException(string message)
            : base(message, HttpStatusCode.InternalServerError, false, "500")
        {
        }

        public InternalServerErrorException(SystemMessage systemMessage)
            : base(systemMessage, HttpStatusCode.InternalServerError)
        {
        }
    }
}
