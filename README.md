# gal2tmx
A tool that converts Graphics Gale file to a Tiled TMX map

The Graphics Gale image will be converted into these files:

* A TMX file that can be loaded in Tiled
* Its corresponding TSX tileset using 16x16 tiles
* The bitmap used by the TSX tileset using 16x16 tiles
* A C language source and header file containing the tileset which includes the 8x8 tiles and its metatiles map

For example, converting "background.gal" will give these files:

* background_tileset_tsx.bmp // bitmap for the Tiled tileset 
* background_tileset.h // the header file for the tileset
* background_tileset.c // the source file for the tileset
* background_tileset.tsx // the Tiled map tileset in 16x16 tiles
* background.tmx // the Tiled map

The Tiled tileset are 16x16 while the actual game tiles are 8x8. The reason they're different is that the 16x16 tiles makes it easier to build maps in Tiled while the 8x8 tiles are meant to be used on consoles. The metatile look up table is used at runtime to tell how a 16x16 tile is built up of 8x8 tiles.

# Assumptions
* the image's width and height are a multiple of 16
* the image is 16 colors (4 bits per pixel)

# Command Line

    gal2tmx [source_gal_file] [destination folder] [-y or -o]

  The tool checks for overwrites and prompts the user. Pass -y or -o to the command line to suppress the prompt. 