using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.ComponentModel;
using System.Threading;
using System.Collections;

namespace BallanceRecordChanger {
    /// <summary>
    /// 数据库读取器
    /// </summary>
    static class DBReader {

        static byte[] byteCut(byte[] b, byte cut) {
            List<byte> list = new List<byte>();
            list.AddRange(b);
            for (int i = list.Count - 1; i >= 0; i--) {
                if (list[i] == cut)
                    list.RemoveAt(i);
            }
            byte[] lastbyte = new byte[list.Count];
            for (int i = 0; i < list.Count; i++) {
                lastbyte[i] = list[i];
            }
            return lastbyte;
        }
        static void scanstr(byte[] str, ref int Pstr, ref byte[] mem) {
            for (int c = 0; ;) {
                mem[c] = str[Pstr];
                if (mem[c] == 0)
                    break;
                c++;
                Pstr++;
            }
            Pstr++;
            mem = byteCut(mem, 0);
        }

        static void scanint(byte[] str, ref int Pstr, ref byte[] mem) {
            Array.Copy(str, Pstr, mem, 0, 4);
            Pstr += 4;
        }
        static void scanint(byte[] str, ref int Pstr, ref int num) {
            Byte[] mem;
            mem = BitConverter.GetBytes(num);
            Array.Copy(str, Pstr, mem, 0, 4);
            Pstr += 4;
            num = BitConverter.ToInt32(mem, 0);
        }
        static byte[] PreReadDB(string path) {
            FileStream fstream = new FileStream(path, FileMode.Open, FileAccess.Read);
            fstream.Seek(0, SeekOrigin.End);
            long FileLen = fstream.Length;
            fstream.Seek(0, SeekOrigin.Begin);
            Byte[] FileData = new Byte[FileLen];
            fstream.Read(FileData, 0, (int)FileLen);
            fstream.Close();
            fstream.Dispose();
            for (int i = 0; i < FileLen; i++) {
                Byte j = FileData[i];
                j = (byte)(j << 3 | j >> 5);
                FileData[i] = (byte)(-(j ^ 0xAF));
            }
            return FileData;
        }

        /// <summary>
        /// 读取数据库
        /// </summary>
        /// <param name="path">数据库路径</param>
        /// <returns>数据库</returns>
        public static Database ReadDB(string path) {
            Database temp = new Database();
            Byte[] FileData = PreReadDB(path);
            Byte[] str = new Byte[100];
            int pData = 0;
            for (int i = 0; i < 12; i++) {
                temp.HighScores[i].LevelIndex = i + 1;
                pData += 60;
                for (int j = 0; j < 10; j++) {
                    scanstr(FileData, ref pData, ref str);
                    temp.HighScores[i].Play[j].Player = Encoding.Default.GetString(str).Trim();
                    str = new Byte[100];
                }

                Byte[] score = new Byte[4];
                for (int j = 0; j < 10; j++) {
                    scanint(FileData, ref pData, ref score);
                    temp.HighScores[i].Play[j].Points = BitConverter.ToInt32(score, 0);
                }
            }
            pData += 54;
            Byte[] forbid = new Byte[4];
            for (int j = 0; j < 13; j++) {
                scanint(FileData, ref pData, ref forbid);
                temp.Settings.LevelOpened[j] = BitConverter.ToBoolean(forbid, 0);
            }
            pData += 211;

            Byte[] volume = new Byte[4];
            scanint(FileData, ref pData, ref volume);
            temp.Settings.MusicVolume = (float)BitConverter.ToSingle(volume, 0);

            Byte[] SynToScr = new Byte[4];
            scanint(FileData, ref pData, ref SynToScr);
            temp.Settings.SynchToScreen = BitConverter.ToBoolean(SynToScr, 0);


            int up = 0;
            int down = 0;
            int left = 0;
            int right = 0;
            scanint(FileData, ref pData, ref up);
            scanint(FileData, ref pData, ref down);
            scanint(FileData, ref pData, ref left);
            scanint(FileData, ref pData, ref right);

            string[] keys = new string[]{"1","2","3","4","5","6","7","8","9","0","-","=","BackSpace","Tab","Q","W","E","R","T","Y","U","I","O","P",
                                         "[","]","Ctrl","A","S","D","F","G","H","J","K","L",";","'","`","Shift","\\","Z","X","C","V","B","N","M",",",".","/",
                                         "Right Shift","Alt","Space","Num 7","Num 8","Num 9","Num -","Num 4","Num 5","Num 6","Num +","Num 1","Num 2","Num 3","Num 0","Num Del","<","Up","Down","Left","Right"};
            temp.Settings.BallKeys = new BallKeys { Up = keys[up], Down = keys[down], Left = keys[left], Right = keys[right] };

            int RotCam = 0, LiftCam = 0;
            scanint(FileData, ref pData, ref RotCam);
            scanint(FileData, ref pData, ref LiftCam);
            temp.Settings.CameraKeys = new CameraKeys { Rotate = keys[RotCam], Lift = keys[LiftCam] };

            int InvCamRot = 0;
            scanint(FileData, ref pData, ref InvCamRot);
            temp.Settings.InventCameraRotation = Convert.ToBoolean(InvCamRot);


            Byte[] RecPlayer = new Byte[100];
            scanstr(FileData, ref pData, ref RecPlayer);
            temp.Settings.RecentPlayer = Encoding.Default.GetString(RecPlayer).Trim();

            int Cloud = 0;
            scanint(FileData, ref pData, ref Cloud);
            temp.Settings.Cloud = Convert.ToBoolean(Cloud);

            for (int i = 0; i < 8; i++) {
                temp.HighScores[i + 12] = new Level();
                temp.HighScores[i + 12].LevelIndex = i + 13;
                pData += 60;
                for (int j = 0; j < 10; j++) {
                    scanstr(FileData, ref pData, ref str);
                    temp.HighScores[i + 12].Play[j].Player = Encoding.Default.GetString(str).Trim();
                    str = new Byte[100];
                }

                Byte[] score = new Byte[4];
                for (int j = 0; j < 10; j++) {
                    scanint(FileData, ref pData, ref score);
                    temp.HighScores[i + 12].Play[j].Points = BitConverter.ToInt32(score, 0);

                }
            }
            return temp;
        }



