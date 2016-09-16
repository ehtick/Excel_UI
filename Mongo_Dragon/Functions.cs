﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDna.Integration;
using MA = Mongo_Adapter;

namespace Mongo_Dragon
{
    public static class Functions
    {
        [ExcelFunction(Description = "Test function", Category = "Mongo_Dragon")]
        public static string ToMongo(string server, string database, string collection, string key, object[] objects)
        {
            MA.MongoLink link = new MA.MongoLink(server, database, collection);

            List<string> json = new List<string>();
            foreach (string obj in objects)
            {
                json.Add(obj);
            }

            link.SaveJson(json, key);

            return "ToMongo";
        }

        /*****************************************************************/

        [ExcelFunction(Description = "Test function", Category = "Mongo_Dragon")]
        public static object[] FromMongo(string server, string database, string collection, string query)
        {
            MA.MongoLink link = new MA.MongoLink(server, database, collection);
            return link.GetJson(query).ToArray();
        }

        [ExcelFunction(Description = "Test function", Category = "Dragon")]
        public static string SayMongo(string name)
        {
            return "Mongo " + name;
        }
    }
}
