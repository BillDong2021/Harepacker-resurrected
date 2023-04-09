using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapleLib.WzLib;

namespace HaRepacker.GUI
{
    public class Test
    {
        private static Dictionary<string, MobType> MobTypeById = new Dictionary<string, MobType>();
        private static Dictionary<int, List<int>> BoundaryByGroupNumber = new Dictionary<int, List<int>>();
        private static Dictionary<int, List<int>> FhByGroupNumber = new Dictionary<int, List<int>>();
        private static List<Foothold> footholds = new List<Foothold>();
        
        public WzFile wzFile;
        public List<WzFile> loadedWzFile;

        public Test(WzFile wzFile)
        {
            this.wzFile = wzFile;
        }

        public Test(List<WzFile> loadedWzFile)
        {
            this.loadedWzFile = loadedWzFile;
        }
        
        public void TestMethod()
        {
            WzDirectory wzDirectory = loadedWzFile[0].WzDirectory;
            WzObject wzObject = wzDirectory.GetChild("Map\\Map0\\000030000.img");
            WzImage mapImage = (WzImage)wzObject;

            WzDirectory mobDirectory = loadedWzFile[1].WzDirectory;
            MapHelper.Main(wzDirectory, mobDirectory);
            //MapHelper.LoadFootholdGroup(mapImage, mobDirectory);

            //WzImageProperty life = wzImage["life"];
            //WzImageProperty mob0 = life["0"];
            //WzImageProperty rx0 = mob0["rx0"];


            //LoadMapGroup(wzImage);
            //Console.WriteLine(rx0.WzValue);
            //rx0.SetValue(1000000);
            //Console.WriteLine(rx0.WzValue);
        }

        public static void LoadMapGroup(WzImage mapImage)
        {
            BoundaryByGroupNumber = new Dictionary<int, List<int>>();
            FhByGroupNumber = new Dictionary<int, List<int>>();
            footholds = new List<Foothold>();

            WzImageProperty fhProperty = mapImage["foothold"];
            foreach (WzImageProperty layerProperty in fhProperty.WzProperties)
            {
                foreach (var groupProperty in layerProperty.WzProperties)
                {
                    int group = int.Parse(groupProperty.Name);
                    List<int> boundary = new List<int>();
                    //List<int> highBoundary = new List<int>();
                    List<int> fhs = new List<int>();
                    BoundaryByGroupNumber.Add(group, boundary);
                    FhByGroupNumber.Add(group, fhs);

                    List<int> temp = new List<int>();
                    foreach (var footholdProperty in groupProperty.WzProperties)
                    {
                        int fh = int.Parse(footholdProperty.Name);
                        fhs.Add(fh);
                        //Console.WriteLine($"fh={fh}, fhElement.Elements().Count()={fhElement.Elements().Count()}");
                        int x1 = footholdProperty["x1"].GetInt();
                        int y1 = footholdProperty["y1"].GetInt();
                        int x2 = footholdProperty["x2"].GetInt();
                        int y2 = footholdProperty["y2"].GetInt();

                        Foothold foothold = new Foothold(fh, group, x1, y1, x2, y2);
                        footholds.Add(foothold);

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
                            //if (LoadHeight(footholdElement, x1) > 65)
                            //{
                            //    highBoundary.Add(x1);
                            //}
                        }
                    }
                   
                    int max = temp.Max();
                    int min = temp.Min();

                    if (!boundary.Contains(max))
                        boundary.Add(max);
                    if (!boundary.Contains(min))
                        boundary.Add(min);
                    //if (!highBoundary.Contains(max))
                    //    highBoundary.Add(max);
                    //if (!highBoundary.Contains(min))
                    //    highBoundary.Add(min);

                    boundary.Sort();//升序排列
                    //highBoundary.Sort();
                }
            }
        }
    }

    public enum MobType
    {
        None = 0,
        Normal,
        Jump,
        Fly,
        Immovable
    }

    public class Foothold
    {
        int number;
        int group;
        int x1;
        int x2;
        int y1;
        int y2;
        private int fh;

        public Foothold(int fh, int group, int x1, int y1, int x2, int y2)
        {
            this.fh = fh;
            this.group = group;
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
        }
    }
}
