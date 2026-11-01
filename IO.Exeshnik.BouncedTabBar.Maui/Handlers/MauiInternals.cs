using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace IO.Exeshnik.BouncedTabBar.Maui;

// Every non-public MAUI member the handlers rely on, verified against Microsoft.Maui.Controls 10.0.120.
// A MAUI update can rename them, so a miss fails loudly in debug builds (and is traced in release) instead of
// silently showing native tabs. Callers keep them from being trimmed with [DynamicDependency].
internal static class MauiInternals
{
    // TabbedPage: Android TabbedPageManager, Windows MauiNavigationView.
    public const string TabbedPageManagerField = "_tabbedPageManager";
    public const string NavigationViewField = "_navigationView";

    // Android TabbedPageManager (internal class, public properties).
    public const string TabLayoutProperty = "TabLayout";
    public const string BottomNavigationViewProperty = "BottomNavigationView";

    private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Callers declare the members with DynamicDependency.")]
    public static object? GetMemberValue(object target, string memberName)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            if (type.GetField(memberName, MemberFlags) is { } field)
                return field.GetValue(target);

            if (type.GetProperty(memberName, MemberFlags) is { } property)
                return property.GetValue(target);
        }

        var message = $"{nameof(BouncedTabBar)}: MAUI member '{memberName}' was not found on {target.GetType().FullName}. The MAUI internals this library relies on have changed.";
        Trace.TraceWarning(message);
        Debug.Fail(message);
        return null;
    }
}