        //15,16 need be replaced
        static readonly byte[] RECORD_HEAD = new byte[] { 0x44, 0x42, 0x5F, 0x48, 0x69, 0x67, 0x68, 0x73, 0x63, 0x6F, 0x72, 0x65, 0x5F, 0x4C, 0x76,
            0x00, 0x00, 0x00,
            //data length
            0xC4, 0x00, 0x00, 0x00,

            0x02, 0x00, 0x00, 0x00,
            0x0A, 0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0xFF,
            0x50, 0x6C, 0x61, 0x79, 0x65, 0x72, 0x6E, 0x61, 0x6D, 0x65, 0x00, 0x03, 0x00, 0x00, 0x00, 0x50, 0x6F, 0x69, 0x6E, 0x74, 0x73, 0x00, 0x01, 0x00, 0x00, 0x00
        };
        static byte[] GetLevelHead(int level, int length) {
            byte[] newByte = new byte[RECORD_HEAD.Length];
            RECORD_HEAD.CopyTo(newByte, 0);
            newByte[15] = (byte)(((int)(level / 10)) + 48);
            newByte[16] = (byte)((level % 10) + 48);

            var num = BitConverter.GetBytes(length);
            newByte[18] = num[0];
            newByte[19] = num[1];
            newByte[20] = num[2];
            newByte[21] = num[3];

            return newByte;
        }

        //FREISCHALTUNG head
        static readonly byte[] FREISCHALTUNG_HEAD = new byte[] {
            0x44, 0x42, 0x5F, 0x4C, 0x65, 0x76, 0x65, 0x6C, 0x66, 0x72, 0x65, 0x69,
            0x73, 0x63, 0x68, 0x61, 0x6C, 0x74, 0x75, 0x6E, 0x67, 0x00, 0x50, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0xFF, 0xFF,
            0xFF, 0xFF, 0x46, 0x72, 0x65, 0x69, 0x67, 0x65, 0x73, 0x63, 0x68, 0x61,
            0x6C, 0x74, 0x65, 0x74, 0x3F, 0x00
        };

