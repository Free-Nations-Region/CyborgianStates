using System;
using CyborgianStates.CommandHandling;
using CyborgianStates.Enums;
using Discord;

namespace CyborgianStates.MessageHandling
{
    public class DiscordResponseBuilder : BaseResponseBuilder
    {
        private readonly EmbedBuilder _builder;
        public DiscordResponseBuilder() : base()
        {
            _builder = new EmbedBuilder();
        }
        public override CommandResponse Build()
        {
            if (_properties.ContainsKey(FieldKey.Url))
            {
                _builder.WithUrl(_properties[FieldKey.Url]);
            }
            if (_properties.ContainsKey(FieldKey.Title))
            {
                _builder.WithTitle(_properties[FieldKey.Title]);
            }
            if (_properties.ContainsKey(FieldKey.Description))
            {
                _builder.WithDescription(_properties[FieldKey.Description]);
            }
            foreach (var field in _fields)
            {
                var value = field.Value.Item1;
                var isInline = field.Value.Item2;
                var fieldName = field.Key;
                _builder.AddField(fieldName, value, isInline);
            }
            if (_properties.ContainsKey(FieldKey.Footer))
            {
                _builder.WithFooter(_properties[FieldKey.Footer]);
            }
            if (_properties.ContainsKey(FieldKey.ThumbnailUrl))
            {
                _builder.WithThumbnailUrl(_properties[FieldKey.ThumbnailUrl]);
            }
            if (_properties.ContainsKey(FieldKey.Color))
            {
                _builder.WithColor(Convert.ToUInt32(_properties[FieldKey.Color][1..], 16));
            }
            return new CommandResponse(_response.Status, _response.Content) { ResponseObject = _builder.Build() };
        }
    }
}
