using System.Text;

namespace gal2tmx
{
    internal class MetatilesUtils
    {
        internal static void ExportMetatilesCode(string blocksetMapsDestinationFolder, string blocksetMapName, TileMap tileMap, bool animated)
        {
            string headerName = blocksetMapName + ".h";
            string headerPath = blocksetMapsDestinationFolder + headerName;
            string sourcePath = blocksetMapsDestinationFolder + blocksetMapName + ".c";

            string typeName = WriteHeader(headerPath, blocksetMapName, tileMap);
            WriteSource(sourcePath, headerName, typeName, tileMap, animated);
        }


        private static string WriteHeader(string headerPath, string blocksetMapName, TileMap tileMap)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("#ifndef " + blocksetMapName.ToUpper() + "_INCLUDE_H");
            stringBuilder.AppendLine("#define " + blocksetMapName.ToUpper() + "_INCLUDE_H");
            stringBuilder.AppendLine("#include <genesis.h>");
            stringBuilder.AppendLine("");

            int blockCount = (tileMap.Map.Count / 4);

            string typeName = "const u16 const " + blocksetMapName + "[" + tileMap.Map.Count + "]";

            stringBuilder.AppendLine("extern " + typeName + "; // " + blockCount + " blocks of 2x2 tiles");

            stringBuilder.AppendLine("");
            stringBuilder.AppendLine("#endif");

            System.IO.StreamWriter file = new System.IO.StreamWriter(headerPath);
            file.WriteLine(stringBuilder.ToString());
            file.Close();

            return typeName;
        }

        private static void WriteSource(string sourcePath, string headerName, string typeName, TileMap tileMap, bool animated)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("#include \"" + headerName + "\"");
            stringBuilder.AppendLine("");
            stringBuilder.AppendLine(typeName + " = ");
            stringBuilder.AppendLine("{");

            var map = tileMap.Map;

            int blockCounter = 0;

            for (int y = 0; y < tileMap.Height; y += 2)
            {
                for (int x = 0; x < tileMap.Width; x += 2)
                {
                    uint value1 = animated ? 0 : map[x + (y * tileMap.Width)];
                    uint value2 = animated ? 1 : map[(x + 1) + (y * tileMap.Width)];
                    uint value3 = animated ? 2 : map[x + ((y + 1) * tileMap.Width)];
                    uint value4 = animated ? 3 : map[(x + 1) + ((y + 1) * tileMap.Width)];

                    stringBuilder.AppendLine("    // block " + blockCounter);

                    stringBuilder.AppendLine("    " + value1 + ", " + value2 + ",");
                    stringBuilder.AppendLine("    " + value3 + ", " + value4 + ",");

                    blockCounter++;
                }
            }

            stringBuilder.AppendLine("};");

            System.IO.StreamWriter file = new System.IO.StreamWriter(sourcePath);
            file.WriteLine(stringBuilder.ToString());
            file.Close();
        }
    }
}