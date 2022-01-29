using Discord;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CyborgianStates.CommandHandling
{
    public class CommandDefinition
    {
        public CommandDefinition(Type type, List<string> triggers)
        {
            Trigger = new ReadOnlyCollection<string>(triggers);
            Type = type;
        }

        public ReadOnlyCollection<string> Trigger { get; }
        public Type Type { get; }

        public bool IsSlashCommand { get; init; }
        public bool IsGlobalSlashCommand { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
        public IReadOnlyList<SlashCommandParameter> SlashCommandParameters { get; init; } = new List<SlashCommandParameter>();
        public IReadOnlyList<SlashCommandPermission> SlashCommandPermissions { get; init; } = new List<SlashCommandPermission>();
    }

    public class SlashCommandParameter
    {
        public string Name { get; init; }
        public ApplicationCommandOptionType Type { get; init; }
        public bool IsRequired { get; init; }
        public string Description { get; init; }
    }

    public class SlashCommandPermission
    {
        /// <summary>
        /// Role or UserId for the permission
        /// </summary>
        public ulong Id { get; init; }
        /// <summary>
        /// If the id belongs to a user
        /// </summary>
        public bool IsUser { get; init; }
        /// <summary>
        /// If the command should only be visible for users with the specified permission
        /// </summary>
        public string RequiredInternalPermission { get; init; }
    }
}