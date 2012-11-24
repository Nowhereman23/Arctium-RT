/*
 * Copyright (C) 2012 Arctium <http://>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Logging;
using System.Globalization;
using System.Text;
using System.Threading;
using Community.CsharpSqlite.SQLiteClient;
using System.Threading.Tasks;
using Windows.Storage;
using System;
using System.IO;
using System.Data;

namespace Framework.Database
{
    public class SQLiteBase
    {
        SqliteConnection Connection;
        IDataReader SqlData;

        public int RowCount { get; set; }

        public void Init(string database)
        {
            var db = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, database);

            try
            {
                Connection = new SqliteConnection(String.Format("Version=3,uri=file:{0}", db));
                Connection.Open();

                Log.Message(LogType.NORMAL, "Successfully connected to {0}", database);
            }
            catch (Exception ex)
            {
                Log.Message(LogType.ERROR, "{0}", ex.Message);

                // Try auto reconnect on error (every 5 seconds)
                Log.Message(LogType.NORMAL, "Try reconnect in 5 seconds...");
                Task.Delay(5000);

                Init(database);
            }
        }

        public bool Execute(string sql, params object[] args)
        {
            StringBuilder sqlString = new StringBuilder();
            // Fix for floating point problems on some languages
            sqlString.AppendFormat(new CultureInfo("en-US").NumberFormat, sql, args);

            IDbCommand sqlCommand = Connection.CreateCommand();
            sqlCommand.CommandText = sqlString.ToString();

            try
            {
                if (Connection.State == ConnectionState.Closed)
                    Connection.Open();

                sqlCommand.ExecuteNonQuery();

                Connection.Close();
                return true;
            }
            catch (Exception ex)
            {
                Log.Message(LogType.ERROR, "{0}", ex.Message);
                return false;
            }
        }

        public SQLResult Select(string sql, params object[] args)
        {
            SQLResult retData = new SQLResult();
            StringBuilder sqlString = new StringBuilder();
            // Fix for floating point problems on some languages
            sqlString.AppendFormat(new CultureInfo("en-US").NumberFormat, sql, args);

            IDbCommand sqlCommand = Connection.CreateCommand();
            sqlCommand.CommandText = sqlString.ToString();
            
            try
            {
                if (Connection.State == ConnectionState.Closed)
                    Connection.Open();

                SqlData = sqlCommand.ExecuteReader();
                retData.DataReader = SqlData;
                retData.FieldCount = SqlData.FieldCount;

                Connection.Close();
            }
            catch (Exception ex)
            {
                Log.Message(LogType.ERROR, "{0}", ex.Message);
            }

            return retData;
        }
    }
}
