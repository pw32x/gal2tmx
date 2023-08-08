using GraphicsGaleWrapper;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

// -tiletypes <file.gal>

namespace gal2tmx
{
    internal class Gal2Tmx
    {
        private string[] mArgs;


        private string DestinationFolder { get; set; } // = @"..\..\..\gamedata\maps\";
        private string TilesetDestinationFolder { get; set; } // = @"..\..\..\gamedata\tilesets\";
        private string MetatilesDestinationFolder { get; set; } // = @"..\..\..\gamedata\metatiles\";
        private string SourceName { get; set; }
        private string SourcePath { get; set; }
        private string TilemapDestinationPath { get; set; }
        private string TiledTilesetName { get; set; }
        private string TiledTilesetFilename { get; set; }
        private string TiledTilesetBitmapName { get; set; }
        private string TSXDestinationPath { get; set; }

        private string TiledTilesetBmpDestinationPath { get; set; }
        private string TilesetBmpDestinationPath { get; set; }
        private string MetatileMapFilename { get; set; }

        // optional flags
        private bool ForceOverwrite { get; set; } = false;
        private bool IsTilesetAnimated { get; set; } = false;
        private int TilesetAnimationSpeed { get; set; } = 10;
        private string TileTypesPath { get; set; } //  = @"..\..\..\assets\themes\tiletypes.gal";

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

            BitmapSplitter tileTypesblocks = null; 

            // load the tiletypes file, if given, and split into blocks.
            if (!String.IsNullOrEmpty(TileTypesPath))
            {
                var tileTypesGGObject = new GaleObject(TileTypesPath);
                var tileTypesBitmap = tileTypesGGObject.Frames[0].Layers[0].CreateBitmap();
                var tileTypes4bppBitmap = BitmapUtils.Create4bppBitmap(tileTypesBitmap, tileTypesGGObject.Palette);
                tileTypesblocks = new BitmapSplitter(tileTypes4bppBitmap, 
                                                     null, 
                                                     null, 
                                                     metatileWidth, 
                                                     metatileHeight, 
                                                     false, 
                                                     BitmapSplitter.ExportFlipType.None);
            }

            var ggObject = new GaleObject(SourcePath);

            if (!GraphicsGaleObjectIsAcceptable(ggObject))
            {
                Console.WriteLine("Graphics gale file doesn't have the format we want.");
                Console.WriteLine("Conversion aborted.");
                return -1;
            }

            var ggFrameBitmap = ggObject.Frames[0].Layers[0].CreateBitmap();

            Bitmap ggAttributeBitmap = null;

            if (ggObject.Frames[0].Layers.Length > 1)
                ggAttributeBitmap = ggObject.Frames[0].Layers[1].CreateBitmap();

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

            Bitmap fbppAttributeBitmap = null;
            if (ggAttributeBitmap != null)
            {
                fbppAttributeBitmap = BitmapUtils.Create4bppBitmap(ggAttributeBitmap, ggObject.Palette);
            }

            // split the image into blocks.
            var tiledTileset = new BitmapSplitter(ggFrame4bppBitmap, 
                                                  ggAttributeBitmap, 
                                                  tileTypesblocks, 
                                                  metatileWidth, 
                                                  metatileHeight, 
                                                  !IsTilesetAnimated, 
                                                  BitmapSplitter.ExportFlipType.None);

            int rowWidth = IsTilesetAnimated ? 1 : 10;

            Bitmap tiledTilesetBitmap = BitmapUtils.PackTilesetBitmap(tiledTileset.Tiles, rowWidth, metatileWidth, metatileHeight);
            tiledTilesetBitmap.Save(TiledTilesetBmpDestinationPath, ImageFormat.Bmp);

            // Save the tsx
            TiledUtils.SaveTSX(TSXDestinationPath,
                               TiledTilesetBitmapName,
                               TiledTilesetName,
                               tiledTilesetBitmap.Width,
                               tiledTilesetBitmap.Height,
                               metatileWidth, 
                               metatileHeight,
                               IsTilesetAnimated,
                               TilesetAnimationSpeed,
                               tiledTileset.Tiles);

