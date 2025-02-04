﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using SixLabors.ImageSharp;

using static coil.Navigation;
using static coil.Util;

namespace coil
{
    public static class Coilutil
    {
        private static Dictionary<string, Image> _Images = ImageUtil.GetImages();

        //for cmdline
        public static void Show(BaseLevel l)
        {
            WL("SHOW:");
            var om = GetOutputMap(l);
            for (var y = 0; y < om.Count; y++)
            {
                for (var x = 0; x < om[0].Count; x++)
                {
                    W(om[y][x]);
                }

                W("\n");
            }
        }

        public static List<List<string>> GetBaseMapForEmpty(BaseLevel level)
        {
            var baseMap = GetOutputMap(level);

            //remove h, s, e
            for (var y = 0; y < baseMap.Count; y++)
            {
                for (var x = 0; x < baseMap[0].Count; x++)
                {
                    if (baseMap[y][x] == "h")
                    {
                        baseMap[y][x] = "x";
                    }

                    if (baseMap[y][x] == "s")
                    {
                        baseMap[y][x] = ".";
                    }

                    if (baseMap[y][x] == "e")
                    {
                        baseMap[y][x] = ".";
                    }
                }
            }
            return baseMap;
        }

        public static void SaveEmpty(BaseLevel level, string fn, string subtitle = "", bool quiet = false, List<PointText> pointTexts = null, bool arrows = false, bool corner = false)
        {
            if (fn.Contains(":"))
            {
                throw new Exception(fn);
            }
            var baseMap = GetBaseMapForEmpty(level);

            ImageUtil.Save(_Images, level, baseMap, fn, subtitle, quiet, pointTexts, arrows: arrows, corner: corner);
        }

        public static void SaveWithPath(BaseLevel level, string fn, string subtitle = "", bool quiet = false, List<(int,int)> highlights = null)
        {
            var baseMap = GetBaseMapForPath(level);
            ImageUtil.Save(_Images, level, baseMap, fn, subtitle, quiet, highlights:highlights);
        }

        public static void SaveAverageOnPath(BaseLevel level, int step, string fn,
            List<List<string>> baseMap, List<PointText> pointTexts,
            string subtitle = "", bool quiet = false
            )
        {
            ImageUtil.Save(_Images, level, baseMap, fn, subtitle, quiet, pointTexts);
        }

        /// <summary>
        /// Savewithpath / empty but with big overlay arrows showing general regional progression
        /// </summary>
        public static void SaveAverageOnPathWithArrows(BaseLevel level, string fn,
            List<List<string>> baseMap, List<PointText> pointTexts,
            string subtitle = "", bool quiet = false
            )
        {
            ImageUtil.Save(_Images, level, baseMap, fn, subtitle, quiet, pointTexts, arrows: true, overrideScale: 1);
        }

        public static void SaveAverageOnEmptyWithArrows(BaseLevel level, int step, string fn,
            List<List<string>> baseMap, List<PointText> pointTexts,
            string subtitle = "", bool quiet = false
            )
        {
            ImageUtil.Save(_Images, level, baseMap, fn, subtitle, quiet, pointTexts, arrows: true);
        }

        internal static void SaveArrowVersions(Level l, int ii, string stem, int arrowStepMin)
        {
            var step = l.TotalLength();
            var lc = l.LevelConfiguration;
            var ct = 0;
            while (step > arrowStepMin)
            {
                ct++;
                if (ct > 9)
                {
                    break;
                }
                step = step / 2;
                //var tpfn = $"{stem}/ap-{ii}-{step}-{lc.GetStr()}.png";
                //SaveAverageOnPath(l, step, tpfn, quiet:false);
                

                var pointTexts = GetAveragePoints(l, step);
                
                var arrowfn = $"{stem}/{lc.GetStr()}-arrow-{ii}-{step}.png";
                SaveAverageOnPathWithArrows(level: l, fn: arrowfn, baseMap: null, pointTexts: pointTexts, quiet: false);
                //var tefn = $"{stem}/ae-{ii}-{step}-{lc.GetStr()}.png";
                //SaveAverageOnEmpty(l, step, tefn, quiet: false);
                //var arrowefn = $"{stem}/{lc.GetStr()}-arrow-empty-{ii}-{step}.png";
                //SaveAverageOnEmptyWithArrows(l, step, arrowefn, baseEmptyMap, pointTexts, quiet: false);
            }
        }

