﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.TextFormatting;
using GPK_RePack.Model;
using GPK_RePack.Model.Composite;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.VisualBasic.Logging;
using NLog;

namespace GPK_RePack.IO
{
    class MapperTools
    {

        private static Logger logger = LogManager.GetCurrentClassLogger();


        public static void DecryptAndWriteFile(string input, string output)
        {
            try
            {
                var data = File.ReadAllBytes(input);
                DecryptFile(data);
                File.WriteAllBytes(output, data);

            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        public static void DecryptFile(byte[] data)
        {
            try
            {
                int offset = 0;

                // Unscramble (1)
                for (offset = 0; offset < data.Length; offset += 16)
                {
                    if (offset + 16 < data.Length)
                    {
                        byte[] copy = new byte[16];
                        Array.ConstrainedCopy(data, offset, copy, 0, 16);

                        data[offset + 0] = copy[12];
                        data[offset + 1] = copy[6];
                        data[offset + 2] = copy[9];
                        data[offset + 3] = copy[4];
                        data[offset + 4] = copy[3];
                        data[offset + 5] = copy[14];
                        data[offset + 6] = copy[1];
                        data[offset + 7] = copy[10];
                        data[offset + 8] = copy[13];
                        data[offset + 9] = copy[2];
                        data[offset + 10] = copy[7];
                        data[offset + 11] = copy[15];
                        data[offset + 12] = copy[0];
                        data[offset + 13] = copy[8];
                        data[offset + 14] = copy[5];
                        data[offset + 15] = copy[11];
                    }
                }

                // Unscramble (2)
                //int divison, floor it
                if ((data.Length / 2) > 0)
                {
                    var offset1 = 1;
                    var offset2 = data.Length - 1;

                    for (int i = ((data.Length / 2) + 1) / 2; i > 0; i--)
                    {
                        var tmp = data[offset1];
                        data[offset1] = data[offset2];
                        data[offset2] = tmp;
                        offset1 += 2;
                        offset2 -= 2;
                    }

                }

                // Decrypt
                string key = "GeneratePackageMapper";

                offset = 0;
                while (offset < data.Length)
                {
                    for (int i = 0; i < key.Length; i++)
                    {
                        if (offset + i < data.Length)
                        {
                            data[offset + i] ^= Convert.ToByte(key[i]);
                        }
                    }

                    offset += key.Length;
                }


            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }


        public static void ParseMappings(string folder, GpkStore store)
        {
            logger.Debug("ParseMappings for " + folder);

            try
            {
                string pkgMapper = folder + "\\PkgMapper.dat";
                string compMapper = folder + "\\CompositePackageMapper.dat";

                if (!File.Exists(pkgMapper) || !File.Exists(compMapper))
                {
                    logger.Info("Not all .dat files found");
                }

                var pkgMapperData = File.ReadAllBytes(pkgMapper);
                DecryptFile(pkgMapperData);

                StreamReader reader = new StreamReader(new MemoryStream(pkgMapperData));
                var pkgMapperText = reader.ReadToEnd();
                reader.Close();

                var objectMapperList = new Dictionary<String, String>();
                var objectEntries = pkgMapperText.Split('|');
                foreach (var entry in objectEntries)
                {
                    var split = entry.Split(',');
                    if (split.Length < 2)
                    {
                        continue;
                    }

                    var uid = split[0];
                    var compositeUID = split[1];

                    logger.Debug("entry {0}:{1}", uid, compositeUID);
                    objectMapperList.Add(compositeUID, uid);
                }

                logger.Debug("parsing CompositePackageMapper");

                var compPkgMapper = File.ReadAllBytes(compMapper);
                DecryptFile(compPkgMapper);

                reader = new StreamReader(new MemoryStream(compPkgMapper));
                var comppPackMapperText = reader.ReadToEnd();
                reader.Close();

                store.CompositeMap = new Dictionary<string, List<CompositeMapEntry>>();
                var fileEntries = comppPackMapperText.Split('!');
                for (int i = 0; i < fileEntries.Length; i += 1)
                {
                    var baseSplit = fileEntries[i].Split('?');
                    if (baseSplit.Length < 2)
                    {
                        continue;
                    }

                    var fileName = baseSplit[0];
                    var fileContent = baseSplit[1];

                    //Composite UID, Unkown Object ID, Sub-GPK File offset, Sub-GPK File length
                    //c7a706fb_6a349a6f_1d212.Chat2_dup,c7a706fb_6a349a6f_1d212,92291307,821218,|
                    var subGPKEntries = baseSplit[1].Split('|');

                    foreach (var entry in subGPKEntries)
                    {
                        var split = entry.Split(',');
                        if (split.Length < 5)
                        {
                            continue;
                        }

                        var tmp = new CompositeMapEntry();
                        tmp.CompositeUID = split[0];
                        tmp.UnknownUID = split[1];
                        tmp.FileOffset = Convert.ToInt32(split[2]);
                        tmp.FileLength = Convert.ToInt32(split[3]);

                        //enrich
                        tmp.SubGPKName = fileName;
                        tmp.UID = objectMapperList[tmp.CompositeUID];

                        if (!store.CompositeMap.ContainsKey(fileName))
                        {
                            store.CompositeMap.Add(fileName, new List<CompositeMapEntry>());
                        }

                        store.CompositeMap[fileName].Add(tmp);

                        var unk = split[4];
                        if (unk != "")
                        {
                            logger.Warn("unk not empty!!!!!!! " + unk);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Info("Loading of mappings failed");
                logger.Error(ex);
            }

        }

    }
}
