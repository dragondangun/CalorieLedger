using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CalorieLedger.ViewModels.History;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CalorieLedger.Controls;

public sealed class WeeklyTrendChart:Control {
    private const double LeftMargin = 50d;
    private const double TopMargin = 16d;
    private const double RightMargin = 48d;
    private const double BottomMargin = 34d;
    private const int GridLineCount = 4;

    public static readonly StyledProperty<IReadOnlyList<WeeklyTrendChartPoint>?> PointsProperty =
        AvaloniaProperty.Register<WeeklyTrendChart, IReadOnlyList<WeeklyTrendChartPoint>?>(nameof(Points));

    public static readonly StyledProperty<IBrush> FoodBrushProperty =
        AvaloniaProperty.Register<WeeklyTrendChart, IBrush>(nameof(FoodBrush), Brushes.DodgerBlue);

    public static readonly StyledProperty<IBrush> AdjustedBrushProperty =
        AvaloniaProperty.Register<WeeklyTrendChart, IBrush>(nameof(AdjustedBrush), Brushes.MediumSeaGreen);

    public static readonly StyledProperty<IBrush> WeightBrushProperty =
        AvaloniaProperty.Register<WeeklyTrendChart, IBrush>(nameof(WeightBrush), Brushes.Goldenrod);

    public static readonly StyledProperty<IBrush> GridBrushProperty =
        AvaloniaProperty.Register<WeeklyTrendChart, IBrush>(nameof(GridBrush), Brushes.Gray);

    public IReadOnlyList<WeeklyTrendChartPoint>? Points {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public IBrush FoodBrush {
        get => GetValue(FoodBrushProperty);
        set => SetValue(FoodBrushProperty, value);
    }

    public IBrush AdjustedBrush {
        get => GetValue(AdjustedBrushProperty);
        set => SetValue(AdjustedBrushProperty, value);
    }

    public IBrush WeightBrush {
        get => GetValue(WeightBrushProperty);
        set => SetValue(WeightBrushProperty, value);
    }

    public IBrush GridBrush {
        get => GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    static WeeklyTrendChart() {
        AffectsRender<WeeklyTrendChart>(
            PointsProperty,
            FoodBrushProperty,
            AdjustedBrushProperty,
            WeightBrushProperty,
            GridBrushProperty
        );
    }

    public override void Render(DrawingContext context) {
        base.Render(context);

        var points = Points;
        if(points is null || points.Count == 0 || Bounds.Width < 220d || Bounds.Height < 160d) {
            return;
        }

        var plot = new Rect(
            LeftMargin,
            TopMargin,
            Bounds.Width - LeftMargin - RightMargin,
            Bounds.Height - TopMargin - BottomMargin
        );

        var calorieValues = points
            .SelectMany(point => new[] { point.FoodCaloriesKcal, point.AdjustedCaloriesKcal })
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();

        var weightValues = points
            .Where(point => point.WeightKg.HasValue)
            .Select(point => point.WeightKg!.Value)
            .ToArray();

        var calorieRange = CreateRange(calorieValues);
        var weightRange = CreateRange(weightValues);

        DrawGrid(
            context,
            points,
            plot,
            calorieRange,
            weightValues.Length > 0 ? weightRange : null
        );

        DrawSeries(
            context,
            points,
            plot,
            point => point.FoodCaloriesKcal,
            calorieRange,
            FoodBrush,
            2d,
            3d
        );

        DrawSeries(
            context,
            points,
            plot,
            point => point.AdjustedCaloriesKcal,
            calorieRange,
            AdjustedBrush,
            2d,
            3d
        );

        if(weightValues.Length > 0) {
            DrawSeries(
                context,
                points,
                plot,
                point => point.WeightKg,
                weightRange,
                WeightBrush,
                2d,
                3d
            );
        }
    }

    private void DrawGrid(
        DrawingContext context,
        IReadOnlyList<WeeklyTrendChartPoint> points,
        Rect plot,
        (double Min, double Max) calorieRange,
        (double Min, double Max)? weightRange
    ) {
        var gridPen = new Pen(GridBrush, 1d);

        for(var index = 0; index <= GridLineCount; index++) {
            var ratio = index / (double)GridLineCount;
            var y = plot.Bottom - ratio * plot.Height;

            context.DrawLine(
                gridPen,
                new Point(plot.Left, y),
                new Point(plot.Right, y)
            );

            var calories = calorieRange.Min + ratio * (calorieRange.Max - calorieRange.Min);
            var calorieLabel = CreateText($"{calories:0}", GridBrush, 10d);

            context.DrawText(
                calorieLabel,
                new Point(plot.Left - calorieLabel.Width - 6d, y - calorieLabel.Height / 2d)
            );

            if(weightRange is { } range) {
                var weight = range.Min + ratio * (range.Max - range.Min);
                var weightLabel = CreateText($"{weight:0.0}", WeightBrush, 10d);

                context.DrawText(
                    weightLabel,
                    new Point(plot.Right + 6d, y - weightLabel.Height / 2d)
                );
            }
        }

        for(var index = 0; index < points.Count; index++) {
            var x = GetX(plot, index, points.Count);
            var label = CreateText(points[index].Label, GridBrush, 10d);

            context.DrawText(
                label,
                new Point(x - label.Width / 2d, plot.Bottom + 8d)
            );
        }
    }

    private static void DrawSeries(
        DrawingContext context,
        IReadOnlyList<WeeklyTrendChartPoint> points,
        Rect plot,
        Func<WeeklyTrendChartPoint, decimal?> valueSelector,
        (double Min, double Max) range,
        IBrush brush,
        double thickness,
        double markerRadius
    ) {
        var pen = new Pen(brush, thickness);
        Point? previous = null;

        for(var index = 0; index < points.Count; index++) {
            var value = valueSelector(points[index]);

            if(value is null) {
                previous = null;
                continue;
            }

            var current = new Point(
                GetX(plot, index, points.Count),
                ScaleY((double)value.Value, plot, range)
            );

            if(previous is { } previousPoint) {
                context.DrawLine(pen, previousPoint, current);
            }

            context.DrawEllipse(
                brush,
                null,
                current,
                markerRadius,
                markerRadius
            );

            previous = current;
        }
    }

    private static double GetX(Rect plot, int index, int count) {
        if(count == 1) {
            return plot.Left + plot.Width / 2d;
        }

        return plot.Left + index * plot.Width / (count - 1);
    }

    private static double ScaleY(
        double value,
        Rect plot,
        (double Min, double Max) range
    ) {
        var ratio = (value - range.Min) / (range.Max - range.Min);
        return plot.Bottom - ratio * plot.Height;
    }

    private static (double Min, double Max) CreateRange(IEnumerable<decimal> values) {
        var source = values.Select(value => (double)value).ToArray();

        if(source.Length == 0) {
            return (0d, 1d);
        }

        var min = source.Min();
        var max = source.Max();

        if(Math.Abs(max - min) < 0.001d) {
            var padding = Math.Max(Math.Abs(max) * 0.05d, 1d);
            return (min - padding, max + padding);
        }

        var rangePadding = (max - min) * 0.1d;
        return (min - rangePadding, max + rangePadding);
    }

    private static FormattedText CreateText(string text, IBrush brush, double fontSize) {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            fontSize,
            brush
        );
    }
}
