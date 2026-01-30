using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Options
{
    public abstract class GenericOptions
    {
        protected abstract (string?, string)[] Values { get; }

        public virtual (bool,string) Valid()
        {
            var errors = new List<string>();

            foreach (var (value, name) in Values)
            {
                if (string.IsNullOrEmpty(value))
                {
                    errors.Add($"Property {name} is empty");
                }
            }

            return (errors.Count == 0, string.Join("; ", errors));
        }
    }
}
