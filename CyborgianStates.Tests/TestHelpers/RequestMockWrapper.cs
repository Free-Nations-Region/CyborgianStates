using NationStatesSharp.Enums;
using System;
using System.Threading;

namespace CyborgianStates.Tests.Helpers
{
    public class RequestMockWrapper
    {
        public RequestStatus Status { get; init; }
        public object Response { get; init; }
        public Exception Exception { get; init; }
        public CancellationToken Token { get; init; }
    }
}