            if (!IsTilesetAnimated)
            {
                // save the tmx but not for animated tilesets

                string oldFilename = Path.ChangeExtension(TilemapDestinationPath, "tmx.old");
                if (File.Exists(oldFilename))
                {
                    File.Delete(oldFilename);
                    System.IO.File.Move(TilemapDestinationPath, oldFilename);
                }

                TiledUtils.SaveTMX(TilemapDestinationPath, TiledTilesetFilename, metatileWidth, metatileHeight, tiledTileset.TileMap);
            }

            // split the blocks into a tileset
            int tileWidth = 8;
            int tileHeight = 8;

            // don't reduce tiles if it's an animated tilset
            var flipType = IsTilesetAnimated ? BitmapSplitter.ExportFlipType.None : BitmapSplitter.ExportFlipType.Genesis;

            var tileset = new BitmapSplitter(tiledTilesetBitmap, null, null, tileWidth, tileHeight, !IsTilesetAnimated, flipType);

            Bitmap tilesetBitmap = null;

            tilesetBitmap = BitmapUtils.PackTilesetBitmap(tileset.Tiles, 2, tileWidth, tileHeight);
 
            tilesetBitmap.Save(TilesetBmpDestinationPath, ImageFormat.Bmp);

            MetatilesUtils.ExportMetatilesCode(MetatilesDestinationFolder, MetatileMapFilename, tileset.TileMap, IsTilesetAnimated);

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
            BuildFilenames(args);

            for (int loop = 2; loop < args.Length; loop++)
            {
                string arg = args[loop];

                if (arg == "-y" || arg == "-o")
                    ForceOverwrite = true;

                if (arg == "-animatedtileset")
                {
                    IsTilesetAnimated = true;
                }

                if (arg == "-animatedtilesetspeed")
                    TilesetAnimationSpeed = int.Parse(arg);

                if (arg == "-tiletypesgal")
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
            }
        }

        private void BuildFilenames(string[] args)
        {
            SourcePath = args[0];
            SourceName = Path.GetFileNameWithoutExtension(SourcePath);


            if (args.Length == 1)
            {
                DestinationFolder = Path.GetDirectoryName(SourcePath);
            }
            else
            {
                string arg = args[1];

                if (arg.StartsWith("-"))
                    DestinationFolder = Path.GetDirectoryName(SourcePath);
                else
                    DestinationFolder = arg;
            }

            if (!String.IsNullOrEmpty(DestinationFolder) && !DestinationFolder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                DestinationFolder += Path.DirectorySeparatorChar;

            TilesetDestinationFolder = DestinationFolder;
            MetatilesDestinationFolder = DestinationFolder;

            TiledTilesetName = SourceName + "_tmx_tileset";
            TiledTilesetBitmapName = TiledTilesetName + ".bmp";
            TiledTilesetFilename = TiledTilesetName + ".tsx";

            MetatileMapFilename = SourceName + "_metatile_map";

            // use the same destination folder for everything right now, but
            // leave the option to set different ones.
            TSXDestinationPath = DestinationFolder + TiledTilesetFilename;
            TilemapDestinationPath = DestinationFolder + SourceName + ".tmx";
            TiledTilesetBmpDestinationPath = DestinationFolder + TiledTilesetBitmapName;
            TilesetBmpDestinationPath = DestinationFolder + SourceName + "_tileset.bmp";
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

            if (!String.IsNullOrWhiteSpace(MetatilesDestinationFolder) && !Directory.Exists(MetatilesDestinationFolder))
            {
                Directory.CreateDirectory(MetatilesDestinationFolder);
            }
        }

        // returns 1 to continue
        //         0 to stop
        private int CheckOverwrites()
        {
            if ((!IsTilesetAnimated && File.Exists(TilemapDestinationPath)) ||
                File.Exists(TSXDestinationPath) ||
                File.Exists(TilesetBmpDestinationPath) ||
                (!IsTilesetAnimated && File.Exists(TiledTilesetBmpDestinationPath + ".h") || File.Exists(TiledTilesetBmpDestinationPath + ".c")))
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