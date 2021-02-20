using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Net;

namespace WindowsFormsApplication1
{
    public partial class briefResult : Form
    {                    
        public briefResult()
        {
            InitializeComponent();
        }

        public briefResult(string key)
        {
            InitializeComponent();

           try
            {
                //Downloads JSON data from iTunes by given artist name from previous Form and puts to string variable
                using (var webClient = new System.Net.WebClient())
                {
                    string result = webClient.DownloadString("https://itunes.apple.com/search?term=" + key + "&media=music&entity=album&limit=70&explicit=no&country=us");
                    //Desrializes the string to get values from JSON objects easily.            
                    dynamic dynObj = JsonConvert.DeserializeObject(result);
                    //Gets the number of results. So we know how many times to loop                 
                    int resultCount = dynObj["resultCount"];

                    //Creates one list. Then we loop through JSON to get all albumID after that 
                    //removes duplicates show results, converts to string array.
                    List<string> albumID = new List<string>();
                    for (int i = 0; i < resultCount; i++)
                    {
                        albumID.Add(Convert.ToString(dynObj["results"][i]["collectionId"]));
                    }
                    albumID = albumID.Distinct().ToList();


                    //Since resultAlbNames have no duplicates we get all album names, and other infos
                    //to fill DataGridView cells 
                    for (int k = 0; k < albumID.Count; k++)
                    {
                        if (dynObj["results"][k]["collectionExplicitness"] != "cleaned")
                        {
                            dataGridView1.Rows.Add();
                            int m = dataGridView1.RowCount - 1;
                            dataGridView1.Rows[m].Cells[0].Value = dynObj["results"][k]["artistName"];
                            dataGridView1.Rows[m].Cells[1].Value = dynObj["results"][k]["collectionName"];
                            dataGridView1.Rows[m].Cells[2].Value = dynObj["results"][k]["trackCount"];
                            dataGridView1.Rows[m].Cells[3].Value = Convert.ToDateTime(dynObj["results"][k]["releaseDate"]).Year;
                            dataGridView1.Rows[m].Cells[4].Value = dynObj["results"][k]["collectionId"];
                        }
                        else
                            continue;
                    }
                }
            }
            catch(WebException ex)
            {
                MessageBox.Show("Can't retrieve info from iTunes server. Please make sure you are connected to the Internet.","Oops... Something went wrong");
            }
        }
            
        //Higlights entire row when one of cells are clicked
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Selected = true;
        }
        //When user choses right album he/she have to double click to get
        //more infos about that album, which opens the finalResultTab.cs
        private void dataGridView1_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            string currentAlbID = dataGridView1.CurrentRow.Cells[4].Value.ToString(); 
            finalResultTab form4 = new finalResultTab(currentAlbID);
            form4.ShowDialog(this);            
        }

        private void briefResult_FormClosed(object sender, FormClosedEventArgs e)
        {
            if(e.CloseReason == CloseReason.UserClosing)
            {
                Form1 frm = (Form1)this.Owner;
                frm.refreshTable();
            }
        }
    }
}
