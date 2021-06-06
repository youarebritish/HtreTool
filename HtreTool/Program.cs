using Newtonsoft.Json;
using System;
using System.Drawing.Imaging;
using System.IO;

namespace HtreTool
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach(var arg in args)
            {
                Console.WriteLine(arg);
                if (!File.Exists(arg))
                {
                    continue;
                }

                if (Path.GetExtension(arg) != ".htre")
                {
                    continue;
                }

                using(var reader = new BinaryReader(new FileStream(arg, FileMode.Open)))
                {
                    var tile = TerrainTileFile.Read(reader);
                    if (tile == null)
                    {
                        continue;
                    }

                    var heightmapFilename = Path.GetFileNameWithoutExtension(arg) + "_height_map.r32";
                    using (var file = File.Create(heightmapFilename))
                    {
                        using (var writer = new BinaryWriter(file))
                        {
                            foreach (float value in tile.HeightMap)
                            {
                                writer.Write(value);
                            }
                        }
                    }

                    var materialWeightMapFilename = Path.GetFileNameWithoutExtension(arg) + "_material_weight_map.png";
                    tile.MaterialWeightMap.Save(materialWeightMapFilename, ImageFormat.Png);

                    var materialSelectMapFilename = Path.GetFileNameWithoutExtension(arg) + "_material_select_map.png";
                    tile.MaterialSelectMap.Save(materialSelectMapFilename, ImageFormat.Png);

                    var configIdsMapFilename = Path.GetFileNameWithoutExtension(arg) + "_configuration_ids_map.png";
                    tile.ConfigurationIdsMap.Save(configIdsMapFilename, ImageFormat.Png);

                    var json = JsonConvert.SerializeObject(tile.Parameters, Formatting.Indented);
                    File.WriteAllText(Path.GetFileNameWithoutExtension(arg) + ".htre.json", json);
                }
            }
        }
    }
}
