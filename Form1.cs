using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using System.Data.SQLite;
using System.Globalization;

namespace NureState
{
    public partial class Form1 : Form
    {
        private String connString = "Data Source=nure.db";
        private String nureUrl = "https://dec.nure.ua/uk-about-department/uk-teaching-staff/";
        public Form1()
        {
            InitializeComponent();
            CreateTable();
            var dates = PullFetchedDates();
            foreach (var d in dates)
            {
                comboBox1.Items.Add(d.ToString("yyyy-MM-dd HH:mm:ss"));
                comboBox2.Items.Add(d.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        private Dictionary<String, String> ParseFile(String filename)
        {
            Dictionary<String, String> results = new Dictionary<String, String>();
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.Load(filename, Encoding.UTF8);
            if (htmlDoc.ParseErrors != null && htmlDoc.ParseErrors.Count() == 0)
            {
                foreach (HtmlNode node in htmlDoc.DocumentNode.SelectNodes("//td[@class]"))
                {
                    var nameNode = node.SelectSingleNode("strong");
                    var positionNode = node.SelectSingleNode("em");
                    if (nameNode == null || positionNode == null)
                        continue;
                    String name = nameNode.InnerText.
                        Replace("'", "''").
                        Replace("\n", " ");
                    String position =positionNode.InnerText.
                        Replace("\n", " "); 
                    results.Add(name, position);
                }
            }

            return results;
        }

        private (String, DateTime) FetchUrl(String url)
        {
            DateTime dt = DateTime.Now;
            String filename = $".\\{dt.ToString("yyyy-MM-dd HH-mm-ss")}.html";
            using(WebClient client = new WebClient()) {
                client.DownloadFile(url, filename);
            }

            return (filename, dt);
        }

        private void CreateTable()
        {
            using (var connection = new SQLiteConnection(connString))
            {
                connection.Open();
 
                SQLiteCommand command = new SQLiteCommand();
                command.Connection = connection;
                command.CommandText = $"CREATE TABLE IF NOT EXISTS NureState(" +
                                      $"Name TEXT NOT NULL," +
                                      $"Position TEXT NOT NULL," +
                                      $"Fetched DATETIME NOT NULL)";
                command.ExecuteNonQuery();
            }
        }

        private List<DateTime> PullFetchedDates()
        {
            List<DateTime> dates = new List<DateTime>();
            using (SQLiteConnection connect = new SQLiteConnection(connString)){
                connect.Open();
                using (SQLiteCommand cmd = connect.CreateCommand()){
                    cmd.CommandText = @"SELECT DISTINCT Fetched FROM NureState";
                    cmd.CommandType = CommandType.Text;
                    SQLiteDataReader r = cmd.ExecuteReader();
                    while (r.Read()){
                        dates.Add(DateTime.Parse(r["Fetched"].ToString()));
                    }
                }
            }

            return dates;
        }
        
        private Dictionary<String, String> PullData(DateTime dt)
        {
            Dictionary<String, String> results = new Dictionary<String, String>();
            using (SQLiteConnection connect = new SQLiteConnection(connString)){
                connect.Open();
                using (SQLiteCommand cmd = connect.CreateCommand()){
                    cmd.CommandText = $"SELECT Name, Position " +
                                      $"FROM NureState " +
                                      $"WHERE Fetched = '{dt.ToString("yyyy-MM-dd HH:mm:ss")}'";
                    cmd.CommandType = CommandType.Text;
                    SQLiteDataReader r = cmd.ExecuteReader();
                    while (r.Read()){
                        results.Add(Convert.ToString(r["Name"]),Convert.ToString(r["Position"]));
                    }
                }
            }

            return results;
        }

        private void PushData(Dictionary<String, String> data, DateTime dt)
        {
            using (var connection = new SQLiteConnection("Data Source=nure.db"))
            {
                connection.Open();
                foreach (KeyValuePair<String, String> d in data)
                {
                    SQLiteCommand command = new SQLiteCommand();
                    command.Connection = connection;
                    command.CommandText = $"INSERT INTO NureState (Name, Position, Fetched)" +
                                          $"VALUES ('{d.Key}', '{d.Value}', '{dt.ToString("yyyy-MM-dd HH:mm:ss")}')";
                    command.ExecuteNonQuery();
                }
            }
        }

        private void btnFetch_Click(object sender, EventArgs e)
        {
            var tuple = FetchUrl(nureUrl);
            var results = ParseFile(tuple.Item1);
            PushData(results, tuple.Item2);
            comboBox1.Items.Add(tuple.Item2.ToString("yyyy-MM-dd HH:mm:ss"));
            comboBox2.Items.Add(tuple.Item2.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = ".\\";
                openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    String filename = openFileDialog.FileName;
                    var data = ParseFile(filename);
                    String date = filename.Split('\\').Last().Split('.').First();
                    DateTime dt = DateTime.ParseExact(date, "yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture);
                    PushData(data, dt);
                    comboBox1.Items.Add(dt.ToString("yyyy-MM-dd HH:mm:ss"));
                    comboBox2.Items.Add(dt.ToString("yyyy-MM-dd HH:mm:ss"));
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void dataGrid_Navigate(object sender, NavigateEventArgs ne)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null || comboBox2.SelectedItem == null)
                return;

            var results1 = PullData(DateTime.ParseExact(
                comboBox1.SelectedItem.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            var results2 = PullData(DateTime.ParseExact(
                comboBox2.SelectedItem.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            dataGridView.Rows.Clear();
            foreach (var pair in results1)
            {
                DataGridViewRow row = new DataGridViewRow();
                row.Cells.Add(new DataGridViewTextBoxCell());
                row.Cells.Add(new DataGridViewTextBoxCell());
                row.Cells[0].Value = pair.Key;
                row.Cells[1].Value = pair.Value;
                if (results2.ContainsKey(pair.Key))
                {
                    if (results2[pair.Key] != pair.Value)
                    {
                        row.Cells[1].Style.BackColor = Color.Blue;
                    }
                }
                else
                    row.DefaultCellStyle.BackColor = Color.Green;
                
                dataGridView.Rows.Add(row);
            }

            foreach (var pair in results2)
            {
                if (results1.ContainsKey(pair.Key))
                    continue;
                
                DataGridViewRow row = new DataGridViewRow();
                row.Cells.Add(new DataGridViewTextBoxCell());
                row.Cells.Add(new DataGridViewTextBoxCell());
                row.Cells[0].Value  = pair.Key;
                row.Cells[1].Value = pair.Value;
                row.DefaultCellStyle.BackColor = Color.Red;
                dataGridView.Rows.Add(row);
            }
        }
    }
}