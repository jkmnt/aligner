using System.Collections.Generic;

using CamBam.UI;
using CamBam.CAD;
using CamBam.CAM;
using CamBam.Geom;

namespace Aligner
{
    enum Anchor_mode
    {
        LAST_SELECTED,
        FIRST_SELECTED,
        ALL_SELECTED,
        DRAWING,
        STOCK,
    }

    enum Align_mode
    {
        NONE = 0,
        RIGHTS_TO_LEFT = 0x01,
        LEFTS = 0x02,
        HCENTER = 0x03,
        RIGHTS = 0x04,
        LEFTS_TO_RIGHT = 0x05,

        TOPS_TO_BOT = 0x10,
        BOTS = 0x20,
        VMIDDLE = 0x30,
        TOPS = 0x40,
        BOTS_TO_TOP = 0x50,

        HMASK = 0x0F,
        VMASK = 0xF0,
    }

    class Aligner
    {
        static CamBamUI ui
        {
            get { return CamBamUI.MainUI; }
        }

        static Point2F[] get_bb(Entity obj)
        {
            Point3F min = Point3F.Undefined;
            Point3F max = Point3F.Undefined;
            obj.GetExtrema(ref min, ref max);
            return new Point2F[] { new Point2F(min.X, min.Y), new Point2F(max.X, max.Y)};
        }

        static Point2F[] get_bb(Entity[] objs)
        {
            Point2F[] bb = {Point2F.Undefined, Point2F.Undefined};

            foreach (Entity e in objs)
            {
                Point2F[] ebb = get_bb(e);
                if (bb[0].IsUndefined)
                {
                    bb = ebb;
                }
                else
                {
                    if (ebb[0].X < bb[0].X) bb[0].X = ebb[0].X;
                    if (ebb[0].Y < bb[0].Y) bb[0].Y = ebb[0].Y;
                    if (ebb[1].X > bb[1].X) bb[1].X = ebb[1].X;
                    if (ebb[1].Y > bb[1].Y) bb[1].Y = ebb[1].Y;
                }
            }
            return bb;
        }

        static void align_object(Entity obj, Point2F[] anchor, Align_mode mode)
        {
            double dx = 0;
            double dy = 0;

            Point2F[] ebb = get_bb(obj);

            Align_mode hor = mode & Align_mode.HMASK;
            Align_mode vert = mode & Align_mode.VMASK;

//          Host.log("vert = {0}, hor = {1}", vert, hor);

            if (hor == Align_mode.RIGHTS_TO_LEFT)
                dx = anchor[0].X - ebb[1].X;
            else if (hor == Align_mode.LEFTS)
                dx = anchor[0].X - ebb[0].X;
            else if (hor == Align_mode.HCENTER)
                dx = (anchor[0].X + anchor[1].X - ebb[0].X - ebb[1].X) / 2;
            else if (hor == Align_mode.RIGHTS)
                dx = anchor[1].X - ebb[1].X;
            else if (hor == Align_mode.LEFTS_TO_RIGHT)
                dx = anchor[1].X - ebb[0].X;

            if (vert == Align_mode.TOPS_TO_BOT)
                dy = anchor[0].Y - ebb[1].Y;
            else if (vert == Align_mode.BOTS)
                dy = anchor[0].Y - ebb[0].Y;
            else if (vert == Align_mode.VMIDDLE)
                dy = (anchor[0].Y + anchor[1].Y - ebb[0].Y - ebb[1].Y) / 2;
            else if (vert == Align_mode.TOPS)
                dy = anchor[1].Y - ebb[1].Y;
            else if (vert == Align_mode.BOTS_TO_TOP)
                dy = anchor[1].Y - ebb[0].Y;

//          Host.log("dx = {0}, dy = {1}", dx, dy);

            obj.ApplyTransformation(Matrix4x4F.Translation(dx, dy, 0));
        }

        static public void align(Align_mode align_mode, Anchor_mode anchor_mode)
        {
//          Host.log("anchor = {0}", anchor_mode);

            List<Entity> to_align = new List<Entity>();

            foreach (object e in ui.ActiveView.SelectedEntities)
            {
                if (e is Entity)
                    to_align.Add((Entity)e);
            }

            if (to_align.Count < 1) return;

            Point2F[] anchor;

            if (anchor_mode == Anchor_mode.ALL_SELECTED)
            {
                anchor = get_bb(to_align.ToArray());
            }
            else if (anchor_mode == Anchor_mode.FIRST_SELECTED)
            {
                if (to_align.Count < 2) return;

                anchor = get_bb(to_align[0]);
                to_align.RemoveAt(0);
            }
            else if (anchor_mode == Anchor_mode.LAST_SELECTED)
            {
                if (to_align.Count < 2) return;

                int last = to_align.Count - 1;
                anchor = get_bb(to_align[last]);
                to_align.RemoveAt(last);
            }
            else if (anchor_mode == Anchor_mode.DRAWING)
            {
                List<Entity> all = new List<Entity>();
                foreach (Layer layer in ui.ActiveView.CADFile.Layers)
                {
                    foreach (Entity e in layer.Entities)
                        all.Add(e);
                }
                anchor = get_bb(all.ToArray());
            }
            else if (anchor_mode == Anchor_mode.STOCK)
            {
                StockDef stock = ui.ActiveView.CADFile.MachiningOptions.Stock;
                if (stock.IsUndefined) return;
                anchor = new Point2F[] {new Point2F(stock.PMin.X, stock.PMin.Y), new Point2F(stock.PMax.X, stock.PMax.Y)};
            }
            else
            {
                // XXX: assert here
                return;
            }

            ui.ActiveView.SuspendRefresh();

            ui.ActiveView.CADFile.Modified = true;
            ui.UndoBuffer.AddUndoPoint("Aligner Plugin");

            foreach (Entity e in to_align)
            {
                ui.UndoBuffer.Add(e);
                align_object(e, anchor, align_mode);
            }

            ui.ActiveView.Selection.RefreshExtrema();
            ui.ActiveView.ResumeRefresh();
            ui.ObjectProperties.Refresh();
            ui.ActiveView.UpdateViewport();
        }
    }
}
