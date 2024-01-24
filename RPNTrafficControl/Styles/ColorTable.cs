using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RPNTrafficControl.Styles
{
    internal class ColorTable : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin
        {
            get { return Color.FromArgb(255, 43, 43, 43); }
        }
        public override Color ToolStripGradientEnd
        {
            get { return Color.FromArgb(255, 43, 43, 43); }
        }
        public override Color MenuItemBorder
        {
            get { return Color.FromArgb(255, 43, 43, 43); }
        }
        public override Color MenuItemSelected
        {
            get { return Color.WhiteSmoke; }
        }
        public override Color ToolStripDropDownBackground
        {
            get { return Color.FromArgb(255, 43, 43, 43); }
        }
        public override Color ImageMarginGradientBegin
        {
            get { return Color.FromArgb(255, 43, 43, 43); }
        }
        public override Color ImageMarginGradientMiddle
        {
            get { return Color.FromArgb(255, 43, 43, 43); }
        }
        public override Color ImageMarginGradientEnd
        {
            get { return Color.FromArgb(255, 43, 43, 43); }
        }
    }
}
