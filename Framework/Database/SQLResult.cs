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

using System;
using System.Collections.Generic;
using System.Data;

namespace Framework.Database
{
    public class SQLResult
    {
        public int Count { get; set; }
        public int FieldCount { get; set; }
        public IDataReader DataReader;
        Dictionary<string, object> Datas;

        public T Read<T>(string columnName)
        {
            return (T)Convert.ChangeType(Datas[columnName], typeof(T));
        }

        public Dictionary<string, object> Read(params string[] columnNames)
        {
            Datas = new Dictionary<string, object>();
            object obj = null;

            while (DataReader.NextResult())
            {
                for (int i = 0; i < columnNames.Length; i++)
                {
                    int colNumber = DataReader.GetOrdinal(columnNames[i]);
                    obj = DataReader.GetValue(colNumber);

                    Datas.Add(columnNames[i], obj);
                    
                }
            }

            Count = Datas.Count / columnNames.Length;
            return Datas;
        }
    }
}
