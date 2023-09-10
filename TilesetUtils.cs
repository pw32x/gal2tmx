using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace gal2tmx
{
    internal class TilesetUtils
    {
        internal static void ExportTileset(string destinationFolder, 
                                           string tilesetName, 
                                           SplitBitmap tilesetSplitBitmap, 
                                           Dictionary<uint, uint> animatedTiles,
                                           bool animated)
        {
            string headerName = tilesetName + ".h";
            string headerPath = destinationFolder + headerName;
            string sourcePath = destinationFolder + tilesetName + ".c";

            WriteHeader(headerPath, tilesetName);
            WriteSource(sourcePath, headerName, tilesetName, tilesetSplitBitmap, animatedTiles, animated);
        }


        private static void WriteHeader(string headerPath, 
                                        string tilesetName)
        {

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("#ifndef " + tilesetName.ToUpper() + "_INCLUDE_H");
            stringBuilder.AppendLine("#define " + tilesetName.ToUpper() + "_INCLUDE_H");
            stringBuilder.AppendLine("#include \"map_types.h\"");

            stringBuilder.AppendLine("");

            stringBuilder.AppendLine("extern const Tileset " + tilesetName + ";");

            stringBuilder.AppendLine("");
            stringBuilder.AppendLine("#endif");

            System.IO.StreamWriter file = new System.IO.StreamWriter(headerPath);
            file.WriteLine(stringBuilder.ToString());
            file.Close();
        }

        private static HashSet<uint> m_animatedTiles = new HashSet<uint>();
        private static void AddToAnimatedTiles(uint value)
        {
            m_animatedTiles.Add(value & 0x1FF);
        }

        private static List<uint> WriteMetatileLUT(StringBuilder stringBuilder,
                                             string tilesetName,
                                             BitmapTileMap metaTileBitmapTileMap,
                                             Dictionary<uint, uint> animatedTiles,
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

                    /*
                    bool changed = false;
                    value1 = ApplyAnimationModifier(value1, animatedTiles, ref changed);
                    value2 = ApplyAnimationModifier(value2, animatedTiles, ref changed);
                    value3 = ApplyAnimationModifier(value3, animatedTiles, ref changed);
                    value4 = ApplyAnimationModifier(value4, animatedTiles, ref changed);
                    */

                    int metatilex = x / 2;
                    int metatiley = y / 2;
                    int metatilewidth = metaTileBitmapTileMap.Width / 2;
                    int metatileindex = metatilex + (metatiley * metatilewidth);

                    if (animatedTiles.ContainsKey((uint)metatileindex))
                    {
                        AddToAnimatedTiles(value1);
                        AddToAnimatedTiles(value2);
                        AddToAnimatedTiles(value3);
                        AddToAnimatedTiles(value4);
                        stringBuilder.AppendLine("    // animated");
                    }

                    stringBuilder.AppendLine("    // metatile " + metatileCounter);


                    stringBuilder.AppendLine("    " + value1 + ", " + value2 + ",");
                    stringBuilder.AppendLine("    " + value3 + ", " + value4 + ",");

                    metatileCounter++;
                }
            }

            stringBuilder.AppendLine("};");

            return m_animatedTiles.ToList();
        }

        
        //private static uint ApplyAnimationModifier(uint value, Dictionary<uint, uint> animatedTiles, ref bool changed)
        //{
        //    
        //    
        //    uint flags = value & 0xFE00;
        //
        //    if (animatedTiles.ContainsKey(value))
        //    {
        //        animatedTileVdpIndex--;
        //        changed = true;
        //        return animatedTiles[value] | flags;
        //    }
        //    else
        //    {
        //        return value;
        //    }
        //}

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
                                               SplitBitmap tilesetSplitBitmap,
                                               List<uint> animatedTilesIndexes)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("const Tileset " + tilesetName + " = ");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine("    " + tilesetName + "_tiles,");
            stringBuilder.AppendLine("    " + tilesetName + "_metatiles,");
            stringBuilder.AppendLine("    " + tilesetSplitBitmap.UniqueBitmapTiles.Count + ", // unique tile count");
            stringBuilder.AppendLine("    " + tilesetSplitBitmap.BitmapTileMap.Map.Count / 4 + ", // 16x16 metatiles count");

            if (animatedTilesIndexes.Count > 0)
                stringBuilder.AppendLine("    " + tilesetName + "AnimatedTileIndexes, // animated tile indexes count");
            else
                stringBuilder.AppendLine("    NULL, // animated tile indexes count");
            stringBuilder.AppendLine("    " + animatedTilesIndexes.Count + ", // animated tile indexes count");

            stringBuilder.AppendLine("};");
        }

        private static void WriteSource(string sourcePath, 
                                        string headerName, 
                                        string tilesetName, 
                                        SplitBitmap tilesetSplitBitmap, 
                                        Dictionary<uint, uint> animatedTiles,
                                        bool animated)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("#include \"" + headerName + "\"");
            stringBuilder.AppendLine("");

            List<uint> animatedTilesIndexes = WriteMetatileLUT(stringBuilder, 
                                              tilesetName, 
                                              tilesetSplitBitmap.BitmapTileMap, 
                                              animatedTiles,
                                              animated);

            WriteTilesetTiles(stringBuilder, tilesetName, tilesetSplitBitmap.UniqueBitmapTiles);

            if (animatedTilesIndexes.Count > 0)
                WriteAnimatedTilesIndexes(stringBuilder, tilesetName, animatedTilesIndexes);

            WriteTilesetStruct(stringBuilder, tilesetName, tilesetSplitBitmap, animatedTilesIndexes);

            System.IO.StreamWriter file = new System.IO.StreamWriter(sourcePath);
            file.WriteLine(stringBuilder.ToString());
            file.Close();
        }

        private static void WriteAnimatedTilesIndexes(StringBuilder stringBuilder, 
                                                      string tilesetName,
                                                      List<uint> animatedTilesIndexes)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("const u16 " + tilesetName + "AnimatedTileIndexes[] = ");
            stringBuilder.AppendLine("{");

            foreach (uint index in animatedTilesIndexes)
            {
                stringBuilder.AppendLine("    " + index + ",");
            }

            stringBuilder.AppendLine("};");
        }
    }
}