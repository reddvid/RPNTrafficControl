using RPNTrafficControl.Styles;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RPNTrafficControl.Renderers
{
    public class ContextMenuRenderer : ToolStripProfessionalRenderer
    {
        public ContextMenuRenderer() : base(new ColorTable()) { }

        public Color ImageColor { get; set; }
        public Color HighlightColor { get; set; }
        public int VerticalPadding { get; set; }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e is null) return;
            e.TextFormat &= ~TextFormatFlags.HidePrefix;
            e.TextFormat |= TextFormatFlags.VerticalCenter;

            var rectangle = e.TextRectangle;
            rectangle.Offset(24, VerticalPadding);
            e.TextRectangle = rectangle;

            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs menu)
        {
            if (!menu.Item.Selected)
            {
                base.OnRenderMenuItemBackground(menu);
            }
            else
            {
                if (menu.Item.Enabled)
                {
                    Rectangle menuRectangle = new Rectangle(Point.Empty, menu.Item.Size);
                    menu.Graphics.FillRectangle(new SolidBrush(RenderHighlight(HighlightColor)), menuRectangle);
                }
                else
                {
                    Rectangle menuRectangle = new Rectangle(Point.Empty, menu.Item.Size);
                    menu.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(20, 128, 128, 128)), menuRectangle);
                }

            }
        }

        private Color RenderHighlight(Color color)
        {
            int r;
            int g;
            int b;

            if (color.R == 0 && color.G == 0 && color.B == 0)
            {
                r = color.R + 65;
                g = color.G + 65;
                b = color.B + 65;
            }
            else
            {
                r = color.R;
                g = color.G;
                b = color.B;
            }

            return Color.FromArgb(r, g, b);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rectangle = new Rectangle(e.ImageRectangle.Location, e.ImageRectangle.Size);
            rectangle.Inflate(1, 1);
            e.Graphics.FillRectangle(new SolidBrush(ImageColor), rectangle);
            e.Graphics.DrawLines(Pens.Gray, new Point[]
            {
                new(rectangle.Left + 4, 10), //2
                new(rectangle.Left - 2 + rectangle.Width / 2,  rectangle.Height / 2 + 4), //3
                new(rectangle.Right - 4, rectangle.Top + 4)
            });
        }
    }
}
