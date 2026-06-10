using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MeowProof.Models;
using MeowProof.Services;
using Microsoft.Win32;
using WpfPath = System.Windows.Shapes.Path;

namespace MeowProof.UI;

public partial class SoundPickerWindow : Window
{
    private string _selected;
    private string? _customPath;

    // Accent / surface brushes resolved once on load
    private SolidColorBrush BrushOf(string key) => (SolidColorBrush)FindResource(key);

    public SoundPickerWindow()
    {
        InitializeComponent();
        _selected   = AppSettings.Current.SelectedSound;
        _customPath = AppSettings.Current.CustomSoundPath;
        BuildList();
    }

    private void BuildList()
    {
        SoundList.Children.Clear();

        foreach (var name in SoundService.BuiltinSounds)
            SoundList.Children.Add(MakeRow(name, isCustom: false));

        // Custom row — shown when a custom file has been imported
        if (_customPath is not null)
            SoundList.Children.Add(MakeCustomRow());
    }

    private Border MakeRow(string name, bool isCustom)
    {
        bool isSelected = isCustom
            ? _selected == "Custom"
            : _selected == name;

        var dot = new Ellipse
        {
            Width = 18, Height = 18,
            Stroke = isSelected ? BrushOf("Accent") : BrushOf("Border"),
            StrokeThickness = 2,
            Fill = isSelected ? BrushOf("Accent") : Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Center,
        };

        if (isSelected)
        {
            var inner = new Ellipse
            {
                Width = 7, Height = 7,
                Fill = Brushes.White,
                IsHitTestVisible = false,
            };
            dot.Tag = inner;
        }

        var label = new TextBlock
        {
            Text = isCustom ? ShortPath(_customPath!) : name,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = isSelected ? BrushOf("Accent") : BrushOf("Text"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0),
        };

        var playBtn = new Button
        {
            Width = 28, Height = 28,
            Background = BrushOf("Surface2"),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            ToolTip = "Preview",
            Visibility = name == "Silence" ? Visibility.Collapsed : Visibility.Visible,
            Content = new WpfPath
            {
                Data = Geometry.Parse("M8,5 L8,19 L20,12 Z"),
                Fill = BrushOf("Accent"),
                Stretch = Stretch.Uniform,
                Width = 10, Height = 10,
            },
        };

        var playTemplate = new ControlTemplate(typeof(Button));
        var playFactory = new FrameworkElementFactory(typeof(Border));
        playFactory.SetValue(Border.BackgroundProperty, BrushOf("Surface2"));
        playFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        var cpFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        cpFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        cpFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        playFactory.AppendChild(cpFactory);
        playTemplate.VisualTree = playFactory;
        playBtn.Template = playTemplate;

        string capturedName = isCustom ? "Custom" : name;
        playBtn.Click += (_, _) => SoundService.Preview(capturedName);

        var rowGrid = new Grid { Margin = new Thickness(0, 1, 0, 1) };
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Dot + optional inner dot overlay
        var dotHost = new Grid { Width = 20, VerticalAlignment = VerticalAlignment.Center };
        dotHost.Children.Add(dot);
        if (dot.Tag is Ellipse innerDot)
            dotHost.Children.Add(innerDot);

        Grid.SetColumn(dotHost, 0);
        Grid.SetColumn(label, 1);
        Grid.SetColumn(playBtn, 2);
        rowGrid.Children.Add(dotHost);
        rowGrid.Children.Add(label);
        rowGrid.Children.Add(playBtn);

        var row = new Border
        {
            Padding = new Thickness(10, 9, 10, 9),
            CornerRadius = new CornerRadius(10),
            Background = isSelected ? BrushOf("AccentSoft") : Brushes.Transparent,
            Cursor = Cursors.Hand,
            Child = rowGrid,
        };

        string selectionKey = isCustom ? "Custom" : name;
        row.MouseLeftButtonUp += (_, _) =>
        {
            _selected = selectionKey;
            BuildList();
        };

        return row;
    }

    private Border MakeCustomRow()
    {
        bool isSelected = _selected == "Custom";

        var dot = new Ellipse
        {
            Width = 18, Height = 18,
            Stroke = isSelected ? BrushOf("Accent") : BrushOf("Border"),
            StrokeThickness = 2,
            Fill = isSelected ? BrushOf("Accent") : Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var dotHost = new Grid { Width = 20, VerticalAlignment = VerticalAlignment.Center };
        dotHost.Children.Add(dot);
        if (isSelected)
            dotHost.Children.Add(new Ellipse { Width = 7, Height = 7, Fill = Brushes.White, IsHitTestVisible = false });

        var label = new TextBlock
        {
            Text = ShortPath(_customPath!),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = isSelected ? BrushOf("Accent") : BrushOf("Text"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        var playBtn = new Button
        {
            Width = 28, Height = 28,
            Background = BrushOf("Surface2"),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            ToolTip = "Preview",
            Content = new WpfPath
            {
                Data = Geometry.Parse("M8,5 L8,19 L20,12 Z"),
                Fill = BrushOf("Accent"),
                Stretch = Stretch.Uniform,
                Width = 10, Height = 10,
            },
        };

        var pt = new ControlTemplate(typeof(Button));
        var pf = new FrameworkElementFactory(typeof(Border));
        pf.SetValue(Border.BackgroundProperty, BrushOf("Surface2"));
        pf.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        var cf = new FrameworkElementFactory(typeof(ContentPresenter));
        cf.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        cf.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        pf.AppendChild(cf);
        pt.VisualTree = pf;
        playBtn.Template = pt;
        playBtn.Click += (_, _) => SoundService.Preview("Custom");

        var rowGrid = new Grid { Margin = new Thickness(0, 1, 0, 1) };
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(dotHost, 0);
        Grid.SetColumn(label, 1);
        Grid.SetColumn(playBtn, 2);
        rowGrid.Children.Add(dotHost);
        rowGrid.Children.Add(label);
        rowGrid.Children.Add(playBtn);

        var row = new Border
        {
            Padding = new Thickness(10, 9, 10, 9),
            CornerRadius = new CornerRadius(10),
            Background = isSelected ? BrushOf("AccentSoft") : Brushes.Transparent,
            Cursor = Cursors.Hand,
            Child = rowGrid,
        };

        row.MouseLeftButtonUp += (_, _) => { _selected = "Custom"; BuildList(); };
        return row;
    }

    private static string ShortPath(string path) =>
        System.IO.Path.GetFileName(path) is { Length: > 0 } f ? f : path;

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Import Sound",
            Filter = "Audio files (*.wav;*.mp3)|*.wav;*.mp3",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog(this) != true) return;

        var destDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MeowProof", "Sounds");
        Directory.CreateDirectory(destDir);
        var dest = System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(dlg.FileName));
        if (!string.Equals(dlg.FileName, dest, StringComparison.OrdinalIgnoreCase))
            File.Copy(dlg.FileName, dest, overwrite: true);

        _customPath = dest;
        _selected = "Custom";
        BuildList();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        AppSettings.Current.SelectedSound  = _selected;
        AppSettings.Current.CustomSoundPath = _customPath;
        AppSettings.Save();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }
}
