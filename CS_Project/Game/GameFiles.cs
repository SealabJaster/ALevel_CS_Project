﻿using System;
using System.IO;

namespace CS_Project.Game
{
    /// <summary>
    /// A static class containing functions related to storing/reading files associated with the game.
    /// </summary>
    public static class GameFiles
    {
        // Data paths
        private const string _dataFolder      = "data/";                        // Path to where all the data is stored.
        private const string _treeFolder      = _dataFolder + "trees/";         // Path to where the trees are stored.
        private const string _helpFileFlag    = _dataFolder + "help_was_shown"; // Path to where the "show help" file is stored.

        // File format info (For .tree files)
        public const byte    treeFileVersion  = 2;      // This is the version that the class supports. It can read older versions, but not newer
        private const string _treeFileHeader  = "TREE"; // The "magic number" for a .tree file.

        /// <summary>
        /// Combines `path` with the path to the tree folder.
        /// </summary>
        private static string makeTreePath(string path)
        {
            var finalPath = Path.Combine(GameFiles._treeFolder, path + ".tree");

            return finalPath;
        }

        /// <summary>
        /// Makes sure the data folder, and the tree folder exist.
        /// </summary>
        private static void ensureDirectories()
        {
            // The directories to check for.
            var directories = new string[] { GameFiles._dataFolder, GameFiles._treeFolder };

            foreach(var dir in directories)
            {
                if(!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
        }

        /// <summary>
        /// Determines whether a tree with a specific name exists.
        /// </summary>
        /// 
        /// <param name="name">The name of the tree to search for.</param>
        /// 
        /// <returns>True if the tree exists. False otherwise.</returns>
        public static bool treeExists(string name)
        {
            return File.Exists(GameFiles.makeTreePath(name));
        }

        /// <summary>
        /// Removes a tree with a specific name.
        /// </summary>
        /// 
        /// <exception cref="System.IO.FileNotFoundException">Thrown if `shouldThrow` is true, and a tree called `name` does not exist.</exception>
        /// 
        /// <param name="name">The name of the tree to remove</param>
        /// <param name="shouldThrow">If True, then this function will throw a FileError if the tree does not exist.</param>
        public static void removeTree(string name, bool shouldThrow = false)
        {
            if(shouldThrow && !GameFiles.treeExists(name))
                throw new FileNotFoundException("Could not remove tree as it does not exist.", GameFiles.makeTreePath(name));

            File.Delete(GameFiles.makeTreePath(name));
        }

        /// <summary>
        /// Saves a tree to a file.
        /// </summary>
        /// 
        /// <exception cref="System.ArgumentNullException">Thrown if `root` is null.</exception>
        /// <exception cref="System.IO.IOException">Thrown if `overwrite` is false, and a tree called `name` already exists.</exception>
        /// 
        /// <param name="name">The name to give the tree.</param>
        /// <param name="root">The root node of the tree.</param>
        /// <param name="overwrite">
        ///     If True, then if a tree called 'name' already exists, it is overwritten.
        ///     If False, then an IOException is thrown if a tree called 'name' already exists.
        /// </param>
        public static void saveTree(string name, Node root, bool overwrite = true)
        {
            if(root == null)
                throw new ArgumentNullException("root");

            if(!overwrite && GameFiles.treeExists(name))
                throw new IOException($"Unable to save tree {name} as it already exists, and overwrite is set to false.");

            // Make sure the tree directory exists, then serialise `root` into a new tree file.
            GameFiles.ensureDirectories();
            using (var fs = new FileStream(GameFiles.makeTreePath(name), FileMode.Create))
            {
                using (var bw = new BinaryWriter(fs))
                {
                    /*
                     * Format:
                     *  [4 bytes, 'TREE']
                     *  [1 byte, file version]
                     *  [X bytes, serialised tree]
                     * */
                    bw.Write((char[])GameFiles._treeFileHeader.ToCharArray());
                    bw.Write((byte)GameFiles.treeFileVersion);
                    root.serialise(bw);
                }
            }
        }

        /// <summary>
        /// Loads a previously saved tree.
        /// </summary>
        /// 
        /// <exception cref="System.IO.FileNotFoundException">Thrown if a tree called `name` doesn't exist.</exception>
        /// <exception cref="System.IO.IOException">Thrown if the tree file is malformed in some way, and cannot be loaded.</exception>
        /// 
        /// <param name="name">The name of the tree to load.</param>
        /// <param name="shouldThrow">If True, then an exception is thrown if 'name' doesn't exist.</param>
        /// 
        /// <returns>
        ///     The root node of the tree saved as 'name'.
        ///     Null is returned if 'shouldThrow' is False, and 'name' doesn't exist.
        /// </returns>
        public static Node loadTree(string name, bool shouldThrow = true)
        {
            try
            {
                if(!GameFiles.treeExists(name))
                    throw new FileNotFoundException($"Unable to load tree {name} as it does not exist.", GameFiles.makeTreePath(name));

                using (var fs = new FileStream(GameFiles.makeTreePath(name), FileMode.Open))
                {
                    using (var br = new BinaryReader(fs))
                    {
                        // Make sure the header is correct
                        string header = new string(br.ReadChars(GameFiles._treeFileHeader.Length));
                        if(header != GameFiles._treeFileHeader)
                            throw new IOException($"Unable to load tree {name} as it's header is incorrect: '{header}'");

                        // Then read in the version number, and make sure we can read it in.
                        byte version = br.ReadByte();
                        if(version > GameFiles.treeFileVersion)
                            throw new IOException($"Unable to load tree {name} as it uses a newer version of the TREE format.\n"
                                                + $"File version: {version} | Highest Supported version: {GameFiles.treeFileVersion}");

                        // Then unserialise the tree.
                        var root = Node.root;
                        root.deserialise(br, version);

                        return root;
                    }
                }
            }
            catch(Exception ex)
            {
                if(shouldThrow)
                    throw ex;

                return null;
            }
        }

        /// <summary>
        /// This function determines if the game should display the help message box.
        /// 
        /// If this function returns `true`, a file is created in the 'data' folder which will then
        /// make this function always return `false` while the file exists.
        /// </summary>
        /// <returns>`true` if the help box should be displayed. `false` otherwise.</returns>
        public static bool shouldShowHelpMessage()
        {
            if(!File.Exists(GameFiles._helpFileFlag))
            {
                File.Create(GameFiles._helpFileFlag).Dispose();
                return true;
            }

            return false;
        }
    }
}
