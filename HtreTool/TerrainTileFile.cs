using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtreTool
{
    public class TerrainTileParameters
    {
        public int Version { get; set; }
        public int Pitch { get; set; }
        public int HeightFormat { get; set; }
        public int ComboFormat { get; set; }
        public int MaxLodLevel { get; set; }
        public List<float> LodParameterFloat { get; set; } = new List<float>();
        public List<int> LodParameterInt { get; set; } = new List<int>();
    }

    class TerrainTileFile
    {
        const int HEIGHTMAP_OFFSET_VERSION4 = 672;
        const int HEIGHTMAP_OFFSET_VERSION3 = 640;

        const int MATERIALIDS_OFFSET_VERSION4 = 33552;
        const int MATERIALIDS_OFFSET_VERSION3 = 33504;

        const int CONFIGIDS_OFFSET_VERSION4 = 33568;
        const int CONFIGIDS_OFFSET_VERSION3 = 33520;

        const int HEIGHTMAP_WIDTH = 64;
        const int HEIGHTMAP_HEIGHT = 64;

        public List<float> HeightMap { get; private set; }
        public Bitmap MaterialWeightMap { get; private set; }
        public Bitmap MaterialSelectMap { get; private set; }
        public Bitmap ConfigurationIdsMap { get; private set; }
        public TerrainTileParameters Parameters { get; private set; } = new TerrainTileParameters();

        public static TerrainTileFile Read(BinaryReader reader)
        {
            var result = new TerrainTileFile();
            result.Parameters.Version = reader.ReadInt32();

            if (result.Parameters.Version != 3 && result.Parameters.Version != 4)
            {
                Console.WriteLine("Unexpected HTRE version.");
                return null;
            }

            result.Parameters.Pitch = ReadPitch(reader, result.Parameters.Version);
            result.Parameters.HeightFormat = ReadHeightFormat(reader, result.Parameters.Version);
            result.Parameters.ComboFormat = ReadComboFormat(reader, result.Parameters.Version);
            result.Parameters.MaxLodLevel = ReadMaxLodLevel(reader, result.Parameters.Version);

            if (result.Parameters.Version == 3)
            {
                reader.BaseStream.Seek(HEIGHTMAP_OFFSET_VERSION3, SeekOrigin.Begin);
            }
            else if (result.Parameters.Version == 4)
            {
                reader.BaseStream.Seek(HEIGHTMAP_OFFSET_VERSION4, SeekOrigin.Begin);
            }

            result.HeightMap = ReadHeightmap(reader);
            result.MaterialWeightMap = ReadMaterialWeightMap(reader);

            if (result.Parameters.Version == 3)
            {
                reader.BaseStream.Seek(MATERIALIDS_OFFSET_VERSION3, SeekOrigin.Begin);
            }
            else if (result.Parameters.Version == 4)
            {
                reader.BaseStream.Seek(MATERIALIDS_OFFSET_VERSION4, SeekOrigin.Begin);
            }

            result.MaterialSelectMap = ReadMaterialSelectMap(reader);

            if (result.Parameters.Version == 3)
            {
                reader.BaseStream.Seek(CONFIGIDS_OFFSET_VERSION3, SeekOrigin.Begin);
            }
            else if (result.Parameters.Version == 4)
            {
                reader.BaseStream.Seek(CONFIGIDS_OFFSET_VERSION4, SeekOrigin.Begin);
            }

            result.ConfigurationIdsMap = ReadConfigIdsMap(reader);

            ReadLodParameter(reader, result.Parameters.Version, result.Parameters);
            return result;
        }

        private static int ReadPitch(BinaryReader reader, int version)
        {
            if (version == 3)
            {
                reader.BaseStream.Seek(92, SeekOrigin.Begin);
            }
            else if (version == 4)
            {
                reader.BaseStream.Seek(92, SeekOrigin.Begin);
            }

            return reader.ReadInt32();
        }

        private static int ReadHeightFormat(BinaryReader reader, int version)
        {
            if (version == 4)
            {
                reader.BaseStream.Seek(92, SeekOrigin.Begin);
                return reader.ReadInt32();
            }
            else
            {
                return -1;
            }
        }

        private static int ReadComboFormat(BinaryReader reader, int version)
        {
            if (version == 4)
            {
                reader.BaseStream.Seek(204, SeekOrigin.Begin);
                return reader.ReadInt32();
            }
            else
            {
                return -1;
            }
        }

        private static int ReadMaxLodLevel(BinaryReader reader, int version)
        {
            if (version == 3)
            {
                reader.BaseStream.Seek(204, SeekOrigin.Begin);
            }
            else if (version == 4)
            {
                reader.BaseStream.Seek(268, SeekOrigin.Begin);
            }

            return reader.ReadInt32();
        }

        private static void ReadLodParameter(BinaryReader reader, int version, TerrainTileParameters parameters)
        {
            if (version == 3)
            {
                reader.BaseStream.Seek(33408, SeekOrigin.Begin);
            }
            else if (version == 4)
            {
                reader.BaseStream.Seek(33440, SeekOrigin.Begin);
            }

            for(var i = 0; i < 16; i++)
            {
                parameters.LodParameterFloat.Add(reader.ReadSingle());
            }

            if (version == 3)
            {
                return;
            }

            for (var i = 0; i < 4; i++)
            {
                parameters.LodParameterInt.Add(reader.ReadInt32());
            }
        }

        private static List<float> ReadHeightmap(BinaryReader reader)
        {
            return ReadMap(HEIGHTMAP_WIDTH, () => reader.ReadSingle() / 1000.0f);
        }

        private static Bitmap ReadMaterialWeightMap(BinaryReader reader)
        {
            var result = new Bitmap(HEIGHTMAP_WIDTH, HEIGHTMAP_HEIGHT, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var pixels = ReadMap(HEIGHTMAP_WIDTH, () => ReadPixel(reader));
            for (var x = 0; x < HEIGHTMAP_WIDTH; x++)
            {
                for (var y = 0; y < HEIGHTMAP_HEIGHT; y++)
                {
                    result.SetPixel(x, y, pixels[x + HEIGHTMAP_WIDTH * y]);
                }
            }

            return result;
        }

        private static Bitmap ReadMaterialSelectMap(BinaryReader reader)
        {
            var result = new Bitmap(2, 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var pixels = ReadMap(2, () => ReadPixel(reader));
            for (var x = 0; x < 2; x++)
            {
                for (var y = 0; y < 2; y++)
                {
                    result.SetPixel(x, y, pixels[x + 2 * y]);
                }
            }

            return result;
        }
        private static Bitmap ReadConfigIdsMap(BinaryReader reader)
        {
            var result = new Bitmap(2, 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var pixels = ReadMap(2, () => ReadPixel(reader));
            for (var x = 0; x < 2; x++)
            {
                for (var y = 0; y < 2; y++)
                {
                    result.SetPixel(x, y, pixels[x + 2 * y]);
                }
            }

            return result;
        }

        private static Color ReadPixel(BinaryReader reader)
        {
            var r = reader.ReadByte();
            var g = reader.ReadByte();
            var b = reader.ReadByte();
            var a = reader.ReadByte();

            // FIXME Hiding the alpha channel for now since it's not outputting correctly
            a = 255;
            return Color.FromArgb(a, r, g, b);
        }

        private static List<T> ReadMap<T>(int width, Func<T> readValue)
        {
            var blocks = ReadBlocks(width / 2, readValue);
            return AssembleBlocks(width, blocks[0], blocks[1], blocks[2], blocks[3]);
        }

        private static List<T[,]> ReadBlocks<T>(int blockWidth, Func<T> readValue)
        {
            var result = new List<T[,]>(4);
            for (var blockIndex = 0; blockIndex < 4; blockIndex++)
            {
                var block = new T[blockWidth, blockWidth];
                result.Add(block);

                for (var i = 0; i < blockWidth; i++)
                {
                    for (var j = 0; j < blockWidth; j++)
                    {
                        block[j, i] = readValue();
                    }
                }
            }

            return result;
        }

        private static List<T> AssembleBlocks<T>(int targetWidth, T[,] blockA, T[,] blockB, T[,] blockC, T[,] blockD)
        {
            var result = new List<T>();
            for (var i = 0; i < targetWidth * targetWidth; i++)
            {
                result.Add(default);
            }

            var halfWidth = targetWidth / 2;
            SetPixels(result, targetWidth, 0, 0, halfWidth, halfWidth, blockA);
            SetPixels(result, targetWidth, 0, halfWidth, halfWidth, halfWidth, blockB);
            SetPixels(result, targetWidth, halfWidth, 0, halfWidth, halfWidth, blockC);
            SetPixels(result, targetWidth, halfWidth, halfWidth, halfWidth, halfWidth, blockD);

            return result;
        }

        private static void SetPixels<T>(List<T> target, int targetWidth, int startX, int startY, int blockWidth, int blockHeight, T[,] block)
        {
            var vals = (from val in block.Cast<T>() select val).ToArray();
            for (var x = 0; x < blockWidth; x++)
            {
                for (var y = 0; y < blockHeight; y++)
                {
                    target[startX + x + ((startY + y) * targetWidth)] = vals[x + y * blockWidth];
                }
            }
        }
    }
}
