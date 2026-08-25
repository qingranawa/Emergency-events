using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EmergencyEvents.Director;

/// <summary>
/// 事件定义注册表，只负责声明管理，不负责启动事件。
/// </summary>
public sealed class EventRegistry
{
    private readonly Dictionary<string, EventDefinition> definitions = new Dictionary<string, EventDefinition>(StringComparer.Ordinal);

    public IReadOnlyList<EventDefinition> All => new ReadOnlyCollection<EventDefinition>(
        definitions.Values.OrderBy(definition => definition.EventId, StringComparer.Ordinal).ToArray());

    public bool Register(EventDefinition definition)
    {
        if (definition is null
            || !Enum.IsDefined(typeof(EventResponseLevel), definition.RequiredResponseLevel)
            || definitions.ContainsKey(definition.EventId))
        {
            return false;
        }

        definitions.Add(definition.EventId, definition);
        return true;
    }

    public IReadOnlyList<EventDefinition> GetByCategory(EventCategory category)
    {
        return new ReadOnlyCollection<EventDefinition>(
            definitions.Values
                .Where(definition => definition.Category == category)
                .OrderBy(definition => definition.EventId, StringComparer.Ordinal)
                .ToArray());
    }
}
