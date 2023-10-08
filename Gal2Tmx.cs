using GraphicsGaleWrapper;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

// -tiletypes <file.gal>
// - add export tile count limit checker "-maxtiles <number>"

namespace gal2tmx
{
    internal class Gal2Tmx
    {
        private string[] mArgs;


        private string DestinationFolder { get; set; }
        private string TilesetDestinationFolder { get; set; }
        private string AnimatedTilesetDestinationFolder { get; set; }
        private string SourceName { get; set; }
        private string SourcePath { get; set; }
        private string TilemapDestinationPath { get; set; }
        private string TiledTilesetName { get; set; }
        private string TiledTilesetFilename { get; set; }
        private string TiledTilesetBitmapName { get; set; }
        private string TSXDestinationPath { get; set; }

        private string TiledTilesetBmpDestinationPath { get; set; }
        private string TilesetBmpDestinationPath { get; set; }
        private string TilesetFilename { get; set; }

        // optional flags
        private bool ForceOverwrite { get; set; } = false;
        private bool IsTilesetOnly { get; set; } = false;
        private bool IsTilesetAnimated { get; set; } = false;

        private bool IsBreakable { get; set; } = false;

        private string TileTypesPath { get; set; }

        public Gal2Tmx()
        {

        }

        public int Run(string[] args)
        {
            mArgs = args;

            if (mArgs.Length == 0)
            {
                Console.WriteLine("No filename specified");
                return -1;
            }

            ProcessArguments(mArgs);

            try
            {
                CheckExists();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }



            int result = ForceOverwrite ? 1 : CheckOverwrites();

            if (result == 0)
            {
                Console.WriteLine("Conversion aborted.");
                return -1;
            }

            int metatileWidth = 16;
            int metatileHeight = 16;

            SplitBitmap tileTypesblocks = null; 

            // load the tiletypes file, if given, and split into blocks.
            if (!String.IsNullOrEmpty(TileTypesPath))
            {
                var tileTypesGGObject = new GaleObject(TileTypesPath);
                var tileTypesBitmap = tileTypesGGObject.Frames[0].Layers[0].CreateBitmap();
                var tileTypes4bppBitmap = BitmapUtils.Create4bppBitmap(tileTypesBitmap, tileTypesGGObject.Palette);
                tileTypesblocks = new SplitBitmap();
                tileTypesblocks.SplitLinearly(tileTypes4bppBitmap, 
                                              null, 
                                              null,
                                              metatileWidth, 
                                              metatileHeight, 
                                              false, 
                                              SplitBitmap.ExportFlipType.None);
            }

            var ggObject = new GaleObject(SourcePath);

            if (!GraphicsGaleObjectIsAcceptable(ggObject))
            {
                Console.WriteLine("Graphics gale file doesn't have the format we want.");
                Console.WriteLine("Conversion aborted.");
                return -1;
            }

            var frame = ggObject.Frames[0];

            var ggFrameBitmap = frame.Layers[0].CreateBitmap();

            Bitmap ggTileTypesBitmap = null;

            var tiletypesLayer = frame.Layers.FirstOrDefault(l => l.Name.StartsWith("tiletypes"));

            if (tiletypesLayer != null)
            {
                ggTileTypesBitmap = tiletypesLayer.CreateBitmap();
            }

            /*
            System.IO.DirectoryInfo di = new DirectoryInfo(testResultsFolder);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            */

            // the ggbitmap is 32 bit. convert to 4bit
            var ggFrame4bppBitmap = BitmapUtils.Create4bppBitmap(ggFrameBitmap, ggObject.Palette);
            //fbppBitmap.Save(testResultsFolder + "4bppBitmap.bmp", ImageFormat.Bmp);

            // split the image into blocks.
            var tiledTilesetSplitBitmap = new SplitBitmap();
            tiledTilesetSplitBitmap.SplitLinearly(ggFrame4bppBitmap, 
                                                  ggTileTypesBitmap, 
                                                  tileTypesblocks, 
                                                  metatileWidth, 
                                                  metatileHeight, 
                                                  true, 
                                                  SplitBitmap.ExportFlipType.None);

            int rowWidth = IsTilesetAnimated ? 1 : 10;

            Bitmap tiledTilesetBitmap = BitmapUtils.PackTilesetBitmap(tiledTilesetSplitBitmap.UniqueBitmapTiles, rowWidth, metatileWidth, metatileHeight);
            tiledTilesetBitmap.Save(TiledTilesetBmpDestinationPath, ImageFormat.Bmp);

            // split the blocks into a tileset
            int tileWidth = 8;
            int tileHeight = 8;

            var tilesetSplitBitmap = new SplitBitmap();
            tilesetSplitBitmap.SplitLinearly(tiledTilesetBitmap, 
                                             null, 
                                             null,
                                             tileWidth, 
                                             tileHeight,
                                             true,
                                             SplitBitmap.ExportFlipType.SegaMasterSystem);

            // Save the tsx
            TiledUtils.SaveTSX(TSXDestinationPath,
                               TiledTilesetBitmapName,
                               TiledTilesetName,
                               tiledTilesetBitmap.Width,
                               tiledTilesetBitmap.Height,
                               metatileWidth, 
                               metatileHeight,
                               IsTilesetAnimated,
                               tilesetSplitBitmap.UniqueBitmapTiles.Count,
                               tiledTilesetSplitBitmap.UniqueBitmapTiles);

            if (!IsTilesetAnimated && !IsTilesetOnly)
            {
                // save the tmx but not for animated or just tilesets

                string oldFilename = Path.ChangeExtension(TilemapDestinationPath, "tmx.old");
                if (File.Exists(oldFilename))
                {
                    File.Delete(oldFilename);
                    System.IO.File.Move(TilemapDestinationPath, oldFilename);
                }

                TiledUtils.SaveTMX(TilemapDestinationPath, TiledTilesetFilename, metatileWidth, metatileHeight, tiledTilesetSplitBitmap.BitmapTileMap);
            }



            Bitmap tilesetBitmap = null;

            tilesetBitmap = BitmapUtils.PackTilesetBitmap(tilesetSplitBitmap.UniqueBitmapTiles, 2, tileWidth, tileHeight);
 
            // uncomment to save the contents of the 8x8 tile set bitmap
            //tilesetBitmap.Save(TilesetBmpDestinationPath, ImageFormat.Bmp);

            TilesetUtils.ExportTileset(TilesetDestinationFolder, 
                                       TilesetFilename, 
                                       SourceName,
                                       tilesetSplitBitmap, 
                                       IsTilesetAnimated,
                                       IsBreakable);

            if (IsTilesetAnimated)
            {
                // copy .gal to destination folder

                File.Copy(SourcePath, AnimatedTilesetDestinationFolder + SourceName + ".animatedtileset.gal", true);
            }

            Console.WriteLine("Conversion complete.");

            return 0;
        }

