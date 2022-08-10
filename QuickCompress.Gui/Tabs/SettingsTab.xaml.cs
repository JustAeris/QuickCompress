using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using ModernWpf;
using QuickCompress.Core.Configuration;

namespace QuickCompress.Gui.Tabs;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public partial class SettingsTab
{
    public SettingsTab()
    {
        InitializeComponent();
        ThemeManager.Current.ApplicationTheme = Context.Options.Theme switch
        {
            ThemeType.Light => ApplicationTheme.Light,
            ThemeType.Dark => ApplicationTheme.Dark,
            ThemeType.Default => null,
            _ => throw new InvalidEnumArgumentException(nameof(Context.Options.Theme), (int) Context.Options.Theme, typeof(ThemeType))
        };

        switch (Context.Options.Theme)
        {
            case ThemeType.Default:
                DefaultThemeButton.IsChecked = true;
                break;
            case ThemeType.Light:
                LightThemeButton.IsChecked = true;
                break;
            case ThemeType.Dark:
                DarkThemeButton.IsChecked = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void LightThemeSelector_OnClick(object sender, RoutedEventArgs e)
    {
        Context.Options.Theme = ThemeType.Light;
        ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
    }

    private void DarkThemeSelector_OnClick(object sender, RoutedEventArgs e)
    {
        Context.Options.Theme = ThemeType.Dark;
        ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
    }

    private void DefaultThemeSelector_OnClick(object sender, RoutedEventArgs e)
    {
        Context.Options.Theme = ThemeType.Default;
        ThemeManager.Current.ApplicationTheme = null;
    }
}
