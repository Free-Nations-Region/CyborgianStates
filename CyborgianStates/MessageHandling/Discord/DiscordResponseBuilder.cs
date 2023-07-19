using System;
using System.Linq;
using CyborgianStates.CommandHandling;
using CyborgianStates.Enums;
using Discord;

namespace CyborgianStates.MessageHandling
{
    public class DiscordResponseBuilder : BaseResponseBuilder
    {
        public override CommandResponse Build()
        {
            var builder = new EmbedBuilder();
            if (_properties.ContainsKey(FieldKey.Url))
            {
                builder.WithUrl(_properties[FieldKey.Url]);
            }
            if (_properties.ContainsKey(FieldKey.Title))
            {
                builder.WithTitle(_properties[FieldKey.Title]);
            }
            if (_properties.ContainsKey(FieldKey.Description))
            {
                builder.WithDescription(_properties[FieldKey.Description]);
            }
            foreach (var field in _fields)
            {
                var value = field.Value.Item1;
                var isInline = field.Value.Item2;
                var fieldName = field.Key;
                builder.AddField(fieldName, value, isInline);
            }
            if (_properties.ContainsKey(FieldKey.Footer))
            {
                builder.WithFooter(_properties[FieldKey.Footer]);
            }
            if (_properties.ContainsKey(FieldKey.ThumbnailUrl))
            {
                builder.WithThumbnailUrl(_properties[FieldKey.ThumbnailUrl]);
            }
            if (_properties.ContainsKey(FieldKey.Color))
            {
                builder.WithColor(Convert.ToUInt32(_properties[FieldKey.Color][1..], 16));
            }
            var embed = builder.Build();
            if(!_fields.Any() && !_properties.Any())
            {
                embed = null;
            }
            return new CommandResponse(_response.Status, _response.Content) { ResponseObject = embed };
        }
    }
}
