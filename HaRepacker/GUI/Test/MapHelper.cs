using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MapleLib.WzLib;
using MapleLib.WzLib.WzStructure;

namespace HaRepacker.GUI
{
    public static class MapHelper
    {
        static StreamWriter streamWriter;
        public static void Main(WzDirectory mapDirectory, WzDirectory mobDirectory)
        {
            string logFilePath = "C:\\Users\\admin\\Desktop\\log.txt";
            streamWriter = new StreamWriter(logFilePath, true, Encoding.UTF8);

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var dir = mapDirectory.GetChild($"Map\\Map{i}") as WzDirectory;
                    if (dir == null)
                    {
                        continue;
                    }
                    foreach (var mapImage in dir.WzImages)
                    {
                        if (mapImage != null)
                        {
                            streamWriter.WriteLine($"mapId={mapImage.Name}");
                            LoadFootholdGroup(mapImage, mobDirectory);
                            streamWriter.WriteLine();
                            Console.WriteLine($"mapId={mapImage.Name}输出完成");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }


                Console.WriteLine($"Map{i}全部输出完成");
            } 
        }

        public static void LoadFootholdGroup(WzImage mapImage, WzDirectory mobDir)
        {
            Dictionary<int, List<WzImageProperty>> groups = new Dictionary<int, List<WzImageProperty>>();
            Dictionary<int, List<int>> ranges = new Dictionary<int, List<int>>();

            WzImageProperty fhProperty = mapImage["foothold"];
            if (fhProperty == null)
            {
                return;
            }
            foreach (var layer in fhProperty.WzProperties)
            {
                foreach (var group in layer.WzProperties)
                {
                    SortFoothold(group, groups, ranges);
                }
            }

            Modify(mapImage, mobDir, groups, ranges);
        }

        public static void Modify(WzImage mapImage, WzDirectory mobDir,  Dictionary<int, List<WzImageProperty>> groups, Dictionary<int, List<int>> ranges)
        {
            WzImageProperty lifeProperty = mapImage["life"];
            if (lifeProperty != null && lifeProperty.WzProperties.Count > 0)
            {
                foreach (var life in lifeProperty.WzProperties)
                {
                    if (life["type"].GetString() != "m")
                    {
                        continue;
                    }

                    string id = life["id"].GetString();
                    if (IsNormalMob(mobDir, id))
                    {
                        int fh = life["fh"].GetInt();
                        int groupNumber = GetNumber(groups, fh);
                        if (groupNumber<0)
                        {
                            //Console.WriteLine($"mob={life.Name}, id={id} , groupNumber<0");
                            continue;
                        }
                        var range = ranges[groupNumber];

                        int x = life["x"].GetInt();
                        (int, int) rx = (0,0);
                        try
                        {
                            rx = GetBoundary(x, range);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine($"mob={life.Name}, id={id}, x={x}");
                        }

                        int rx0 = life["rx0"].GetInt();
                        int rx1 = life["rx1"].GetInt();

                        if (rx0 < rx.Item1)
                        {
                            life["rx0"].SetValue(rx.Item1);
                            life["rx0"].ParentImage.Changed = true;
                            //Console.WriteLine($"mob {life.Name}, 发生rx0修正： 原始rx0 : {rx0},  新rx0 : {rx.Item1}");
                            streamWriter.WriteLine($"mob {life.Name}, 发生rx0修正： 原始rx0 : {rx0},  新rx0 : {rx.Item1}");
                        }
                        if (rx1 > rx.Item2)
                        {
                            life["rx1"].SetValue(rx.Item2);
                            life["rx1"].ParentImage.Changed = true;
                            streamWriter.WriteLine($"mob {life.Name}, 发生rx1修正： 原始rx1 : {rx1},  新rx1 : {rx.Item2}");
                        }
                    }
                }
            }
        }

        private static bool IsNormalMob(WzDirectory mobDir, string id)
        {
            WzImage mobImage = mobDir.GetChild(id+".img") as WzImage;
            List<string> list = mobImage.WzProperties.Select(t => t.Name).ToList();

            if (list.Contains("jump") || list.Contains("fly") || !list.Contains("move"))
            {
                return false;
            }
            return true;
        }

