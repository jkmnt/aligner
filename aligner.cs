using System;
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
        ORIGIN,
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
        private class Primitive : List<Entity>
        {
            public readonly string Name;

            private Point2F[] _obj_bb(Entity obj)
            {
                Point3F min = Point3F.Undefined;
                Point3F max = Point3F.Undefined;
                obj.GetExtrema(ref min, ref max);
                return new Point2F[] { (Point2F)min, (Point2F)max };                
            }

            public Point2F[] Get_bbox()
            {
                Point2F[] bb = { Point2F.Undefined, Point2F.Undefined };

                foreach (Entity e in this)
                {                    
                    Point2F[] ebb = _obj_bb(e);
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

            public Primitive(string name)
            {
                Name = name;
            }
        }

        private class Primitives_collection : List<Primitive>
        {
            public Point2F[] Get_bbox()
            {
                Point2F[] bb = { Point2F.Undefined, Point2F.Undefined };

                foreach (Primitive e in this)
                {
                    Point2F[] ebb = e.Get_bbox();
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
        }

        static CamBamUI ui
        {
            get { return CamBamUI.MainUI; }
        }

        static void align_primitive(Primitive primitive, Point2F[] anchor, Align_mode mode)
        {
            double dx = 0;
            double dy = 0;

            Point2F[] ebb = primitive.Get_bbox();

            Align_mode hor = mode & Align_mode.HMASK;
            Align_mode vert = mode & Align_mode.VMASK;

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

            //
            if (Math.Abs(dx) < 1E-8) dx = 0;
            if (Math.Abs(dy) < 1E-8) dy = 0;

            foreach (Entity e in primitive)                                            
                e.ApplyTransformation(Matrix4x4F.Translation(dx, dy, 0));            
        }

        static public void align(Align_mode align_mode, Anchor_mode anchor_mode)
        {
            Primitives_collection primitives = new Primitives_collection();

            foreach (object obj in ui.ActiveView.SelectedEntities)
            {
                if (obj is Entity)
                {
                    Entity e = (Entity)obj;
                    Primitive pr = null;
                    string tag = null;

                    if (e.Tag != null && e.Tag.StartsWith("Group"))
                    {
                        tag = e.Tag;
                        pr = primitives.Find(x => x.Name == e.Tag);
                    }

                    if (pr == null)
                        pr = new Primitive(tag);

                    pr.Add(e);
                    primitives.Add(pr);
                }
            }

            if (primitives.Count < 1) return;

            Point2F[] anchor;

            if (anchor_mode == Anchor_mode.ALL_SELECTED)
            {
                anchor = primitives.Get_bbox();
            }
            else if (anchor_mode == Anchor_mode.FIRST_SELECTED)
            {
                if (primitives.Count < 2) return;

                anchor = primitives[0].Get_bbox();
                primitives.RemoveAt(0);
            }
            else if (anchor_mode == Anchor_mode.LAST_SELECTED)
            {
                if (primitives.Count < 2) return;

                int last = primitives.Count - 1;
                anchor = primitives[last].Get_bbox();
                primitives.RemoveAt(last);
            }
            else if (anchor_mode == Anchor_mode.DRAWING)
            {
                Primitive all = new Primitive(null);                
                foreach (Layer layer in ui.ActiveView.CADFile.Layers)
                {
                    foreach (Entity e in layer.Entities)
                        all.Add(e);
                }
                anchor = all.Get_bbox();
            }
            else if (anchor_mode == Anchor_mode.ORIGIN)
            {
                anchor = new Point2F[] { new Point2F(0, 0), new Point2F(0, 0) };
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

            foreach (Primitive primitive in primitives)
            {
                foreach (Entity e in primitive)
                    ui.UndoBuffer.Add(e);

                align_primitive(primitive, anchor, align_mode);
            }

            ui.ActiveView.Selection.RefreshExtrema();
            ui.ActiveView.ResumeRefresh();
            ui.ObjectProperties.Refresh();
            ui.ActiveView.UpdateViewport();
        }
    }
}
