using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyborgianStates.CommandHandling;
using CyborgianStates.Enums;
using Discord;

namespace CyborgianStates.Interfaces
{
    public interface IResponseBuilder
    {
        IResponseBuilder Failed(string reason);
        IResponseBuilder Success();
        IResponseBuilder WithContent(string content);
        IResponseBuilder WithCustomProperty(FieldKey key, string value);
        IResponseBuilder WithField(string key, string value, bool isInline = false);
        void Clear();
        CommandResponse Build();
    }

    public static class ResponseBuilderExtensions
    {
        public static IResponseBuilder WithTitle(this IResponseBuilder builder, string value) => builder.WithCustomProperty(FieldKey.Title, value);

        public static IResponseBuilder WithDescription(this IResponseBuilder builder, string value) => builder.WithCustomProperty(FieldKey.Description, value);

        public static IResponseBuilder WithFooter(this IResponseBuilder builder, string value) => builder.WithCustomProperty(FieldKey.Footer, value);

        public static IResponseBuilder WithThumbnailUrl(this IResponseBuilder builder, string value) => builder.WithCustomProperty(FieldKey.ThumbnailUrl, value);

        public static IResponseBuilder WithUrl(this IResponseBuilder builder, string value) => builder.WithCustomProperty(FieldKey.Url, value);
        public static IResponseBuilder WithColor(this IResponseBuilder builder, Color color) => builder.WithCustomProperty(FieldKey.Color, color.ToString());

        public static IResponseBuilder FailWithDescription(this IResponseBuilder builder, string reason)
        {
            var _builder = builder.Failed(null);
            _builder = _builder.WithTitle("Something went wrong");
            _builder = _builder.WithDescription(reason);
            _builder = _builder.WithColor(Color.Red);
            return _builder;
        }
        public static IResponseBuilder WithRandomColor(this IResponseBuilder builder)
        {
            var _rnd = new Random();
            var rndBytes = new byte[3];
            _rnd.NextBytes(rndBytes);
            return builder.WithColor(new Color(rndBytes[0], rndBytes[1], rndBytes[2]));
        }

        public static IResponseBuilder WithDefaults(this IResponseBuilder builder, string footer)
        {
            var _builder = builder.WithFooter(footer);
            return _builder.WithRandomColor();
        }
    }
}
