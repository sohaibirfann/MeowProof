using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MeowProof.UI.Controls;

public partial class CatHero : UserControl
{
    public CatHero()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(CatHero),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x8C, 0x84, 0x78))));

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly DependencyProperty BadgeFillProperty = DependencyProperty.Register(
        nameof(BadgeFill), typeof(Brush), typeof(CatHero),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xCC, 0x7C, 0x57))));

    public Brush BadgeFill
    {
        get => (Brush)GetValue(BadgeFillProperty);
        set => SetValue(BadgeFillProperty, value);
    }

    public static readonly DependencyProperty LockedProperty = DependencyProperty.Register(
        nameof(Locked), typeof(bool), typeof(CatHero),
        new PropertyMetadata(false, OnLockedChanged));

    public bool Locked
    {
        get => (bool)GetValue(LockedProperty);
        set => SetValue(LockedProperty, value);
    }

    // Read-only DP that the XAML binds to; derived from Locked.
    private static readonly DependencyPropertyKey BadgeVisibilityPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(BadgeVisibility), typeof(Visibility), typeof(CatHero),
        new PropertyMetadata(Visibility.Collapsed));

    public static readonly DependencyProperty BadgeVisibilityProperty = BadgeVisibilityPropertyKey.DependencyProperty;

    public Visibility BadgeVisibility => (Visibility)GetValue(BadgeVisibilityProperty);

    private static void OnLockedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var hero = (CatHero)d;
        hero.SetValue(BadgeVisibilityPropertyKey, hero.Locked ? Visibility.Visible : Visibility.Collapsed);
    }
}