        private static void SortFoothold(WzImageProperty group, Dictionary<int, List<WzImageProperty>> groups, Dictionary<int, List<int>> boundary)
        {
            var starts = group.WzProperties.Where(t => t["prev"].GetInt() == 0);
            foreach (var start in starts)
            {
                List<WzImageProperty> listForward = new List<WzImageProperty>();
                WzImageProperty node = start;
                WzImageProperty last = null;
                bool isTestSlope = true;
                while (node != null)
                {
                    if (isTestSlope)
                    {
                        if (IsSlopeLessThan45(node)) { listForward.Add(node); }
                        isTestSlope = false;
                    }
                    else
                    {
                        listForward.Add(node);
                    }
                    int next = node["next"].GetInt();
                    last = node;
                    node = group.WzProperties.FirstOrDefault(t => t.Name == next.ToString());
                }

                node = last;
                List<WzImageProperty> listRemoveFromBack = new List<WzImageProperty>();
                while (node != null)
                {
                    if (!IsSlopeLessThan45(node))
                    {
                        listRemoveFromBack.Add(node);
                    }
                    else
                    {
                        break;
                    }
                    int prev = node["prev"].GetInt();
                    node = group.WzProperties.FirstOrDefault(t => t.Name == prev.ToString());
                }

                foreach (var item in listRemoveFromBack)
                {
                    if (listForward.Contains(item))
                    {
                        listForward.Remove(item);
                    }
                }

                //foreach (var item in listForward)
                //{
                //    Console.Write(item.Name + "  ");
                //    Console.WriteLine();
                //}

                if (listForward.Count > 0)
                {
                    groups.Add(groups.Count, listForward);
                    boundary.Add(boundary.Count, CalcBoundary(listForward));
                }
            }
        }

        /// <summary>
        /// 计算foothold节点坡度是否小于45度
        /// </summary>
        /// <param name="foothold"></param>
        /// <returns></returns>
        public static bool IsSlopeLessThan45(WzImageProperty foothold)
        {
            int x1 = foothold["x1"].GetInt();
            int y1 = foothold["y1"].GetInt();
            int x2 = foothold["x2"].GetInt();
            int y2 = foothold["y2"].GetInt();
            float slope = (x2 - x1) == 0 ? 2 : (float)(y2 - y1) / (x2 - x1); //斜率绝对值
            return Math.Abs(slope) < 1;
        }


        public static List<int> CalcBoundary(List<WzImageProperty> list)
        {
            List<int> boundary = new List<int>();
            List<int> temp = new List<int>();
            foreach (var item in list)
            {
                int x1 = item["x1"].GetInt();
                int x2 = item["x2"].GetInt();
                
                if (!temp.Contains(x1))
                {
                    temp.Add(x1);
                }
                if (!temp.Contains(x2))
                {
                    temp.Add(x2);
                }

                if (x1 == x2 && !boundary.Contains(x1))
                {
                    boundary.Add(x1);
                }
            }

            int max = temp.Max();
            int min = temp.Min();

            if (!boundary.Contains(max))
                boundary.Add(max);
            if (!boundary.Contains(min))
                boundary.Add(min);

            boundary.Sort();

            //foreach (var item in boundary)
            //{
            //    Console.Write("边界：");
            //    Console.Write(item+"  ");
            //    Console.WriteLine();
            //}

            return boundary;
        }
        

        public static int GetNumber(this Dictionary<int, List<WzImageProperty>> list , int number)
        {
            foreach (var item in list)
            {
                foreach (var node in item.Value)
                {
                    if (node.Name == number.ToString())
                    {
                        return item.Key;
                    }
                }
            }
            return -1;
        }


        //找一个int类型数在List中恰好包含它的两个边界
        public static ValueTuple<int, int> GetBoundary(int value, List<int> boundary)
        {
            for (int i = 0; i < boundary.Count; i++)
            {
                //Console.WriteLine($"i={i}, value={value}, boundary[i]={boundary[i]}");
                if (value < boundary[i])
                {
                    if (i == 0)
                    {
                        throw new Exception();
                    }
                    return new ValueTuple<int, int>(boundary[i - 1], boundary[i]);
                }
            }
            return new ValueTuple<int, int>(-1, -1);
        }
    }
}