        public static void SaveLevelAsText(Level l, int seed)
        {
            if (!System.IO.Directory.Exists("../../../levels"))
            {
                System.IO.Directory.CreateDirectory("../../../levels");
            }
            var textfn = $"../../../levels/{l.Width - 2}x{l.Height - 2} seed={seed} lc={l.LevelConfiguration.GetStr()}.coil";
            using (StreamWriter oo = File.AppendText(textfn))
            {
                var line1 = $"{l.Width - 2}x{l.Height - 2} - {l.LevelConfiguration.GetStr()}";
                oo.WriteLine(line1);
                for (var yy = 1; yy <= l.Height - 2; yy++)
                {
                    var row = new StringBuilder();
                    for (var xx = 1; xx <= l.Width - 2; xx++)
                    {
                        if (l.GetRowValue((xx, yy)) == null)
                        {
                            row.Append("X");
                        }
                        else
                        {
                            row.Append(".");
                        }
                    }
                    oo.WriteLine(row);
                }
            }
        }

        public static void SaveAverageOnEmpty(BaseLevel level, int step, string fn,
            List<List<string>> baseMap, List<PointText> pointTexts,
            string subtitle = "", bool quiet = false
            )
        {
            ImageUtil.Save(_Images, level, baseMap, fn, subtitle, quiet, pointTexts);
        }

        //return the average of the last step points, every step points.
        //ideally including start and end as the first and last steps.
        public static List<PointText> GetAveragePoints(BaseLevel level, int step)
        {
            var res = new List<PointText>();
            var allPoints = level.Iterate();
            var skip = 0;
            var lastpt = 0;
            res.Add(new PointText(0.ToString(), level.Segs.First.Value.Start));
            while (skip < allPoints.Count)
            {
                var elements = allPoints.Skip(skip).Take(step);
                var avgx = elements.Select(el => el.Item1).Sum() / elements.Count();
                var avgy = elements.Select(el => el.Item2).Sum() / elements.Count();

                lastpt = skip / step + 1;
                skip += step;
                res.Add(new PointText(lastpt.ToString(), (avgx, avgy)));
            }

            res.Add(new PointText(lastpt.ToString(), level.Segs.Last.Value.GetEnd()));
            return res;
        }

        /// <summary>
        /// Generalize the path by taking its point after N steps.
        /// </summary>
        public static Dictionary<(int, int), string> GetRawTracePoints(BaseLevel level, int step)
        {
            var sqct = 0;
            var target = step;
            var points = new List<(int, int)>();
            foreach (var seg in level.Segs)
            {
                var needed = (target - sqct % target);
                if (seg.Len >= needed)
                {
                    var nextPoint = Add(seg.Start, seg.Dir, needed);
                    target = target + step;
                    points.Add(nextPoint);
                }
                sqct += seg.Len;
            }
            var ii = 0;
            var pointTexts = new Dictionary<(int, int), string>();
            foreach (var point in points)
            {
                pointTexts[(point.Item1, point.Item2)] = ii.ToString();
                ii++;
            }
            return pointTexts;
        }

        public static List<List<string>> GetBaseMapForPath(BaseLevel level)
        {
            var baseMap = GetOutputMap(level);
            var path = GetInOutStrings(level);
            var decisions = GetDecisions(level);
            var easyDecisions = decisions.Item1;
            var hardDecisions = decisions.Item2;
            var ins = path.Item1;
            var outs = path.Item2;
            //TODO add in decision markers.
            //throw new Exception();
            for (var y = 0; y < baseMap.Count; y++)
            {
                for (var x = 0; x < baseMap[0].Count; x++)
                {
                    if (ins[y][x] != "")
                    {

                        baseMap[y][x] = ins[y][x] + outs[y][x];
                        if (hardDecisions.Contains((x, y)))
                        {
                            if (baseMap[y][x].Length != 2)
                            {
                                WL("Bad basemap len");
                            }
                            baseMap[y][x] += "-hard";
                        }
                        if (easyDecisions.Contains((x, y)))
                        {
                            if (baseMap[y][x].Length != 2)
                            {
                                WL("Bad basemap len");
                            }
                            baseMap[y][x] += "-easy";
                        }
                        //TODO adjust this for easy decisions
                    }
                }
            }
            return baseMap;
        }




