using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BallanceRecordChanger {
    class Program {
        static void Main(string[] args) {

            Console.WriteLine("Ballance Record Changer");
            Console.WriteLine("Please put your Database.tdb in this app's folder. Then press any key to read it.");
            Console.ReadKey();

            var data = Databasetdb.DatabasetdbWrapper.ReadDatabase("Database.tdb");
            using (StreamWriter fs = new StreamWriter("DecodeData.json", false, Encoding.UTF8)) {
                fs.Write(Newtonsoft.Json.JsonConvert.SerializeObject(data));
                fs.Close();
            }

            Console.WriteLine("Database file has been decoded into Database.json. Please modify it. Then press any key. This app will encode your modified file into new Database.tdb.");
            Console.ReadKey();

            string oriData;
            using (StreamReader fs = new StreamReader("DecodeData.json", Encoding.UTF8)) {
                oriData = fs.ReadToEnd();
                fs.Close();
            }

            var dataR = Newtonsoft.Json.JsonConvert.DeserializeObject<Databasetdb.BallanceDatabase>(oriData);

            Databasetdb.DatabasetdbWrapper.SaveDatabase("Database.new.tdb", dataR);

            Console.WriteLine("New Database.tdb file has been written into Database.new.tdb. Press any key to quit app.");
            Console.ReadKey();

        }
    }
}
