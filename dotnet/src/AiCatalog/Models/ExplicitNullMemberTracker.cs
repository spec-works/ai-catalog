using System.Runtime.CompilerServices;

namespace SpecWorks.AiCatalog.Models;

internal static class ExplicitNullMemberTracker
{
    private static readonly ConditionalWeakTable<object, HashSet<string>> s_members = new();

    public static void Mark(object instance, string memberName) =>
        s_members.GetOrCreateValue(instance).Add(memberName);

    public static void Clear(object instance, string memberName)
    {
        if (s_members.TryGetValue(instance, out var members))
            members.Remove(memberName);
    }

    public static bool IsMarked(object instance, string memberName) =>
        s_members.TryGetValue(instance, out var members) && members.Contains(memberName);
}
