using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyborgianStates.CommandHandling;
using CyborgianStates.Enums;
using CyborgianStates.Interfaces;

namespace CyborgianStates.MessageHandling
{
    public abstract class BaseResponseBuilder : IResponseBuilder
    {
        protected CommandResponse _response;
        protected readonly Dictionary<FieldKey, string> _properties = new();
        protected readonly Dictionary<string, (string, bool)> _fields = new();

        public abstract CommandResponse Build();

        public BaseResponseBuilder()
        {
            _response = new CommandResponse();
        }

        public IResponseBuilder Failed(string reason)
        {
            _response.Status = CommandStatus.Error;
            WithContent(reason);
            return this;
        }

        public IResponseBuilder Success()
        {
            _response.Status = CommandStatus.Success;
            return this;
        }

        public IResponseBuilder WithContent(string content)
        {
            _response.Content = content;
            return this;
        }

        public IResponseBuilder WithCustomProperty(FieldKey key, string value)
        {
            _properties[key] = value;
            return this;
        }

        public IResponseBuilder WithField(string key, string value, bool isInline = false)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), $"Key: {key}");
            }

            _fields[key] = (value, isInline);
            return this;
        }

        public void Clear()
        {
            _properties.Clear();
            _fields.Clear();
            _response = null;
        }
    }
}