using CheckYourBudget.App.Services;
using CheckYourBudget.Core;

namespace CheckYourBudget.App.Charts;

/// <summary>A part of a chart: how much, and its color.</summary>
public readonly record struct Slice(decimal Value, Color Color);

/// <summary>A ring split into the month's categories, with a line of text in the middle.</summary>
public sealed class DonutChart(IReadOnlyList<Slice> slices, string centerText, string centerCaption) : IDrawable
{
    public void Draw(ICanvas canvas, RectF rect)
    {
        float size = Math.Min(rect.Width, rect.Height);
        float thickness = size * 0.15f;
        float diameter = size - thickness;
        float x = rect.Center.X - diameter / 2;
        float y = rect.Center.Y - diameter / 2;

        canvas.StrokeSize = thickness;
        canvas.StrokeLineCap = LineCap.Butt;

        decimal total = slices.Sum(s => s.Value);
        if (total <= 0)
        {
            canvas.StrokeColor = AppColors.LeftOver;
            canvas.DrawEllipse(x, y, diameter, diameter);
        }
        else
        {
            // Clockwise from the top. Angles count counterclockwise from 3 o'clock, so the top is 90°.
            List<Slice> visible = [.. slices.Where(s => s.Value > 0)];
            float gap = visible.Count > 1 ? 1.5f : 0;
            float start = 90;
            foreach (Slice slice in visible)
            {
                float sweep = (float)(slice.Value / total) * 360;
                if (sweep > gap)
                {
                    canvas.StrokeColor = slice.Color;
                    canvas.DrawArc(x, y, diameter, diameter, start - gap / 2, start - sweep + gap / 2, clockwise: true, closed: false);
                }

                start -= sweep;
            }
        }

        canvas.FontColor = AppColors.Text;
        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
        canvas.FontSize = size * 0.13f;
        RectF inner = new(rect.Center.X - diameter / 2, rect.Center.Y - size * 0.16f, diameter, size * 0.18f);
        canvas.DrawString(centerText, inner, HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.FontColor = AppColors.Muted;
        canvas.Font = Microsoft.Maui.Graphics.Font.Default;
        canvas.FontSize = size * 0.08f;
        RectF caption = new(rect.Center.X - diameter / 2, rect.Center.Y + size * 0.03f, diameter, size * 0.12f);
        canvas.DrawString(centerCaption, caption, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}

/// <summary>A thin bar split into the month's categories, used on each History row.</summary>
public sealed class SplitBar(IReadOnlyList<Slice> slices) : IDrawable
{
    public void Draw(ICanvas canvas, RectF rect)
    {
        decimal total = slices.Sum(s => s.Value);
        canvas.FillColor = AppColors.LeftOver;
        canvas.FillRoundedRectangle(rect, rect.Height / 2);
        if (total <= 0)
        {
            return;
        }

        // Clip to the rounded bar so only its ends are round.
        PathF clip = new();
        clip.AppendRoundedRectangle(rect, rect.Height / 2);
        canvas.SaveState();
        canvas.ClipPath(clip);
        float left = rect.Left;
        foreach (Slice slice in slices.Where(s => s.Value > 0))
        {
            float width = (float)(slice.Value / total) * rect.Width;
            canvas.FillColor = slice.Color;
            canvas.FillRectangle(left, rect.Top, width, rect.Height);
            left += width;
        }

        canvas.RestoreState();
    }
}

/// <summary>One bar per month: spending stacked by category, with a line where the income is.</summary>
public sealed class MonthBarsChart(IReadOnlyList<MonthBarsChart.Bar> bars) : IDrawable
{
    public sealed record Bar(YearMonth Month, decimal Income, IReadOnlyList<Slice> Spending);

    public void Draw(ICanvas canvas, RectF rect)
    {
        if (bars.Count == 0)
        {
            return;
        }

        const float labelHeight = 20;
        const float top = 6;
        float chartHeight = rect.Height - labelHeight - top;
        decimal max = bars.Max(b => Math.Max(b.Income, b.Spending.Sum(s => s.Value)));
        if (max <= 0)
        {
            max = 1;
        }

        float slot = rect.Width / bars.Count;
        float barWidth = Math.Min(36, slot * 0.6f);
        float baseline = rect.Top + top + chartHeight;

        canvas.StrokeColor = AppColors.LeftOver;
        canvas.StrokeSize = 1;
        canvas.DrawLine(rect.Left, baseline, rect.Right, baseline);

        canvas.FontColor = AppColors.Muted;
        canvas.Font = Microsoft.Maui.Graphics.Font.Default;
        canvas.FontSize = 11;

        for (int i = 0; i < bars.Count; i++)
        {
            Bar bar = bars[i];
            float center = rect.Left + slot * i + slot / 2;
            float x = center - barWidth / 2;
            float bottom = baseline;
            foreach (Slice slice in bar.Spending.Where(s => s.Value > 0))
            {
                float height = (float)(slice.Value / max) * chartHeight;
                canvas.FillColor = slice.Color;
                canvas.FillRectangle(x, bottom - height, barWidth, height);
                bottom -= height;
            }

            if (bar.Income > 0)
            {
                float incomeY = baseline - (float)(bar.Income / max) * chartHeight;
                canvas.StrokeColor = AppColors.Text;
                canvas.StrokeSize = 2;
                canvas.DrawLine(x - 4, incomeY, x + barWidth + 4, incomeY);
            }

            string label = bar.Month.Month == 1 || i == 0
                ? $"{bar.Month.ShortTitle[..3]} {bar.Month.Year % 100:D2}"
                : bar.Month.ShortTitle[..3];
            canvas.DrawString(label, center - slot / 2, baseline + 2, slot, labelHeight, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }
}