        //option head
        static readonly byte[] OPTION_HEAD = new byte[] {
            0x44, 0x42, 0x5F, 0x4F, 0x70, 0x74, 0x69, 0x6F, 0x6E, 0x73, 0x00, 0xF6,
            0x00, 0x00, 0x00, 0x0B, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0xFF,
            0xFF, 0xFF, 0xFF, 0x56, 0x6F, 0x6C, 0x75, 0x6D, 0x65, 0x00, 0x02, 0x00,
            0x00, 0x00, 0x53, 0x79, 0x6E, 0x63, 0x68, 0x20, 0x74, 0x6F, 0x20, 0x53,
            0x63, 0x72, 0x65, 0x65, 0x6E, 0x3F, 0x00, 0x01, 0x00, 0x00, 0x00, 0x4B,
            0x65, 0x79, 0x20, 0x46, 0x6F, 0x72, 0x77, 0x61, 0x72, 0x64, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x4B, 0x65, 0x79, 0x20, 0x42, 0x61, 0x63, 0x6B, 0x77,
            0x61, 0x72, 0x64, 0x00, 0x01, 0x00, 0x00, 0x00, 0x4B, 0x65, 0x79, 0x20,
            0x4C, 0x65, 0x66, 0x74, 0x00, 0x01, 0x00, 0x00, 0x00, 0x4B, 0x65, 0x79,
            0x20, 0x52, 0x69, 0x67, 0x68, 0x74, 0x00, 0x01, 0x00, 0x00, 0x00, 0x4B,
            0x65, 0x79, 0x20, 0x52, 0x6F, 0x74, 0x61, 0x74, 0x65, 0x20, 0x43, 0x61,
            0x6D, 0x00, 0x01, 0x00, 0x00, 0x00, 0x4B, 0x65, 0x79, 0x20, 0x4C, 0x69,
            0x66, 0x74, 0x20, 0x43, 0x61, 0x6D, 0x00, 0x01, 0x00, 0x00, 0x00, 0x49,
            0x6E, 0x76, 0x65, 0x72, 0x74, 0x20, 0x43, 0x61, 0x6D, 0x20, 0x52, 0x6F,
            0x74, 0x61, 0x74, 0x69, 0x6F, 0x6E, 0x3F, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x4C, 0x61, 0x73, 0x74, 0x50, 0x6C, 0x61, 0x79, 0x65, 0x72, 0x00, 0x03,
            0x00, 0x00, 0x00, 0x43, 0x6C, 0x6F, 0x75, 0x64, 0x4C, 0x61, 0x79, 0x65,
            0x72, 0x3F, 0x00, 0x01, 0x00, 0x00, 0x00
        };

        static void FixBoolFormat(List<byte> list) {
            list.Add(0x00);
            list.Add(0x00);
            list.Add(0x00);
        }

        static void PreWriteDB(Byte[] FileData, string path) {
            using (FileStream fstream = new FileStream(path, FileMode.Create, FileAccess.Write)) {
                var FileLen = FileData.LongLength;
                for (int i = 0; i < FileLen; i++) {
                    Byte cache = (byte)((-FileData[i]) ^ 0xAF);
                    fstream.WriteByte((byte)(cache << 5 | cache >> 3));
                    //fstream.WriteByte(FileData[i]);
                }
                fstream.Close();
            }
        }
        /// <summary>
        /// 保存数据库
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="path">保存路径</param>
        public static void SaveDB(Database db, string path) {
            List<byte> FileData = new List<byte>();

            var levelDataCache = new List<byte>();
            for (int i = 0; i < 12; i++) {
                for (int j = 0; j < 10; j++) {
                    levelDataCache.AddRange(Encoding.Default.GetBytes(db.HighScores[i].Play[j].Player));
                    levelDataCache.Add(0x00);
                }

                for (int j = 0; j < 10; j++) {
                    levelDataCache.AddRange(BitConverter.GetBytes(db.HighScores[i].Play[j].Points));
                }

                FileData.AddRange(GetLevelHead(i + 1, levelDataCache.Count + 38));
                FileData.AddRange(levelDataCache.ToArray());
                levelDataCache.Clear();
            }

            FileData.AddRange(FREISCHALTUNG_HEAD);
            for (int j = 0; j < 13; j++) {
                FileData.Add((byte)(db.Settings.LevelOpened[j] ? 0x01 : 0x00));
                FixBoolFormat(FileData);
            }

            FileData.AddRange(OPTION_HEAD);
            FileData.AddRange(BitConverter.GetBytes(db.Settings.MusicVolume));
            FileData.AddRange(BitConverter.GetBytes(db.Settings.SynchToScreen));
            FixBoolFormat(FileData);

            List<string> keys = new List<string>() {"1","2","3","4","5","6","7","8","9","0","-","=","BackSpace","Tab","Q","W","E","R","T","Y","U","I","O","P",
                                         "[","]","Ctrl","A","S","D","F","G","H","J","K","L",";","'","`","Shift","\\","Z","X","C","V","B","N","M",",",".","/",
                                         "Right Shift","Alt","Space","Num 7","Num 8","Num 9","Num -","Num 4","Num 5","Num 6","Num +","Num 1","Num 2","Num 3","Num 0","Num Del","<","Up","Down","Left","Right"};
            FileData.AddRange(BitConverter.GetBytes(keys.IndexOf(db.Settings.BallKeys.Up)));
            FileData.AddRange(BitConverter.GetBytes(keys.IndexOf(db.Settings.BallKeys.Down)));
            FileData.AddRange(BitConverter.GetBytes(keys.IndexOf(db.Settings.BallKeys.Left)));
            FileData.AddRange(BitConverter.GetBytes(keys.IndexOf(db.Settings.BallKeys.Right)));

            FileData.AddRange(BitConverter.GetBytes(keys.IndexOf(db.Settings.CameraKeys.Rotate)));
            FileData.AddRange(BitConverter.GetBytes(keys.IndexOf(db.Settings.CameraKeys.Lift)));

            FileData.AddRange(BitConverter.GetBytes(db.Settings.InventCameraRotation));
            FixBoolFormat(FileData);

            FileData.AddRange(Encoding.Default.GetBytes(db.Settings.RecentPlayer));
            FileData.Add(0x00);

            FileData.AddRange(BitConverter.GetBytes(db.Settings.Cloud));
            FixBoolFormat(FileData);

            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 10; j++) {
                    levelDataCache.AddRange(Encoding.Default.GetBytes(db.HighScores[i+12].Play[j].Player));
                    levelDataCache.Add(0x00);
                }