        private bool GraphicsGaleObjectIsAcceptable(GaleObject ggObject)
        {
            if (ggObject.FrameCount == 0)
            {
                Console.WriteLine("Needs at least one frame.");
                return false;
            }

            if (ggObject.Frames[0].LayerCount == 0)
            {
                Console.WriteLine("Needs at least one layer.");
                return false;
            }

            var palette = ggObject.Palette;

            if (palette == null || palette.Entries.Length != 16)
            {
                Console.WriteLine("Only works on 4bpp images.");
                return false;
            }

            return true;
        }

        private void ProcessArguments(string[] args)
        {
            int numPaths = BuildFilenames(args);

            for (int loop = numPaths; loop < args.Length; loop++)
            {
                string arg = args[loop];

                if (arg == "-y" || arg == "-o")
                    ForceOverwrite = true;


                if (arg == "-tiletypes")
                {
                    if (loop + 1 < args.Length)
                    {
                        TileTypesPath = args[loop + 1];
                        if (TileTypesPath.StartsWith("-"))
                        {
                            throw new Exception("No valid value given for TileTypes .gal file");
                        }

                        if (!File.Exists(TileTypesPath))
                        {
                            throw new Exception("Tiletypes .gal file not found");
                        }
                    }
                }

                if (arg == "-tilesetdest")
                {
                    if (loop + 1 < args.Length)
                    {
                        TilesetDestinationFolder = args[loop + 1];
                        if (TilesetDestinationFolder.StartsWith("-"))
                        {
                            throw new Exception("No valid value given for tileset destination");
                        }

                        TilesetDestinationFolder += '\\';

                        if (!Directory.Exists(TilesetDestinationFolder))
                        {
                            Directory.CreateDirectory(TilesetDestinationFolder);
                        }
                    }
                }

                // where the copied animated tile .gal file will be copied
                if (arg == "-animationdest")
                {
                    if (loop + 1 < args.Length)
                    {
                        AnimatedTilesetDestinationFolder = args[loop + 1];
                        if (AnimatedTilesetDestinationFolder.StartsWith("-"))
                        {
                            throw new Exception("No valid value given for animation destination");
                        }

                        AnimatedTilesetDestinationFolder += '\\';

                        if (!Directory.Exists(AnimatedTilesetDestinationFolder))
                        {
                            Directory.CreateDirectory(AnimatedTilesetDestinationFolder);
                        }

                    }
                }
            }
        }

