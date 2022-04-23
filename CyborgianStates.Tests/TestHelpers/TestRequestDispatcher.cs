using CyborgianStates.Enums;
using CyborgianStates.Interfaces;
using NationStatesSharp;
using NationStatesSharp.Enums;
using NationStatesSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CyborgianStates.Tests.Helpers
{

    public class TestRequestDispatcher : IRequestDispatcher
    {
        private List<RequestMockWrapper> requests = new List<RequestMockWrapper>();

        public void PrepareNextRequest(RequestStatus status = RequestStatus.Success, object response = null, Exception exception = null)
        {
            requests.Add(new RequestMockWrapper()
            {
                Status = status,
                Response = response,
                Exception = exception,
            });
        }

        public void AddToQueue(Request request, uint priority = 1000)
        {
            throw new NotImplementedException();
        }

        public void AddToQueue(IEnumerable<Request> requests, uint priority = 1000)
        {
            throw new NotImplementedException();
        }

        public void Dispatch(IEnumerable<Request> requests, int priority) => throw new NotImplementedException();

        public void Dispatch(Request request, uint priority = 1000)
        {
            if (!requests.Any())
            {
                var reason = "TestDispatcher does not accept unprepared requests.";
                request.Fail(new InvalidOperationException(reason));
            }
            else
            {
                var mockRequest = requests.Take(1).First();
                requests.Remove(mockRequest);
                if (mockRequest.Status == RequestStatus.Success)
                {
                    request.Complete(mockRequest.Response);
                }
                else if (mockRequest.Status == RequestStatus.Failed)
                {
                    request.Fail(mockRequest.Exception);
                }
            }
        }

        public void Dispatch(IEnumerable<Request> requests, uint priority = 1000)
        {
            throw new NotImplementedException();
        }

        public void Shutdown() => throw new NotImplementedException();

        public void Start() => throw new NotImplementedException();
    }
}