        private static string GetDString(Dir dir)
        {
            switch (dir)
            {
                case Dir.Up:
                    return "u";

                case Dir.Right:
                    return "r";

                case Dir.Down:
                    return "d";

                case Dir.Left:
                    return "l";

                default:
                    throw new Exception("Bad dstring");
            }
        }

        //for image creation - just get the path parts - rest ""
        public static Tuple<List<List<string>>, List<List<string>>> GetInOutStrings(BaseLevel l)
        {
            var ins = new List<List<string>>();
            var outs = new List<List<string>>();
            for (var yy = 0; yy < l.Height; yy++)
            {
                ins.Add(new List<string>());
                outs.Add(new List<string>());
                for (var xx = 0; xx < l.Width; xx++)
                {
                    ins[yy].Add("");
                    outs[yy].Add("");
                }
            }

            foreach (var seg in l.Segs)
            {
                var len = 0;
                var target = seg.Start;
                var dirstring = GetDString(seg.Dir);
                while (len < seg.Len)
                {
                    //you go out to target
                    outs[target.Item2][target.Item1] = dirstring;
                    target = Add(target, seg.Dir);
                    ins[target.Item2][target.Item1] = dirstring;
                    len++;
                }
            }

            var start = l.Segs.First.Value.Start;
            ins[start.Item2][start.Item1] = "s";

            var end = GetEnd(l.Segs.Last.Value);
            outs[end.Item2][end.Item1] = "e";

            return new Tuple<List<List<string>>, List<List<string>>>(ins, outs);
        }

        public static void ShowHit(BaseLevel l)
        {
            WL("HITS:");
            List<Seg> h = null;
            List<Seg> t = null;
            for (var yy = 0; yy < l.Height; yy++)
            {
                for (var xx = 0; xx < l.Width; xx++)
                {
                    if (l.Hits.Contains((xx, yy)))
                    {
                        var hits = l.Hits.Get((xx, yy));
                        if (hits.Count == 0)
                        {
                            W(".");
                        }
                        else if (hits.Count == 1)
                        {
                            Console.Write(hits[0].Index % 10);
                        }
                        else if (hits.Count == 2)
                        {
                            W("T");
                            t = hits;
                        }
                        else if (hits.Count == 3)
                        {
                            W("H");
                            h = hits;
                        }
                    }
                    else
                    {
                        W(".");
                    }
                }

                W("\n");
            }

            if (h != null)
            {
                WL("H:");
                foreach (var el in h)
                {
                    WL(el.ToString());
                }
            }

            if (t != null)
            {
                WL("t:");
                foreach (var el in t)
                {
                    WL(el.ToString());
                }
            }
        }

        //TODO this needs fixing to show the actual index not the space-filling index.
        public static void ShowSeg(Level l)
        {
            WL("SEG");
            var segindex = 1;
            var indexes = new Dictionary<uint, int>();
            foreach (var seg in l.Segs)
            {
                indexes[seg.Index] = segindex;
                segindex++;
            }


            for (var yy = 0; yy < l.Height; yy++)
            {
                for (var xx = 0; xx < l.Width; xx++)
                {
                    var rv = l.GetRowValue((xx, yy));
                    if (rv != null)
                    {
                        Console.Write(indexes[rv.Index] % 10);
                    }
                    else
                    {
                        Console.Write(".");
                    }
                    segindex++;
                }

                Console.Write("\n");
            }
        }

