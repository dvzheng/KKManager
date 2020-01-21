﻿using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityPlugin;

namespace KKManager.SB3UGS
{
    public static class SB3UGS_Utils
    {
        public static bool FileIsAssetBundle(FileInfo file)
        {
            if (string.Equals(Path.GetExtension(file.Extension), ".unity3d", StringComparison.OrdinalIgnoreCase))
                return true;

            if (file.Length < 10) return false;

            var buffer = new byte[7];
            using (var fs = file.OpenRead())
            {
                fs.Read(buffer, 0, buffer.Length);
                fs.Close();
            }
            return Encoding.UTF8.GetString(buffer, 0, buffer.Length) == "UnityFS";
        }

        public static int HashFileContents(string unity3DFilePath)
        {
            SB3UGS_Initializer.ThrowIfNotAvailable();
            // Avoid loading the sb3ugs types before above has a chance to init them
            return HashFileContentsImpl(unity3DFilePath);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int HashFileContentsImpl(string unity3DFilePath)
        {
            using (var parser = Plugins.OpenUnity3d(unity3DFilePath))
            using (var editor = new Unity3dEditor(parser, false))
            {
                var sbResult = new StringBuilder();
                var names = editor.GetAssetNames(false);
                for (var i = 0; i < names.Length; i++)
                {
                    var asset = editor.Parser.Cabinet.Components[i];
                    sbResult.AppendFormat(CultureInfo.InvariantCulture, "{0}\0{1}\0{2}\0{3}\n", asset.pathID, asset.classID1, asset.classID2, names[i]);
                    //Console.WriteLine("PathID=" + asset.pathID.ToString("D") + " id1=" + (int)asset.classID1 + "/" + asset.classID1 + " id2=" + asset.classID2 + " " + names[i]);
                    if (asset is NotLoaded notLoaded)
                        sbResult.AppendFormat(CultureInfo.InvariantCulture, "{0}\0{1}\n", notLoaded.offset, notLoaded.size);
                }

                return GetStableHashCode(sbResult);
            }
        }

        private static int GetStableHashCode(StringBuilder str)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2)
                {
                    var c = str[i];
                    if (c == 0) c = (char)i;
                    hash1 = ((hash1 << 5) + hash1) ^ c;

                    if (i == str.Length - 1)
                        break;

                    c = str[i + 1];
                    if (c == 0) c = (char)i;
                    hash2 = ((hash2 << 5) + hash2) ^ c;
                }

                return hash1 + hash2 * 1566083941;
            }
        }
    }
}