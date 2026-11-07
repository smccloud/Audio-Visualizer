#if !WINDOWS
using SkiaSharp;
using System.Drawing;
using System;
using System.IO;

namespace Compat.Drawing
{
    public readonly struct Color
    {
        public byte A { get; }
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public Color(byte a, byte r, byte g, byte b) { A = a; R = r; G = g; B = b; }

        public static Color Black => new(255, 0, 0, 0);
        public static Color White => new(255, 255, 255, 255);
        public static Color Red => new(255, 255, 0, 0);
        public static Color Lime => new(255, 0, 255, 0);
        public static Color LimeGreen => new(255, 50, 205, 50);
        public static Color Yellow => new(255, 255, 255, 0);

        public static Color FromArgb(int a, int r, int g, int b) =>
            new((byte)Math.Clamp(a, 0, 255), (byte)Math.Clamp(r, 0, 255),
                (byte)Math.Clamp(g, 0, 255), (byte)Math.Clamp(b, 0, 255));

        public static Color FromArgb(int r, int g, int b) => FromArgb(255, r, g, b);

        public static Color FromArgb(int a, Color c) => new((byte)Math.Clamp(a, 0, 255), c.R, c.G, c.B);

        public static implicit operator SKColor(Color c) => new(c.R, c.G, c.B, c.A);
    }

    public struct Rectangle
    {
        public int X, Y, Width, Height;
        public Rectangle(int x, int y, int w, int h) { X = x; Y = y; Width = w; Height = h; }
        public int Left => X;
        public int Right => X + Width;
        public int Top => Y;
        public int Bottom => Y + Height;
    }

    public struct RectangleF
    {
        public float X, Y, Width, Height;
        public RectangleF(float x, float y, float w, float h) { X = x; Y = y; Width = w; Height = h; }
    }

    public struct PointF
    {
        public float X, Y;
        public PointF(float x, float y) { X = x; Y = y; }
    }

    public enum FontStyle { Regular, Bold, Italic }

    public enum SmoothingMode { Default, AntiAlias }

    public enum TextRenderingHint { SystemDefault, ClearTypeGridFit, AntiAlias }

    public enum LineCap { Flat, Round, Square }

    public enum LinearGradientMode { Vertical, Horizontal, ForwardDiagonal, BackwardDiagonal }

    public sealed class Font : IDisposable
    {
        public string Name { get; }
        public float Size { get; }
        public FontStyle Style { get; }
        public Font(string name, float size, FontStyle style = FontStyle.Regular)
        {
            Name = name; Size = size; Style = style;
        }
        public void Dispose() { }
    }

    public abstract class Brush : IDisposable
    {
        public Color Color { get; set; }
        public void Dispose() { }
    }

    public sealed class SolidBrush : Brush
    {
        public SolidBrush(Color color) { Color = color; }
    }

    public sealed class Pen : IDisposable
    {
        public Color Color { get; set; }
        public float Width { get; set; }
        public LineCap StartCap { get; set; }
        public LineCap EndCap { get; set; }
        public Pen(Color color, float width = 1f)
        {
            Color = color; Width = width; StartCap = LineCap.Flat; EndCap = LineCap.Flat;
        }
        public void Dispose() { }
    }

    public sealed class LinearGradientBrush : Brush, IDisposable
    {
        public Rectangle Rect;
        public Color StartColor, EndColor;
        public LinearGradientMode Mode;
        public LinearGradientBrush(Rectangle rect, Color c1, Color c2, LinearGradientMode mode)
        {
            Rect = rect; StartColor = c1; EndColor = c2; Mode = mode; Color = c1;
        }
        public new void Dispose() { }
    }

    public sealed class PathGradientBrush : Brush, IDisposable
    {
        public PointF[] Points { get; }
        public Color CenterColor { get; set; }
        public Color[] SurroundColors { get; set; } = Array.Empty<Color>();
        public PathGradientBrush(PointF[] points) { Points = points; CenterColor = Color.White; }
        public new void Dispose() { }
    }

    public sealed class Matrix : IDisposable
    {
        public SKMatrix Value { get; private set; } = SKMatrix.Identity;
        public void RotateAt(float angleDegrees, PointF center)
        {
            var m = SKMatrix.CreateRotationDegrees(angleDegrees, center.X, center.Y);
            Value = SKMatrix.Concat(Value, m);
        }
        public void Translate(float x, float y)
        {
            var m = SKMatrix.CreateTranslation(x, y);
            Value = SKMatrix.Concat(Value, m);
        }
        public void Dispose() { }
    }

    public sealed class GraphicsState { }

    public sealed class Image : IDisposable
    {
        public SKBitmap Bitmap { get; }
        private Image(SKBitmap bmp) { Bitmap = bmp; }
        public static Image FromStream(Stream stream)
        {
            var bmp = SKBitmap.Decode(stream);
            return new Image(bmp);
        }
        public void Dispose() { Bitmap?.Dispose(); }
    }

