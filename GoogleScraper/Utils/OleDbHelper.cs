using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using GoogleScraper.Const;

namespace GoogleScraper.Utils
{
    public class OleDbHelper
    {
        public void CreateTableIfNotExists(string filePath, string tableName)
        {
            if (File.Exists(filePath))
                DropTable(filePath, tableName);

            if (!TableExists(filePath, tableName))
            {
                using (OleDbConnection conn = new OleDbConnection(string.Format(Config.GetOleDbConnectionString(filePath))))
                {
                    conn.Open();
                    OleDbCommand cmd = new OleDbCommand();
                    cmd.Connection = conn;


                    cmd.CommandText = SqlQueries.CREATE_TABLE;
                    cmd.ExecuteNonQuery();
                }
            }

        }

        public void InsertRow(string filePath, string websiteUrl, string root, string email, string phone, string companyName)
        {
            using (OleDbConnection conn = new OleDbConnection(string.Format(Config.GetOleDbConnectionString(filePath))))
            {
                conn.Open();
                OleDbCommand cmd = new OleDbCommand();

                cmd.Connection = conn;
                cmd.CommandText = string.Format(SqlQueries.INSERT, websiteUrl, root, email, phone, companyName);
                cmd.ExecuteNonQuery();
            }
        }

        private bool TableExists(string filePath, string tableName)
        {
            string connectionString = Config.GetOleDbConnectionString(filePath);

            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = conn;

                // Get all Sheets in Excel File
                DataTable dtSheet = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

                // Loop through all Sheets to get data
                foreach (DataRow dr in dtSheet.Rows)
                {
                    string sheetName = dr["TABLE_NAME"].ToString();

                    if (sheetName == tableName) return true;
                }

                cmd = null;
                conn.Close();
            }

            return false;
        }

        public void DropTable(string filePath, string tableName)
        {
            string connectionString = Config.GetOleDbConnectionString(filePath);

            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = conn;

                cmd.CommandText = SqlQueries.DROP_TABLE;
                cmd.ExecuteNonQuery();
            }
        }

        public List<ScrapeData> ReadFile(string filePath)
        {
            DataSet ds = new DataSet();
            string connectionString = Config.GetOleDbConnectionString(filePath);

            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = conn;

                // Get all Sheets in Excel File
                DataTable dtSheet = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

                // Loop through all Sheets to get data
                foreach (DataRow dr in dtSheet.Rows)
                {
                    string sheetName = dr["TABLE_NAME"].ToString();

                    if (!sheetName.EndsWith("$"))
                        continue;

                    cmd.CommandText = "SELECT * FROM [" + sheetName + "]";

                    DataTable dt = new DataTable();
                    dt.TableName = sheetName;

                    OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                    da.Fill(dt);

                    ds.Tables.Add(dt);
                }

                cmd = null;
                conn.Close();
            }

            var list = new List<ScrapeData>();

            foreach (DataRow row in ds.Tables[0].Rows)
            {
                if (!string.IsNullOrEmpty(row["WebsiteUrl"].ToString()))
                {
                    list.Add(new ScrapeData
                    {
                        WebsiteUrl = row["WebsiteUrl"].ToString(),
                        Root = row["Root"].ToString(),
                        Email = row["Email"].ToString(),
                        PhoneNumber = row["PhoneNumber"].ToString(),
                        CompanyName = row["CompanyName"].ToString()
                    });
                }
            }

            return list;
        }

    }
}