        private int BuildFilenames(string[] args)
        {
            int numPaths = 1;

            SourcePath = args[0];

            string sourceNameAndOptions = Path.GetFileNameWithoutExtension(SourcePath);

            var tokens = sourceNameAndOptions.Split('.');

            foreach (var token in tokens)
            {
                if (token == "animatedtileset")
                {
                    IsTilesetAnimated = true;
                }

                if (token == "tileset")
                {
                    IsTilesetOnly = true;
                }

                if (token == "breakable")
                {
                    IsBreakable = true;
                }
            }

            SourceName = tokens[0];

            if (args.Length == 1)
            {
                DestinationFolder = Path.GetDirectoryName(SourcePath);
            }
            else
            {
                string arg = args[1];

                if (arg.StartsWith("-"))
                {
                    DestinationFolder = Path.GetDirectoryName(SourcePath);
                }
                else
                {
                    DestinationFolder = arg;
                    numPaths++;
                }
            }

            if (!String.IsNullOrEmpty(DestinationFolder) && !DestinationFolder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                DestinationFolder += Path.DirectorySeparatorChar;

            TiledTilesetName = SourceName + "_tileset";
            TiledTilesetBitmapName = TiledTilesetName + "_tsx.bmp";
            TiledTilesetFilename = TiledTilesetName + ".tsx";

            TilesetFilename = SourceName + "_tileset";

            // use the same destination folder for everything right now, but
            // leave the option to set different ones.
            TSXDestinationPath = DestinationFolder + TiledTilesetFilename;
            TilemapDestinationPath = DestinationFolder + SourceName + ".tmx";
            TiledTilesetBmpDestinationPath = DestinationFolder + TiledTilesetBitmapName;
            TilesetBmpDestinationPath = DestinationFolder + SourceName + "_tileset.bmp";

            return numPaths;
        }

        private void CheckExists()
        {
            if (!File.Exists(SourcePath))
            {
                throw new Exception("File not found");
            }

            if (!String.IsNullOrWhiteSpace(DestinationFolder) && !Directory.Exists(DestinationFolder))
            {
                Directory.CreateDirectory(DestinationFolder);
            }

            if (!String.IsNullOrWhiteSpace(TilesetDestinationFolder) && !Directory.Exists(TilesetDestinationFolder))
            {
                Directory.CreateDirectory(TilesetDestinationFolder);
            }
        }

        // returns 1 to continue
        //         0 to stop
        private int CheckOverwrites()
        {
            if ((!IsTilesetAnimated && !IsTilesetOnly && File.Exists(TilemapDestinationPath)) ||
                File.Exists(TSXDestinationPath) ||
                File.Exists(TilesetBmpDestinationPath) ||
                (!IsTilesetAnimated && !IsTilesetOnly && File.Exists(TiledTilesetBmpDestinationPath + ".h") || File.Exists(TiledTilesetBmpDestinationPath + ".c")))
            {
                Console.WriteLine("Destination files already exist. Overwrite?");

                var keyInfo = Console.ReadKey();

                char answer = keyInfo.KeyChar;

                while (keyInfo.Key != ConsoleKey.Enter)
                    keyInfo = Console.ReadKey();

                if (answer == 'y' || answer == 'Y')
                {
                    Console.WriteLine("Overwriting files.");
                    return 1;
                }
                else
                {
                    Console.WriteLine("Not overwriting files.");
                    return 0;
                }
            }

            return 1;
        }
    }
}