using System;
using System.Windows.Forms;

using CamBam;
using CamBam.UI;

namespace Aligner
{
    public class Host
    {
        static public void log(string s, params object[] args)
        {
            ThisApplication.AddLogMessage(s, args);
        }
        static public void warn(string s, params object[] args)
        {
            ThisApplication.AddLogMessage("Warning: " + s, args);
        }
        static public void err(string s, params object[] args)
        {
            ThisApplication.AddLogMessage("Error: " + s, args);
        }
        static public void msg(string s, params object[] args)
        {
            ThisApplication.MsgBox(String.Format(s, args));
        }
        static public void sleep(int ms)
        {
            System.Threading.Thread.Sleep(ms);
            System.Windows.Forms.Application.DoEvents();
        }
    }

    public class Aligner_plugin
    {
        static ToolStripComboBox anchor_selector;
        static bool show_extra_aligns = false;

        static void click_handler(object sender, EventArgs e)
        {
            ToolStripItem ts = (ToolStripItem)sender;
            Aligner.align((Align_mode)ts.Tag, (Anchor_mode)anchor_selector.SelectedIndex);
        }

        static void on_load(object sender, EventArgs e)
        {                    
            ToolStrip ts = new ToolStrip();
            ToolStripItem item;

            if (show_extra_aligns)
            {
                item = ts.Items.Add("R2L");
                item.Tag = Align_mode.RIGHTS_TO_LEFT;
                item.Click += click_handler;
            }

            item = ts.Items.Add("L");
            item.Tag = Align_mode.LEFTS;
            item.Click += click_handler;

            item = ts.Items.Add("C");
            item.Tag = Align_mode.HCENTER;
            item.Click += click_handler;

            item = ts.Items.Add("R");
            item.Tag = Align_mode.RIGHTS;
            item.Click += click_handler;

            if (show_extra_aligns)
            {
                item = ts.Items.Add("L2R");
                item.Tag = Align_mode.LEFTS_TO_RIGHT;
                item.Click += click_handler;
            }

            if (show_extra_aligns)
            {
                item = ts.Items.Add("B2T");
                item.Tag = Align_mode.BOTS_TO_TOP;
                item.Click += click_handler;
            }

            item = ts.Items.Add("T");
            item.Tag = Align_mode.TOPS;
            item.Click += click_handler;

            item = ts.Items.Add("M");
            item.Tag = Align_mode.VMIDDLE;
            item.Click += click_handler;

            item = ts.Items.Add("B");
            item.Tag = Align_mode.BOTS;
            item.Click += click_handler;

            if (show_extra_aligns)
            {
                item = ts.Items.Add("T2B");
                item.Tag = Align_mode.TOPS_TO_BOT;
                item.Click += click_handler;
            }

            anchor_selector = new ToolStripComboBox();
            anchor_selector.Items.Add("To Last Sel");
            anchor_selector.Items.Add("To First Sel");
            anchor_selector.Items.Add("To Selection");
            anchor_selector.Items.Add("To Drawing");
            anchor_selector.Items.Add("To Stock");

            anchor_selector.AutoSize = false;
            anchor_selector.Width = 90;
            anchor_selector.DropDownStyle = ComboBoxStyle.DropDownList;
            anchor_selector.SelectedIndex = 0;

            ts.Items.Add(anchor_selector);

            // find app toolstrip control
            ToolStripPanel tsp = null;
            foreach (Control c in ThisApplication.TopWindow.Controls)
            {
                if (!(c is ToolStripContainer))
                    continue;
                tsp = ((ToolStripContainer)c).TopToolStripPanel;
                break;
            }

            // since controls layed in the reverse order, attach new toolstrip to the right edgde of the rightmost existing toolstrip.
            // extra dirty, but ... ok
            tsp.Join(ts, tsp.Controls[0].Right, 0);
        }

        public static void InitPlugin(CamBamUI ui)
        {
            ThisApplication.TopWindow.Load += on_load;
        }
    }
}