                for (int j = 0; j < 10; j++) {
                    levelDataCache.AddRange(BitConverter.GetBytes(db.HighScores[i+12].Play[j].Points));
                }

                FileData.AddRange(GetLevelHead(i + 13, levelDataCache.Count + 38));
                FileData.AddRange(levelDataCache.ToArray());
                levelDataCache.Clear();

            }

            PreWriteDB(FileData.ToArray(), path);

        }


    }
    /// <summary>
    /// 数据库
    /// </summary>
    class Database {
        /// <summary>
        /// 构造函数
        /// </summary>
        public Database() {
            for (int i = 0; i < 20; i++) {
                HighScores[i] = new Level();
            }
        }
        /// <summary>
        /// 关卡成绩
        /// </summary>
        public Level[] HighScores = new Level[20];
        /// <summary>
        /// 游戏设置
        /// </summary>
        public BallanceSettings Settings = new BallanceSettings();
    }
    /// <summary>
    /// 关卡成绩
    /// </summary>
    class Level {
        /// <summary>
        /// 构造函数
        /// </summary>
        public Level() {
            for (int i = 0; i < 10; i++)
                Play[i] = new Score();
        }
        /// <summary>
        /// 关卡编号
        /// </summary>
        public int LevelIndex;
        /// <summary>
        /// 成绩信息
        /// </summary>
        public Score[] Play = new Score[10];
    }
    /// <summary>
    /// 游戏设置
    /// </summary>
    class BallanceSettings {
        /// <summary>
        /// 关卡是否打开
        /// </summary>
        public bool[] LevelOpened = new bool[13];
        /// <summary>
        /// 音量
        /// </summary>
        public float MusicVolume { get; set; }
        /// <summary>
        /// 垂直同步
        /// </summary>
        public bool SynchToScreen { get; set; }
        /// <summary>
        /// 游戏控制按键
        /// </summary>
        public BallKeys BallKeys = new BallKeys();
        /// <summary>
        /// 镜头控制按键
        /// </summary>
        public CameraKeys CameraKeys = new CameraKeys();
        /// <summary>
        /// 旋转反转
        /// </summary>
        public bool InventCameraRotation { get; set; }
        /// <summary>
        /// 最近玩家
        /// </summary>
        public string RecentPlayer { get; set; }
        /// <summary>
        /// 云层
        /// </summary>
        public bool Cloud { get; set; }
    }
    /// <summary>
    /// 游戏控制按键
    /// </summary>
    class BallKeys {
        /// <summary>
        /// 上
        /// </summary>
        public string Up { get; set; }
        /// <summary>
        /// 下
        /// </summary>
        public string Down { get; set; }
        /// <summary>
        /// 左
        /// </summary>
        public string Left { get; set; }
        /// <summary>
        /// 右
        /// </summary>
        public string Right { get; set; }
    }
    /// <summary>
    /// 镜头控制按键
    /// </summary>
    class CameraKeys {
        /// <summary>
        /// 旋转
        /// </summary>
        public string Rotate { get; set; }
        /// <summary>
        /// 抬升
        /// </summary>
        public string Lift { get; set; }
    }

    class Score {
        public string Player { get; set; }
        public int Points { get; set; }
    }
}
