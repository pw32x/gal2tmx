using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace gal2tmx
{
    internal class BitmapTile
    {
        public BitmapTile(int index, Bitmap bitmap, int attribute)
        {
            Index = index;
            Bitmap = bitmap;
            Attribute = attribute;
        }

        public int Index { get; }
        public Bitmap Bitmap { get; }
        public int Attribute { get; }
    }

    internal class BitmapTileMap
    {
        public int Width { get; }
        public int Height { get; }
        public List<uint> Map { get; } // contains the index of the unique tile used at this map location. 

        public BitmapTileMap(int width, int height)
        {
            Width = width;
            Height = height;
            Map = new List<uint>(Width * Height);
        }
    }

    internal class SplitBitmap
    {
        public enum ExportFlipType
        {
            None,
            Genesis,
            Tiled
        }

        public List<BitmapTile> UniqueBitmapTiles { get; } = new List<BitmapTile>();

        public BitmapTileMap BitmapTileMap { get; }

        public unsafe SplitBitmap(Bitmap bitmap, 
                                  Bitmap attributeBitmap, 
                                  SplitBitmap tileTypes,
                                  int splitWidth, 
                                  int splitHeight, 
                                  bool removeDuplicates, 
                                  ExportFlipType exportFlipType)
        {
            int tilesWidth = bitmap.Width / splitWidth;
            int tilesHeight = bitmap.Height / splitHeight;

            int tileIndex = 0;

            BitmapTileMap = new BitmapTileMap(tilesWidth, tilesHeight);

            for (int tiley = 0; tiley < tilesHeight; tiley++)
            {
                for (int tilex = 0; tilex < tilesWidth; tilex++)
                {
                    int bitmapx = tilex * splitWidth;
                    int bitmapy = tiley * splitHeight;

                    Rectangle rect = new Rectangle(bitmapx,
                                                   bitmapy, 
                                                   splitWidth,
                                                   splitHeight);

                    // get attribute for tile
                    int tileAttribute = 0;

                    // the attribute (TILE_SOLID, etc) comes from the index of the color in the second layer
                    // of the image.
                    if (attributeBitmap != null && tileTypes != null)
                    {
                        var attributeBitmapTile = attributeBitmap.Clone(rect, System.Drawing.Imaging.PixelFormat.DontCare);

                        var foundTile = tileTypes.getTileWithSameBitmap(attributeBitmapTile, ExportFlipType.None);

                        if (foundTile != null)
                            tileAttribute = foundTile.Item1.Index;

                        //BitmapData sourceBitmapData = attributeBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format4bppIndexed);
                        //byte* sourceBuffer = (byte*)sourceBitmapData.Scan0.ToPointer();
                        //tileAttribute = sourceBuffer[0];
                        //tileAttribute &= 0x0f;
                        //attributeBitmap.UnlockBits(sourceBitmapData);

                    }

                    uint tileIndexToUse;

                    if (removeDuplicates)
                    {
                        var bitmapTile = bitmap.Clone(rect, System.Drawing.Imaging.PixelFormat.DontCare);


                        RotateFlipType flipFlag = RotateFlipType.RotateNoneFlipNone;

                        var tuple = getTileWithSameBitmap(bitmapTile, exportFlipType);

                        if (tuple == null)
                        {
                            var newtile = new BitmapTile(tileIndex, bitmapTile, tileAttribute);

                            UniqueBitmapTiles.Add(newtile);

                            tileIndexToUse = (uint)tileIndex;

                            tileIndex++;
                        }
                        else
                        {
                            var tile = tuple.Item1; // tile
                            tileIndexToUse = (uint)tile.Index;
                            flipFlag = tuple.Item2;
                        }

                        tileIndexToUse = ApplyFlip(tileIndexToUse, flipFlag, exportFlipType);
                    }
                    else
                    {
                        var bitmapTile = bitmap.Clone(rect, System.Drawing.Imaging.PixelFormat.DontCare);
                        var newtile = new BitmapTile(tileIndex, bitmapTile, tileAttribute);

                        UniqueBitmapTiles.Add(newtile);

                        tileIndexToUse = (uint)tileIndex;

                        tileIndex++;
                    }

                    BitmapTileMap.Map.Add(tileIndexToUse);
                }
            }
        }

        private uint ApplyFlip(uint tileIndexToUse, RotateFlipType flipFlag, ExportFlipType exportFlipType)
        {
            const uint GenesisFlippedHorizontallyFlag = (1 << 11);
            const uint GenesisFlippedVerticallyFlag = (1 << 12);
            const uint TILED_FLIPPED_HORIZONTALLY_FLAG = 0x80000000;
            const uint TILED_FLIPPED_VERTICALLY_FLAG = 0x40000000;

            if (exportFlipType == ExportFlipType.Tiled)
            {
                switch (flipFlag)
                {
                    case RotateFlipType.RotateNoneFlipX:
                        tileIndexToUse |= TILED_FLIPPED_HORIZONTALLY_FLAG;
                        break;
                    case RotateFlipType.RotateNoneFlipY:
                        tileIndexToUse |= TILED_FLIPPED_VERTICALLY_FLAG;
                        break;
                    case RotateFlipType.RotateNoneFlipXY:
                        tileIndexToUse |= TILED_FLIPPED_VERTICALLY_FLAG;
                        tileIndexToUse |= TILED_FLIPPED_HORIZONTALLY_FLAG;
                        break;
                }
            }
            else
            {
                switch (flipFlag)
                {
                    case RotateFlipType.RotateNoneFlipX:
                        tileIndexToUse |= GenesisFlippedHorizontallyFlag;
                        break;
                    case RotateFlipType.RotateNoneFlipY:
                        tileIndexToUse |= GenesisFlippedVerticallyFlag;
                        break;
                    case RotateFlipType.RotateNoneFlipXY:
                        tileIndexToUse |= GenesisFlippedVerticallyFlag;
                        tileIndexToUse |= GenesisFlippedHorizontallyFlag;
                        break;
                }
            }
            return tileIndexToUse;
        }

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int memcmp(IntPtr b1, IntPtr b2, long count);

        public static bool CompareMemCmp(Bitmap b1, Bitmap b2)
        {
            if ((b1 == null) != (b2 == null)) return false;
            if (b1.Size != b2.Size) return false;

            var bd1 = b1.LockBits(new Rectangle(new Point(0, 0), b1.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bd2 = b2.LockBits(new Rectangle(new Point(0, 0), b2.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                IntPtr bd1scan0 = bd1.Scan0;
                IntPtr bd2scan0 = bd2.Scan0;

                int stride = bd1.Stride;
                int len = stride * b1.Height;

                return memcmp(bd1scan0, bd2scan0, len) == 0;
            }
            finally
            {
                b1.UnlockBits(bd1);
                b2.UnlockBits(bd2);
            }
        }

        private Tuple<BitmapTile, RotateFlipType> getTileWithSameBitmap(Bitmap bitmap, ExportFlipType exportFlipType)
        {
            foreach (var tile in UniqueBitmapTiles)
            {
                if (CompareMemCmp(bitmap, tile.Bitmap))
                {
                    return new Tuple<BitmapTile, RotateFlipType>(tile, RotateFlipType.RotateNoneFlipNone);
                }

                if (exportFlipType != ExportFlipType.None)
                {
                    // CompareFlippedX
                    var flippedXBitmap = (Bitmap)bitmap.Clone();
                    flippedXBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);

                    if (CompareMemCmp(flippedXBitmap, tile.Bitmap))
                    {
                        return new Tuple<BitmapTile, RotateFlipType>(tile, RotateFlipType.RotateNoneFlipX);
                    }

                    // CompareFlippedY
                    var flippedYBitmap = (Bitmap)bitmap.Clone();
                    flippedYBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

                    if (CompareMemCmp(flippedYBitmap, tile.Bitmap))
                    {
                        return new Tuple<BitmapTile, RotateFlipType>(tile, RotateFlipType.RotateNoneFlipY);
                    }

                    // CompareFlippedXY
                    var flippedXYBitmap = (Bitmap)bitmap.Clone();
                    flippedXYBitmap.RotateFlip(RotateFlipType.RotateNoneFlipXY);

                    if (CompareMemCmp(flippedXYBitmap, tile.Bitmap))
                    {
                        return new Tuple<BitmapTile, RotateFlipType>(tile, RotateFlipType.RotateNoneFlipXY);
                    }
                }
            }

            return null;
        }
    }
}