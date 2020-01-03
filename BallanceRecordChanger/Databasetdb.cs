using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BallanceRecordChanger.Databasetdb {

    public static class DatabasetdbWrapper {

        public static BallanceDatabase ReadDatabase(string path) {
            var fs = new TdbReader(path);
            var res = new BallanceDatabase();

            res.ImportVirtoolsArray(fs.ReadVirtoolsArray());
            fs.Close();
            return res;
        }

        public static void SaveDatabase(string path, BallanceDatabase data) {
            var fs = new TdbWriter(path);
            fs.WriteVirtoolsArray(data.ExportVirtoolsArray());
            fs.Close();
        }

    }

    #region ballance related

    public class BallanceDatabase {
        public BallanceDatabase() {
            LevelOpened = new List<bool>();
            HighscoreLists = new Dictionary<int, HighscoreList>();
            Settings = new BallanceSettings();
        }

        private static readonly List<string> keys = new List<string>() {"1","2","3","4","5","6","7","8","9","0","-","=","BackSpace","Tab","Q","W","E","R","T","Y","U","I","O","P",
                                         "[","]","Ctrl","A","S","D","F","G","H","J","K","L",";","'","`","Shift","\\","Z","X","C","V","B","N","M",",",".","/",
                                         "Right Shift","Alt","Space","Num 7","Num 8","Num 9","Num -","Num 4","Num 5","Num 6","Num +","Num 1","Num 2","Num 3","Num 0","Num Del","<","Up","Down","Left","Right"};


        public BallanceVersion Version { get; set; }

        public Dictionary<int, HighscoreList> HighscoreLists { get; set; }

        public List<bool> LevelOpened { get; set; }

        public BallanceSettings Settings { get; set; }

        private bool ConvertIntToBool(int i) => (i != 0);
        private int ConvertBoolToInt(bool i) => (i ? 1 : 0);
        private string ConvertIntToKey(int index) => keys[index];
        private int ConvertKeyToInt(string key) => keys.IndexOf(key);

        public void ImportVirtoolsArray(List<VirtoolsArray> data) {
            foreach (var item in data) {
                if (item.SheetName == "DB_Levelfreischaltung") {

                    item.ResetCellPos();
                    for (int i = 0; i < 12; i++) {
                        LevelOpened.Add(ConvertIntToBool(item.PopCell().Value_Int32));
                    }

                } else if (item.SheetName == "DB_Options") {

                    item.ResetCellPos();

                    Settings.MusicVolume = item.PopCell().Value_Float;
                    Settings.SynchToScreen = ConvertIntToBool(item.PopCell().Value_Int32);

                    Settings.BallUpKeys = ConvertIntToKey(item.PopCell().Value_Int32);
                    Settings.BallDownKeys = ConvertIntToKey(item.PopCell().Value_Int32);
                    Settings.BallLeftKeys = ConvertIntToKey(item.PopCell().Value_Int32);
                    Settings.BallRightKeys = ConvertIntToKey(item.PopCell().Value_Int32);

                    Settings.CameraRotateKeys = ConvertIntToKey(item.PopCell().Value_Int32);
                    Settings.CameraLiftKeys = ConvertIntToKey(item.PopCell().Value_Int32);

                    Settings.InventCameraRotation = ConvertIntToBool(item.PopCell().Value_Int32);
                    Settings.RecentPlayer = item.PopCell().Value_String;
                    Settings.Cloud = ConvertIntToBool(item.PopCell().Value_Int32);

                } else {

                    item.ResetCellPos();

                    var cache = new HighscoreList();
                    var levelIndex = int.Parse(item.SheetName.Substring(15, 2));

                    for (int i = 0; i < 10; i++) {
                        var cache2 = new HighscoreItem();
                        cache2.Name = item.PopCell().Value_String;
                        cache.HighscoreItems.Add(cache2);
                    }

                    for (int i = 0; i < 10; i++) {
                        cache.HighscoreItems[i].Score = item.PopCell().Value_Int32;
                    }

                    HighscoreLists.Add(levelIndex, cache);
                }
            }

            //conclusion
            if (HighscoreLists.Count == 12) Version = BallanceVersion.Origin;
            else if (HighscoreLists.Count == 20) Version = BallanceVersion.SuDu;
            else Version = BallanceVersion.Undefined;
        }

        public List<VirtoolsArray> ExportVirtoolsArray() {
            if (Version == BallanceVersion.Undefined) throw new Exception("Undefined database couldn't be converted.");

            var res = new List<VirtoolsArray>();

            //=================================write first 12 levels
            for (int i = 1; i <= 12; i++) {
                var sheets = new VirtoolsArray($"DB_Highscore_Lv{i.ToString().PadLeft(2, '0')}", 2, 10);
                var cache = HighscoreLists[i].HighscoreItems;

                //write header
                sheets.SetHeader(new VirtoolsArrayHeader() { name = "Playername", type = FieldType.String }, 0);
                sheets.SetHeader(new VirtoolsArrayHeader() { name = "Points", type = FieldType.Int32 }, 1);

                //write data
                for (int j = 0; j < 10; j++) {
                    sheets.PushCell(new VirtoolsArrayItem() { Value_String = cache[j].Name });
                }

                for (int j = 0; j < 10; j++) {
                    sheets.PushCell(new VirtoolsArrayItem() { Value_Int32 = cache[j].Score });
                }

                res.Add(sheets);
            }

            //=================================write freischaltung
            var freischaltungSheets = new VirtoolsArray("DB_Levelfreischaltung", 1, 12);
            //write header
            freischaltungSheets.SetHeader(new VirtoolsArrayHeader() { name = "Freigeschaltet?", type = FieldType.Int32 }, 0);
            //write data
            for (int i = 0; i < 12; i++) {
                freischaltungSheets.PushCell(new VirtoolsArrayItem() { Value_Int32 = ConvertBoolToInt(LevelOpened[i]) });
            }
            res.Add(freischaltungSheets);

            //=================================write option
            var settingSheets = new VirtoolsArray("DB_Options", 11, 1);
            //write header
            settingSheets.SetHeader(new VirtoolsArrayHeader() { name = "Volume", type = FieldType.Float }, 0);
            settingSheets.SetHeader(new VirtoolsArrayHeader() { name = "Synch to Screen?", type = FieldType.Int32 }, 1);
            settingSheets.SetHeader(new VirtoolsArrayHeader() { name = "Key Forward", type = FieldType.Int32 }, 2);
            settingSheets.SetHeader(new VirtoolsArrayHeader() { name = "Key Backward", type = FieldType.Int32 }, 3);
            settingSheets.SetHeader(new VirtoolsArrayHeader() { name = "Key Left", type = FieldType.Int32 }, 4);
            settingSheets.SetHeader(new VirtoolsArrayHeader() { name = "Key Right", type = FieldType.Int32 }, 5);
            settingSheets.SetHeader(new VirtoolsArrayHeader() { name = "Key Rotate Cam", type = FieldType.Int32 }, 6);
            settingSheets.SetHeader(new VirtoolsArrayHeader() { name = "Key Lift Cam", type = FieldType.Int32 }, 7);
            settingSheets.SetHeader(new VirtoolsArrayHeader() { name = "Invert Cam Rotation?", type = FieldType.Int32 }, 8);
            settingSheets.SetHeader(new VirtoolsArrayHeader() { name = "LastPlayer", type = FieldType.String }, 9);
            settingSheets.SetHeader(new VirtoolsArrayHeader() { name = "CloudLayer?", type = FieldType.Int32 }, 10);
            //write data
            settingSheets.PushCell(new VirtoolsArrayItem() { Value_Float = Settings.MusicVolume });
            settingSheets.PushCell(new VirtoolsArrayItem() { Value_Int32 = ConvertBoolToInt(Settings.SynchToScreen) });
            settingSheets.PushCell(new VirtoolsArrayItem() { Value_Int32 = ConvertKeyToInt(Settings.BallUpKeys) });
            settingSheets.PushCell(new VirtoolsArrayItem() { Value_Int32 = ConvertKeyToInt(Settings.BallDownKeys) });
            settingSheets.PushCell(new VirtoolsArrayItem() { Value_Int32 = ConvertKeyToInt(Settings.BallLeftKeys) });
            settingSheets.PushCell(new VirtoolsArrayItem() { Value_Int32 = ConvertKeyToInt(Settings.BallRightKeys) });
            settingSheets.PushCell(new VirtoolsArrayItem() { Value_Int32 = ConvertKeyToInt(Settings.CameraRotateKeys) });
            settingSheets.PushCell(new VirtoolsArrayItem() { Value_Int32 = ConvertKeyToInt(Settings.CameraLiftKeys) });
            settingSheets.PushCell(new VirtoolsArrayItem() { Value_Int32 = ConvertBoolToInt(Settings.InventCameraRotation) });
            settingSheets.PushCell(new VirtoolsArrayItem() { Value_String = Settings.RecentPlayer });
            settingSheets.PushCell(new VirtoolsArrayItem() { Value_Int32 = ConvertBoolToInt(Settings.Cloud) });
            res.Add(settingSheets);

            //=================================write 13-20 levels
            if (Version == BallanceVersion.SuDu) {
                for (int i = 13; i <= 20; i++) {
                    var sheets = new VirtoolsArray($"DB_Highscore_Lv{i.ToString().PadLeft(2, '0')}", 2, 10);
                    var cache = HighscoreLists[i].HighscoreItems;

                    //write header
                    sheets.SetHeader(new VirtoolsArrayHeader() { name = "Playername", type = FieldType.String }, 0);
                    sheets.SetHeader(new VirtoolsArrayHeader() { name = "Points", type = FieldType.Int32 }, 1);

                    //write data
                    for (int j = 0; j < 10; j++) {
                        sheets.PushCell(new VirtoolsArrayItem() { Value_String = cache[j].Name });
                    }

                    for (int j = 0; j < 10; j++) {
                        sheets.PushCell(new VirtoolsArrayItem() { Value_Int32 = cache[j].Score });
                    }

                    res.Add(sheets);
                }
            }

            return res;

        }
    }

    public class BallanceSettings {
        public float MusicVolume { get; set; }
        public bool SynchToScreen { get; set; }
        public string BallUpKeys { get; set; }
        public string BallDownKeys { get; set; }
        public string BallLeftKeys { get; set; }
        public string BallRightKeys { get; set; }
        public string CameraRotateKeys { get; set; }
        public string CameraLiftKeys { get; set; }
        public bool InventCameraRotation { get; set; }
        public string RecentPlayer { get; set; }
        public bool Cloud { get; set; }
    }

    public class HighscoreList {
        public HighscoreList() => HighscoreItems = new List<HighscoreItem>();
        public List<HighscoreItem> HighscoreItems { get; set; }
    }

    public class HighscoreItem {
        public string Name { get; set; }
        public int Score { get; set; }
    }

    public enum BallanceVersion {
        Undefined,
        Origin,
        SuDu
    }

    #endregion

    #region basic reader / writer

    public class TdbReader {
        public TdbReader(string path) {
            fstream = new FileStream(path, FileMode.Open, FileAccess.Read);
        }

        FileStream fstream;

        public List<VirtoolsArray> ReadVirtoolsArray() {
            var res = new List<VirtoolsArray>();

            try {
                while (true) {
                    //read sheet name
                    var sheetName = ReadString();
                    //skip chunk size
                    ReadInt32();
                    //read columns and rows
                    Int32 columns = ReadInt32(), rows = ReadInt32();
                    //skip unknow FF FF FF FF
                    ReadBytes(4);

                    //init sheet
                    var cache = new VirtoolsArray(sheetName, columns, rows);

                    //read header
                    for (int i = 0; i < columns; i++) {
                        var newHeader = new VirtoolsArrayHeader();
                        newHeader.name = ReadString();
                        newHeader.type = (FieldType)ReadInt32();

                        cache.SetHeader(newHeader, i);
                    }

                    //read data
                    for (int i = 0; i < columns; i++) {
                        for (int j = 0; j < rows; j++) {
                            switch (cache.GetHeader(i).type) {
                                case FieldType.Int32:
                                    cache.SetCell(new VirtoolsArrayItem() { Value_Int32 = ReadInt32() });
                                    break;
                                case FieldType.Float:
                                    cache.SetCell(new VirtoolsArrayItem() { Value_Float = ReadFloat() });
                                    break;
                                case FieldType.String:
                                    cache.SetCell(new VirtoolsArrayItem() { Value_String = ReadString() });
                                    break;
                                default:
                                    break;
                            }
                            cache.NextCell();
                        }
                    }
                    cache.ResetCellPos();

                    //add into list
                    res.Add(cache);
                }
            } catch {
                ;//end of file. skip
            }

            return res;
        }

        private byte ReadByte() {
            var cache = fstream.ReadByte();
            if (cache == -1) throw new Exception("EOF detect!");

            var j = (byte)(cache << 3 | cache >> 5);
            return (byte)(-(j ^ 0xAF));
        }
        private byte[] ReadBytes(int length) {
            byte[] buffer = new byte[length];
            for (int i = 0; i < length; i++)
                buffer[i] = ReadByte();
            return buffer;
        }
        private string ReadString() {
            List<byte> str = new List<byte>();

            byte cache = 0;
            while (true) {
                cache = ReadBytes(1)[0];
                if (cache == 0) break;
                str.Add(cache);
            }

            return Encoding.ASCII.GetString(str.ToArray());
        }
        private Int32 ReadInt32() => BitConverter.ToInt32(ReadBytes(4), 0);
        private float ReadFloat() => BitConverter.ToSingle(ReadBytes(4), 0);

        public void Close() {
            fstream.Close();
            fstream.Dispose();
        }
    }

    public class TdbWriter {
        public TdbWriter(string path) {
            fstream = new FileStream(path, FileMode.Create, FileAccess.Write);
        }

        FileStream fstream;
        List<byte> internalCache = new List<byte>();

        public void WriteVirtoolsArray(List<VirtoolsArray> sheets) {
            foreach (var item in sheets) {
                //write name
                WriteString(item.SheetName, false);

                //write columns and rows
                WriteInt32(item.ArrayColumns);
                WriteInt32(item.ArrayRows);
                //write unknow field
                WriteInt32(-1);

                //write header
                for (int i = 0; i < item.ArrayColumns; i++) {
                    WriteString(item.GetHeader(i).name);
                    WriteInt32((Int32)item.GetHeader(i).type);
                }

                item.ResetCellPos();
                //write data
                for (int i = 0; i < item.ArrayColumns; i++) {
                    for (int j = 0; j < item.ArrayRows; j++) {
                        switch (item.GetHeader(i).type) {
                            case FieldType.Int32:
                                WriteInt32(item.GetCell().Value_Int32);
                                break;
                            case FieldType.Float:
                                WriteFloat(item.GetCell().Value_Float);
                                break;
                            case FieldType.String:
                                WriteString(item.GetCell().Value_String);
                                break;
                            default:
                                break;
                        }
                        item.NextCell();
                    }
                }
                item.ResetCellPos();

                //now, write chunksize and data
                WriteInt32(internalCache.Count, false);
                FlushCache();

            }
        }

        private void FlushCache() {
            fstream.Write(internalCache.ToArray(), 0, internalCache.Count);
            internalCache.Clear();
        }
        private void WriteByte(byte bt, bool intoCache = true) {
            bt = (byte)((-bt) ^ 0xAF);
            if (intoCache) internalCache.Add((byte)(bt << 5 | bt >> 3));
            else fstream.WriteByte((byte)(bt << 5 | bt >> 3));
            //if (intoCache) internalCache.Add(bt);
            //else fstream.WriteByte(bt);
        }
        private void WriteBytes(byte[] data, bool intoCache = true) {
            for (int i = 0; i < data.Length; i++)
                WriteByte(data[i], intoCache);
        }
        private void WriteString(string str, bool intoCache = true) {
            WriteBytes(Encoding.ASCII.GetBytes(str), intoCache);
            WriteByte(0, intoCache);
        }
        private void WriteInt32(Int32 i, bool intoCache = true) => WriteBytes(BitConverter.GetBytes(i), intoCache);
        private void WriteFloat(float i, bool intoCache = true) => WriteBytes(BitConverter.GetBytes(i), intoCache);

        public void Close() {
            fstream.Close();
            fstream.Dispose();
        }
    }

    public class VirtoolsArray {
        public VirtoolsArray(string sheetName, int columns, int rows) {
            ArrayColumns = columns;
            ArrayRows = rows;
            SheetName = sheetName;

            headers = new VirtoolsArrayHeader[columns];
            items = new VirtoolsArrayItem[rows, columns];
        }

        VirtoolsArrayHeader[] headers;
        VirtoolsArrayItem[,] items;

        public string SheetName { get; private set; }
        public int ArrayColumns { get; private set; }
        public int ArrayRows { get; private set; }

        int currentColumns = 0;
        int currentRows = 0;

        public VirtoolsArrayHeader GetHeader(int index) => headers[index];
        public void SetHeader(VirtoolsArrayHeader data, int index) => headers[index] = data;


        public void ResetCellPos() {
            currentRows = 0;
            currentColumns = 0;
        }
        public void NextCell() {
            currentRows++;
            if (currentRows == ArrayRows) {
                currentColumns++;
                currentRows = 0;
                if (currentColumns == ArrayColumns)
                    currentColumns = 0;
            }
        }
        public void SetCell(VirtoolsArrayItem data) {
            items[currentRows, currentColumns] = data;
        }
        public void PushCell(VirtoolsArrayItem data) {
            SetCell(data);
            NextCell();
        }
        public VirtoolsArrayItem GetCell() {
            return items[currentRows, currentColumns];
        }
        public VirtoolsArrayItem PopCell() {
            var cache = GetCell();
            NextCell();
            return cache;
        }

    }

    public struct VirtoolsArrayItem {
        public Int32 Value_Int32;
        public float Value_Float;
        public string Value_String;
    }

    public struct VirtoolsArrayHeader {
        public FieldType type;
        public string name;
    }

    public enum FieldType : int {
        Int32 = 1,
        Float = 2,
        String = 3
    }

    #endregion

}
