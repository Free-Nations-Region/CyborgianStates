using NationStatesSharp.Enums;
using System;

namespace CyborgianStates.Tests.Helpers
{
    public class RequestMockWrapper
    {
        public RequestStatus Status { get; init; }
        public object Response { get; init; }
        public Exception Exception { get; init; }
    }
}