        public static List<List<string>> GetOutputMap(BaseLevel l)
        {
            var hasStart = false;
            (int, int) start = (0, 0);
            (int, int) end = (0, 0);
            if (l.Segs.Count > 0)
            {
                start = l.Segs.First.Value.Start;
                end = GetEnd(l.Segs.Last.Value);
                hasStart = true;
            }

            var outputMap = new List<List<string>>();

            for (var yy = 0; yy < l.Height; yy++)
            {
                var row = new List<string>();
                for (var xx = 0; xx < l.Width; xx++)
                {
                    var point = (xx, yy);
                    bool edge = xx == 0 || xx == l.Width - 1 || yy == 0 || yy == l.Height - 1;

                    if (hasStart)
                    {
                        if (yy == start.Item2 && xx == start.Item1)
                        {
                            row.Add("s");
                            continue;
                        }

                        if (yy == end.Item2 && xx == end.Item1)
                        {
                            row.Add("e");
                            continue;
                        }
                    }

                    var hit = l.Hits.Get(point);
                    var val = l.GetRowValue(point);
                    if (val != null)
                    {
                        row.Add(".");
                    }
                    else if (hit!.Count > 0)
                    {
                        row.Add("h");
                    }
                    else if (edge)
                    {
                        row.Add("b");
                    }

                    else if (val == null)
                    {
                        row.Add("x");
                    }
                }

                outputMap.Add(row);
            }

            return outputMap;
        }


        public static (HashSet<(int, int)>, HashSet<(int, int)>) GetDecisions(BaseLevel level)
        {
            //one side is an obvious dead end.
            var easyDecisions = new List<(int, int)>();

            //those where one side isn't an obvious dead end.
            var hardDecisions = new List<(int, int)>();
            var levelend = level.Segs.Last.Value.GetEnd();
            foreach (var seg in level.Segs)
            {
                var end = seg.GetEnd();
                var right = Add(end, Rot(seg.Dir));
                var left = Add(end, ARot(seg.Dir));

                //decision if left and right are empty, and the seg that fills them has index greater than current.
                //var rval = level.GetRowValue(right);
                //var lval = level.GetRowValue(left);
                //if (rval != null && lval != null
                //    && rval.Index > seg.Index && lval.Index > seg.Index)
                //{
                //    //we know both sides are open when you reach this point.

                //    hardDecisions.Add(end);
                //}

                //skip end points since they ARE allowed to be double-hard decisions (left and right both touch one empty square)
                if (right==levelend || left == levelend)
                {
                    continue;
                }

                if (PointIsOpenAfterSeg(right, seg, level) && PointIsOpenAfterSeg(left, seg, level))
                {
                    //find three further neighbors.
                    var rightneighbors = new List<(int, int)>() { Add(right, seg.Dir), Add(right, Rot(seg.Dir)), Add(right, Rot(Rot(seg.Dir)))};
                    var leftneighbors = new List<(int, int)>() { Add(left, seg.Dir), Add(left, ARot(seg.Dir)), Add(left, ARot(ARot(seg.Dir)))};
                    var rightopenneighbors = rightneighbors.Where(rn => PointIsOpenAfterSeg(rn, seg, level));
                    var leftopenneighbors = leftneighbors.Where(ln => PointIsOpenAfterSeg(ln, seg, level));
                    //Show(level);
                    //ShowHit(level);
                    
                    var easy = false;
                    if (rightopenneighbors.Count() == 1 || leftopenneighbors.Count() == 1)
                    {
                        easy = true;
                    }
                    if (rightopenneighbors.Count() == 1 && leftopenneighbors.Count() == 1)
                    {
                        throw new Exception("EX");
                    }
                    if (easy)
                    {
                        easyDecisions.Add(end);
                    }
                    else
                    {
                        hardDecisions.Add(end);
                    }
                }

            }
            return (new HashSet<(int, int)>(easyDecisions), new HashSet<(int, int)>(hardDecisions));
        }

        public static bool PointIsOpenAfterSeg((int,int) pt, Seg seg, BaseLevel level)
        {
            var val = level.GetRowValue(pt);
            if (val!=null && val.Index > seg.Index)
            {
                return true;
            }
            return false;
        }
    }
}
