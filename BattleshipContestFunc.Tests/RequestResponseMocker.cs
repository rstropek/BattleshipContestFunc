using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Tests
{
    public record RequestResponseMock(Mock<HttpRequestData> RequestMock, Mock<HttpResponseData> ResponseMock,
        MemoryStream ResponseStream, HttpHeadersCollection Headers)
    {
        public string ResponseBodyAsString
        {
            get
            {
                ResponseStream.Seek(0, SeekOrigin.Begin);
                var bytes = new byte[ResponseStream.Length];
                ResponseStream.Read(bytes.AsSpan());
                return Encoding.UTF8.GetString(bytes);
            }
        }
    }

    public static class RequestResponseMocker
    {
        public static RequestResponseMock Create(string? requestBody = null)
        {
            var context = Mock.Of<FunctionContext>();
            var requestMock = new Mock<HttpRequestData>(context);
            var responseMock = new Mock<HttpResponseData>(context);
            requestMock.Setup(r => r.CreateResponse()).Returns(responseMock.Object);
            if (requestBody != null)
            {
                requestMock.SetupGet(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(requestBody)));
            }

            var responseStream = new MemoryStream();
            var headers = new HttpHeadersCollection();
            responseMock.SetupGet(r => r.Body).Returns(responseStream);
            responseMock.SetupGet(r => r.Headers).Returns(headers);
            responseMock.SetupProperty(r => r.StatusCode);

            return new(requestMock, responseMock, responseStream, headers);
        }
    }
}
