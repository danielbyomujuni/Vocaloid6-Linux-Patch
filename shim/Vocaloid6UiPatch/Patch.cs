using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace Vocaloid6UiPatch;

// Restyles VOCALOID 6 dialog windows (Yamaha.VOCALOID.DialogBase) at runtime.
// The app's dialogs use hardcoded flat-light styles compiled into VOCALOID6.dll
// (DialogFlatButton, DialogFlatComboBox, DialogRadioButton, ...), so a WPF theme
// assembly can't reach them; explicit Style assignments are swapped here instead.
public static class Patch
{
    private static ResourceDictionary s_dict;
    private static bool s_installed;

    public static void Install()
    {
        if (s_installed) return;
        s_installed = true;
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnWindowLoaded), handledEventsToo: true);
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Window window || !IsVocaloidDialog(window)) return;
            ResourceDictionary dict = LoadDictionary();
            if (dict == null) return;
            if (!window.Resources.MergedDictionaries.Contains(dict))
                window.Resources.MergedDictionaries.Add(dict);
            Restyle(window, dict);
        }
        catch
        {
            // A skin failure must never take the dialog down.
        }
    }

    private static bool IsVocaloidDialog(Window window)
    {
        for (Type t = window.GetType(); t != null; t = t.BaseType)
            if (t.FullName == "Yamaha.VOCALOID.DialogBase")
                return true;
        return false;
    }

    private static ResourceDictionary LoadDictionary()
    {
        if (s_dict != null) return s_dict;
        using System.IO.Stream stream =
            typeof(Patch).Assembly.GetManifestResourceStream("Vocaloid6UiPatch.Styles.xaml");
        if (stream == null) return null;
        s_dict = (ResourceDictionary)XamlReader.Load(stream);
        return s_dict;
    }

    private static void Restyle(DependencyObject node, ResourceDictionary dict)
    {
        int count = VisualTreeHelper.GetChildrenCount(node);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(node, i);
            Apply(child as FrameworkElement, dict);
            Restyle(child, dict);
        }
    }

    private static void Apply(FrameworkElement element, ResourceDictionary dict)
    {
        if (element == null) return;

        Type key = element switch
        {
            ComboBoxItem => typeof(ComboBoxItem),
            ComboBox => typeof(ComboBox),
            System.Windows.Controls.Primitives.ToggleButton tb =>
                tb is RadioButton ? typeof(RadioButton) :
                tb is CheckBox ? typeof(CheckBox) : null,
            Button button when !IsSpinner(button) => typeof(Button),
            TextBox => typeof(TextBox),
            ListBoxItem => typeof(ListBoxItem),
            ListBox => typeof(ListBox),
            Label => typeof(Label),
            _ => null,
        };
        if (key == null) return;

        if (dict[key] is Style style && style.TargetType.IsInstanceOfType(element))
            element.Style = style;
    }

    // The Track Count / Part Duration up-down arrows are ~16px app-templated
    // buttons; the padded dialog button style would wreck their layout.
    private static bool IsSpinner(Button button)
    {
        string name = button.Name ?? string.Empty;
        if (name.EndsWith("UpButton", StringComparison.Ordinal) ||
            name.EndsWith("DownButton", StringComparison.Ordinal))
            return true;
        return button.ActualWidth > 0 && button.ActualWidth < 26;
    }
}
