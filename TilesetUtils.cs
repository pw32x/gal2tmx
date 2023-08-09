using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace gal2tmx
{
    internal class TilesetUtils
    {
        internal static void ExportTileset(string destinationFolder, 
                                           string tilesetName, 
                                           SplitBitmap tilesetSplitBitmap, 
                                           bool animated)
        {
            string headerName = tilesetName + ".h";
            string headerPath = destinationFolder + headerName;
            string sourcePath = destinationFolder + tilesetName + ".c";

            WriteHeader(headerPath, tilesetName);
            WriteSource(sourcePath, headerName, tilesetName, tilesetSplitBitmap, animated);
        }


        private static void WriteHeader(string headerPath, 
                                        string tilesetName)
        {

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("#ifndef " + tilesetName.ToUpper() + "_INCLUDE_H");
            stringBuilder.AppendLine("#define " + tilesetName.ToUpper() + "_INCLUDE_H");
            stringBuilder.AppendLine("#include \"maptypes.h\"");

            stringBuilder.AppendLine("");

            stringBuilder.AppendLine("extern const Tileset " + tilesetName + ";");

            stringBuilder.AppendLine("");
            stringBuilder.AppendLine("#endif");

            System.IO.StreamWriter file = new System.IO.StreamWriter(headerPath);
            file.WriteLine(stringBuilder.ToString());
            file.Close();
        }

        private static void WriteMetatileLUT(StringBuilder stringBuilder,
                                             string tilesetName,
                                             BitmapTileMap metaTileBitmapTileMap,
                                             bool animated)
        {
            var map = metaTileBitmapTileMap.Map;

            int metatileCounter = 0;

            string typeName = "const unsigned short const " + tilesetName + "_metatiles[" + metaTileBitmapTileMap.Map.Count + "]";

            stringBuilder.AppendLine(typeName + " = // " + metaTileBitmapTileMap.Map.Count / 4 + " metatiles" );
            stringBuilder.AppendLine("{");
            for (int y = 0; y < metaTileBitmapTileMap.Height; y += 2)
            {
                for (int x = 0; x < metaTileBitmapTileMap.Width; x += 2)
                {
                    uint value1 = animated ? 0 : map[x + (y * metaTileBitmapTileMap.Width)];
                    uint value2 = animated ? 1 : map[(x + 1) + (y * metaTileBitmapTileMap.Width)];
                    uint value3 = animated ? 2 : map[x + ((y + 1) * metaTileBitmapTileMap.Width)];
                    uint value4 = animated ? 3 : map[(x + 1) + ((y + 1) * metaTileBitmapTileMap.Width)];

                    stringBuilder.AppendLine("    // metatile " + metatileCounter);

                    stringBuilder.AppendLine("    " + value1 + ", " + value2 + ",");
                    stringBuilder.AppendLine("    " + value3 + ", " + value4 + ",");

                    metatileCounter++;
                }
            }

            stringBuilder.AppendLine("};");
        }

        /*

        void WriteToFourBytes(char value, char shift, BYTE bytes[4])
        {
            bytes[0] |= ((value & 0x01) >> 0) << shift;
            bytes[1] |= ((value & 0x02) >> 1) << shift;
            bytes[2] |= ((value & 0x04) >> 2) << shift;
            bytes[3] |= ((value & 0x08) >> 3) << shift;
        }

        void ConvertToPlanar(int row, const BYTE* tileData, BYTE bytes[4])
        {
            BYTE rowData[8];

            for (int loop = 0; loop < SMS_TILE_WIDTH; loop++)
            {
                BYTE value = tileData[loop + (row * SMS_TILE_WIDTH)];

                rowData[loop] = value;
            }

            for (int loop = 0; loop < 8; loop++)
            {
                WriteToFourBytes(rowData[loop], 7 - loop, bytes);
            }
        }

        void OutputTilePlanar(std::ofstream& sourceFile, const Tile& tile)
        {
            for (int row = 0; row < SMS_TILE_HEIGHT; row++)
            {
                BYTE bytes[4];
                memset(bytes, 0, sizeof(bytes));
                ConvertToPlanar(row, tile.data(), bytes);

                sourceFile << "    ";

                for (int loop = 0; loop < 4; loop++)
                {
                    sourceFile << WriteByteAsHex(bytes[loop]) <<", ";
                }

                sourceFile << "\n";
            }
        }

        */

        private static void WriteToFourBytes(byte value, byte shift, byte[] bytes)
        {
            bytes[0] |= (byte)(((value & 0x01) >> 0) << shift);
            bytes[1] |= (byte)(((value & 0x02) >> 1) << shift);
            bytes[2] |= (byte)(((value & 0x04) >> 2) << shift);
            bytes[3] |= (byte)(((value & 0x08) >> 3) << shift);
        }

        private unsafe static void ConvertToPlanar(int row, byte* tileData, byte[] outBytes)
        {
            var rowData = new byte[8];

            for (int loop = 0; loop < 8; loop++)
            {
                byte value = tileData[loop + (row * 8)];

                rowData[loop] = value;
            }

            for (byte loop = 0; loop < 8; loop++)
            {
                WriteToFourBytes(rowData[loop], (byte)(7 - loop), outBytes);
            }
        }


        private unsafe static void OutputTilePlanar(StringBuilder stringBuilder, byte* tileData)
        {
            for (int row = 0; row < 8; row++)
            {
                byte[] bytes = { 0, 0, 0, 0 };

                ConvertToPlanar(row, tileData, bytes);

                stringBuilder.Append("    ");

                for (int loop = 0; loop < 4; loop++)
                {
                    stringBuilder.Append("0x" + bytes[loop].ToString("X2") + ", ");
                }

                stringBuilder.Append("\n");
            }
        }

        private static void WriteTilesetTiles(StringBuilder stringBuilder, 
                                              string tilesetName, 
                                              List<BitmapTile> uniqueBitmapTiles)
        {
            int tileCounter = 0;

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("const unsigned char const " + tilesetName + "_tiles[" + uniqueBitmapTiles.Count * 32 + "] = // " + uniqueBitmapTiles.Count + " tiles");
            stringBuilder.AppendLine("{");

            foreach (var bitmapTile in uniqueBitmapTiles)
            {
                stringBuilder.AppendLine("// tile: " + tileCounter++);

                Bitmap bitmap = bitmapTile.Bitmap;

                var bitmapData = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size), 
                                                 ImageLockMode.ReadOnly, 
                                                 PixelFormat.Format8bppIndexed);

                unsafe
                {
                    byte* pixels = (byte*)bitmapData.Scan0;

                    int stride = bitmapData.Stride;
                    int len = stride * bitmap.Height;

                    OutputTilePlanar(stringBuilder, pixels);
                }

                bitmap.UnlockBits(bitmapData);
            }

            stringBuilder.AppendLine("};");
        }

        private static void WriteTilesetStruct(StringBuilder stringBuilder, 
                                               string tilesetName,
                                               SplitBitmap tilesetSplitBitmap)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("const Tileset " + tilesetName + " = ");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine("    " + tilesetName + "_tiles,");
            stringBuilder.AppendLine("    " + tilesetName + "_metatiles,");
            stringBuilder.AppendLine("    " + tilesetSplitBitmap.UniqueBitmapTiles.Count + ", // unique tile count");
            stringBuilder.AppendLine("    " + tilesetSplitBitmap.BitmapTileMap.Map.Count / 4 + ", // 16x16 metatiles count");
            stringBuilder.AppendLine("};");
        }

        private static void WriteSource(string sourcePath, 
                                        string headerName, 
                                        string tilesetName, 
                                        SplitBitmap tilesetSplitBitmap, 
                                        bool animated)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("#include \"" + headerName + "\"");
            stringBuilder.AppendLine("");

            WriteMetatileLUT(stringBuilder, tilesetName, tilesetSplitBitmap.BitmapTileMap, animated);
            WriteTilesetTiles(stringBuilder, tilesetName, tilesetSplitBitmap.UniqueBitmapTiles);
            WriteTilesetStruct(stringBuilder, tilesetName, tilesetSplitBitmap);

            System.IO.StreamWriter file = new System.IO.StreamWriter(sourcePath);
            file.WriteLine(stringBuilder.ToString());
            file.Close();
        }


    }
}