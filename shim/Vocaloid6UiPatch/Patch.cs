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

    private static readonly System.Windows.Media.SolidColorBrush s_white = MakeWhite();

    private static System.Windows.Media.SolidColorBrush MakeWhite()
    {
        var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
        brush.Freeze();
        return brush;
    }

    // The app paints several dialog texts and glyphs in near-black on a dark
    // background (they were designed for its light flat styles). Anything dark
    // is flipped to white; already-light brushes are left alone.
    private static bool IsDark(Brush brush)
    {
        if (brush is not SolidColorBrush solid) return false;
        System.Windows.Media.Color c = solid.Color;
        return (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) < 128.0;
    }

    // Recolor a spinner button's logical content (a Path/TextBlock arrow glyph);
    // logical content survives the re-templating done by the style swap.
    private static void RecolorGlyph(object content)
    {
        switch (content)
        {
            case System.Windows.Shapes.Shape shape:
                if (IsDark(shape.Fill)) shape.Fill = s_white;
                if (IsDark(shape.Stroke)) shape.Stroke = s_white;
                break;
            case TextBlock text:
                if (IsDark(text.Foreground)) text.Foreground = s_white;
                break;
            case Panel panel:
                foreach (object child in panel.Children) RecolorGlyph(child);
                break;
            case System.Windows.Controls.Decorator decorator:
                RecolorGlyph(decorator.Child);
                break;
            case ContentControl cc:
                RecolorGlyph(cc.Content);
                break;
        }
    }

    private static void Apply(FrameworkElement element, ResourceDictionary dict)
    {
        if (element == null) return;

        if (element is TextBlock textBlock)
        {
            if (IsDark(textBlock.Foreground)) textBlock.Foreground = s_white;
            return;
        }

        if (element is Button spinner && IsSpinner(spinner))
        {
            if (dict["V6.SpinnerButton"] is Style spinnerStyle) spinner.Style = spinnerStyle;
            string name = spinner.Name ?? string.Empty;
            if (name.EndsWith("UpButton", StringComparison.Ordinal) ||
                name.EndsWith("DownButton", StringComparison.Ordinal))
            {
                // The app's arrows were drawn by its own button template, which
                // the style swap replaced — supply a chevron as content instead.
                // An app-set ContentTemplate would override direct content.
                spinner.ClearValue(ContentControl.ContentTemplateProperty);
                spinner.ClearValue(ContentControl.ContentTemplateSelectorProperty);
                bool up = name.EndsWith("UpButton", StringComparison.Ordinal);
                spinner.Content = new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse(up ? "M 0 3.5 L 3.5 0 L 7 3.5"
                                             : "M 0 0 L 3.5 3.5 L 7 0"),
                    Stroke = s_white,
                    StrokeThickness = 1.4,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
            }
            else
            {
                RecolorGlyph(spinner.Content);
            }
            return;
        }

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

        // The app sets Foreground locally on many dialog elements (its brushes
        // assume the light flat styles), and local values beat style setters —
        // flip dark ones to white on text-bearing controls.
        if (element is Label or System.Windows.Controls.Primitives.ToggleButton &&
            element is Control control && IsDark(control.Foreground))
            control.Foreground = s_white;
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
