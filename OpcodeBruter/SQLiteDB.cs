using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace OpcodeBruter
{
    public class SQLiteDB
    {
        protected SQLiteConnection dbConnection;

        public SQLiteDB(string dbPath)
        {
            dbConnection = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True;", dbPath));
        }

        public bool openConnection()
        {
            try
            {
                dbConnection.Open();
                return true;
            }
            catch (SQLiteException /*e*/)
            {
                return false;
            }
        }

        public void closeConnection()
        {
            dbConnection.Close();
        }
    }

    public class BinDiff : SQLiteDB
    {
        public BinDiff(string dbPath) : base(dbPath) { }

        public Int32 getOldFunction(uint NewFunctionAddr)
        {
            string sql = string.Format("SELECT * FROM function WHERE address1={0}", NewFunctionAddr.ToString());
            SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
                return reader.GetInt32(2);
            else
                return 0;
        }

        public double getCertianty(uint NewFunctionAddr)
        {
            string sql = string.Format("SELECT * FROM function WHERE address1={0}", NewFunctionAddr.ToString());
            SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
                return reader.GetDouble(4);
            else
                return 0.0f;
        }
    }

    public class OpcodeTable : SQLiteDB
    {
        public OpcodeTable(string dbPath) : base(dbPath) { }

        public string getSMSGNameFromHandler(Int32 FuncAddr)
        {
            string sql = string.Format("SELECT * FROM SMSG WHERE CallHandler='0x{0}'", FuncAddr.ToString("X8"));
            SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
                return reader.GetString(5);
            else
                return string.Empty;
        }

        public string getCMSGNameFromCtor(Int32 FuncAddr)
        {
            string sql = string.Format("SELECT * FROM CMSG WHERE Ctor='0x{0}'", FuncAddr.ToString("X8"));
            SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
                return reader.GetString(5);
            else
                return string.Empty;
        }
    }
}


