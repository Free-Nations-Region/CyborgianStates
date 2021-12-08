using System.Collections.Generic;
using System.Linq;
using System.Text;
using CyborgianStates.CommandHandling;
using CyborgianStates.Enums;
using CyborgianStates.Interfaces;

namespace CyborgianStates.MessageHandling
{
    public class ConsoleResponseBuilder : BaseResponseBuilder
    {
        public override CommandResponse Build()
        {
            var builder = new StringBuilder();
            var contentEmpty = string.IsNullOrWhiteSpace(_response.Content);
            AddContent(builder, contentEmpty);
            if (_properties.Any() || _fields.Any())
            {
                AddSeperator(builder, contentEmpty);
                AddTitle(builder);
                AddDescription(builder);
                AddFields(builder);
                AddFooter(builder);
            }
            return new CommandResponse(_response.Status, builder.ToString());
        }

        private void AddContent(StringBuilder builder, bool contentEmpty)
        {
            if (!contentEmpty)
            {
                builder.AppendLine(_response.Content);
            }
        }

        private static void AddSeperator(StringBuilder builder, bool contentEmpty)
        {
            if (!contentEmpty)
            {
                builder.AppendLine("= = = = =");
            }
        }

        private void AddFooter(StringBuilder builder)
        {
            if (_properties.ContainsKey(FieldKey.Footer))
            {
                builder.AppendLine(_properties[FieldKey.Footer]);
            }
        }

        private void AddDescription(StringBuilder builder)
        {
            if (_properties.ContainsKey(FieldKey.Description))
            {
                builder.AppendLine(_properties[FieldKey.Description]);
                builder.AppendLine();
            }
        }

        private void AddTitle(StringBuilder builder)
        {
            if (_properties.ContainsKey(FieldKey.Title))
            {
                builder.AppendLine(_properties[FieldKey.Title]);
                builder.AppendLine();
            }
        }

        private void AddFields(StringBuilder builder)
        {
            var prevInlineNameLine = "";
            var prevInlineValueLine = "";
            foreach (var field in _fields)
            {
                var value = field.Value.Item1;
                var isInline = field.Value.Item2;
                var length = value.Length + 2;
                if (length > field.Key.Length && isInline)
                {
                    var fieldName = field.Key.PadRight(length);
                    value = value.PadRight(length);
                    prevInlineNameLine += fieldName;
                    prevInlineValueLine += value;
                }
                else
                {
                    if (!string.IsNullOrEmpty(prevInlineNameLine) && !string.IsNullOrEmpty(prevInlineValueLine))
                    {
                        builder.AppendLine(prevInlineNameLine);
                        builder.AppendLine(prevInlineValueLine);
                        builder.AppendLine();
                        prevInlineNameLine = string.Empty;
                        prevInlineValueLine = string.Empty;
                    }
                    builder.AppendLine(field.Key);
                    builder.AppendLine(value);
                    builder.AppendLine();
                }
            }
        }
    }
}