    public sealed class Graphics : IDisposable
    {
        private readonly SKCanvas _canvas;
        public Graphics(SKCanvas canvas) { _canvas = canvas; }

        public SmoothingMode SmoothingMode { get; set; }
        public TextRenderingHint TextRenderingHint { get; set; }

        private Matrix _transform = new();
        public Matrix Transform
        {
            get => _transform;
            set { _transform = value; _canvas.SetMatrix(value.Value); }
        }

        public void Clear(Color color) => _canvas.Clear(color);

        public GraphicsState Save()
        {
            _canvas.Save();
            return new GraphicsState();
        }

        public void Restore(GraphicsState state) => _canvas.Restore();

        public void ResetTransform() { _transform = new Matrix(); _canvas.ResetMatrix(); }

        public void TranslateTransform(float x, float y)
        {
            _canvas.Translate(x, y);
        }

        public void RotateTransform(float angleDegrees)
        {
            _canvas.RotateDegrees(angleDegrees);
        }

        public void FillRectangle(Brush brush, float x, float y, float w, float h)
        {
            using var paint = CreatePaint(brush);
            _canvas.DrawRect(x, y, w, h, paint);
        }

        public void FillRectangle(Brush brush, Rectangle rect) =>
            FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);

        public void DrawRectangle(Pen pen, float x, float y, float w, float h)
        {
            using var paint = CreatePaint(pen);
            _canvas.DrawRect(x, y, w, h, paint);
        }

        public void DrawRectangle(Pen pen, Rectangle rect) =>
            DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);

        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            using var paint = CreatePaint(pen);
            _canvas.DrawLine(x1, y1, x2, y2, paint);
        }

        public void DrawLine(Pen pen, PointF a, PointF b) => DrawLine(pen, a.X, a.Y, b.X, b.Y);

        public void DrawLines(Pen pen, PointF[] points)
        {
            if (points.Length < 2) return;
            using var paint = CreatePaint(pen);
            for (var i = 1; i < points.Length; i++)
            {
                _canvas.DrawLine(points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y, paint);
            }
        }

        public void FillEllipse(Brush brush, float x, float y, float w, float h)
        {
            using var paint = CreatePaint(brush);
            _canvas.DrawOval(x + w / 2f, y + h / 2f, w / 2f, h / 2f, paint);
        }

        public void DrawString(string text, Font font, Brush brush, float x, float y)
        {
            using var paint = CreatePaint(brush);
            paint.TextSize = font.Size;
            paint.IsAntialias = true;
            _canvas.DrawText(text, x, y + font.Size, paint);
        }

        public void DrawImage(Image image, Rectangle rect)
        {
            if (image?.Bitmap is null) return;
            var src = new SKRect(0, 0, image.Bitmap.Width, image.Bitmap.Height);
            var dst = new SKRect(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
            _canvas.DrawBitmap(image.Bitmap, src, dst);
        }

        private static SKPaint CreatePaint(Brush brush)
        {
            if (brush is SolidBrush sb)
            {
                return new SKPaint { Color = sb.Color, Style = SKPaintStyle.Fill, IsAntialias = true };
            }
            if (brush is LinearGradientBrush lgb)
            {
                var shader = SKShader.CreateLinearGradient(
                    new SKPoint(lgb.Rect.X, lgb.Rect.Y),
                    new SKPoint(lgb.Rect.X, lgb.Rect.Y + lgb.Rect.Height),
                    new[] { (SKColor)lgb.StartColor, (SKColor)lgb.EndColor },
                    SKShaderTileMode.Clamp);
                return new SKPaint { Shader = shader, Style = SKPaintStyle.Fill, IsAntialias = true };
            }
            if (brush is PathGradientBrush pgb)
            {
                var shader = SKShader.CreateRadialGradient(
                    new SKPoint(pgb.Points.Length > 0 ? (pgb.Points[0].X + pgb.Points[2].X) / 2f : 0,
                                pgb.Points.Length > 0 ? (pgb.Points[0].Y + pgb.Points[2].Y) / 2f : 0),
                    100f,
                    new[] { (SKColor)pgb.CenterColor, (SKColor)(pgb.SurroundColors.Length > 0 ? pgb.SurroundColors[0] : Color.Black) },
                    SKShaderTileMode.Clamp);
                return new SKPaint { Shader = shader, Style = SKPaintStyle.Fill, IsAntialias = true };
            }
            return new SKPaint { Color = brush.Color, Style = SKPaintStyle.Fill, IsAntialias = true };
        }

        private static SKPaint CreatePaint(Pen pen)
        {
            var paint = new SKPaint
            {
                Color = pen.Color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = pen.Width,
                IsAntialias = true,
            };
            if (pen.StartCap == LineCap.Round || pen.EndCap == LineCap.Round)
            {
                paint.StrokeCap = SKStrokeCap.Round;
            }
            return paint;
        }

        public void Dispose() { }
    }
}
#endif
