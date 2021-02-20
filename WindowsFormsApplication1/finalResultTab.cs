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
using System.IO;
using System.Security.Permissions;

namespace WindowsFormsApplication1
{
    public partial class finalResultTab : Form
    {       
        public finalResultTab()
        {
            InitializeComponent();
        }

        public finalResultTab(string var1)
        {
            InitializeComponent();

            using (var webClient = new System.Net.WebClient())
            {
                string result = webClient.DownloadString("https://itunes.apple.com/lookup?id="+var1+"&entity=song&limit=200");
                //Desrializes the string to get values from JSON objects easily.            
                dynamic dynObj = JsonConvert.DeserializeObject(result);
                //Gets the number of results. So we know how many times to loop                 
                int resultCount = dynObj["resultCount"];

                for (int i = 0; i < resultCount; i++)
                {
                    if (dynObj["results"][i]["wrapperType"] == "track")
                    {
                        dataGridView1.Rows.Add();
                        int theLastRow = dataGridView1.Rows.Count - 1;
                        dataGridView1.Rows[theLastRow].Cells[0].Value = dynObj["results"][i]["trackName"];
                        dataGridView1.Rows[theLastRow].Cells[1].Value = dynObj["results"][i]["trackNumber"];
                        dataGridView1.Rows[theLastRow].Cells[2].Value = dynObj["results"][i]["primaryGenreName"];
                        dataGridView1.Rows[theLastRow].Cells[3].Value = dynObj["results"][i]["collectionName"];
                        dataGridView1.Rows[theLastRow].Cells[4].Value = dynObj["results"][i]["releaseDate"];
                        dataGridView1.Rows[theLastRow].Cells[5].Value = dynObj["results"][i]["artistName"];
                        dataGridView1.Rows[theLastRow].Cells[6].Value = dynObj["results"][i]["discCount"];
                        dataGridView1.Rows[theLastRow].Cells[7].Value = dynObj["results"][i]["discNumber"];
                        dataGridView1.Rows[theLastRow].Cells[8].Value = dynObj["results"][i]["trackCount"];
                        //getting artwork from link to picturebox 
                        string artworkLink = Convert.ToString(dynObj["results"][i]["artworkUrl100"]);
                        pictureBox1.Load(artworkLink.Replace("100x100bb.jpg", "600x600bb.jpg"));
                        pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                    }
                 }                   
                }
            }
               
       
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int theRow = dataGridView1.CurrentCell.RowIndex;
            dataGridView1.Rows[theRow].Selected = true;

        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            tagSaving();
        }

        private void finalResultTab_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                this.Owner.Enabled = true;
                this.Owner.BringToFront();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            tagSaving();
        }

        private TagLib.IPicture getCover()
        {          
            TagLib.Picture pic = new TagLib.Picture();
            pic.Type = TagLib.PictureType.FrontCover;            
            pic.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
            MemoryStream ms = new MemoryStream();
            pictureBox1.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            ms.Position = 0;
            pic.Data = TagLib.ByteVector.FromStream(ms);
            return pic;                   
        }
        
        private void tagSaving()
        {
            try
            {
                FileIOPermission perm = new FileIOPermission(FileIOPermissionAccess.AllAccess, Form1.passFile);
                string infoVar1 = dataGridView1.CurrentRow.Cells[0].Value.ToString(); //title
                string infoVar2 = dataGridView1.CurrentRow.Cells[1].Value.ToString(); //tracknumber            
                string infoVar3 = dataGridView1.CurrentRow.Cells[2].Value.ToString(); //genre
                string infoVar10 = dataGridView1.CurrentRow.Cells[3].Value.ToString(); //album
                DateTime infoVar4 = Convert.ToDateTime(dataGridView1.CurrentRow.Cells[4].Value);//year                
                string infoVar5 = dataGridView1.CurrentRow.Cells[5].Value.ToString();//artist
                string infoVar6 = dataGridView1.CurrentRow.Cells[6].Value.ToString();//diskcount
                string infoVar7 = dataGridView1.CurrentRow.Cells[7].Value.ToString();//disknumber
                string infoVar8 = dataGridView1.CurrentRow.Cells[8].Value.ToString();//trackCount               

                TagLib.File tagFile = TagLib.File.Create(Form1.passFile);
                tagFile.Tag.Title = infoVar1;
                tagFile.Tag.Album = infoVar10;
                tagFile.Tag.AlbumArtists = infoVar5.Split(',');
                tagFile.Tag.Year = Convert.ToUInt32(infoVar4.Year);
                tagFile.Tag.DiscCount = Convert.ToUInt32(infoVar6);
                tagFile.Tag.Disc = Convert.ToUInt32(infoVar7);
                tagFile.Tag.TrackCount = Convert.ToUInt32(infoVar8);
                tagFile.Tag.Track = Convert.ToUInt32(infoVar2);
                tagFile.Tag.Genres = infoVar3.Split(',');
                //checles if user wanted to save the artwork that we get from iTunes                
                if (checkBox1.Checked && tagFile.Tag.Pictures.Length == 0)
                {
                    tagFile.Tag.Pictures = new TagLib.IPicture[] { getCover() };

                }

                else if(checkBox1.Checked && tagFile.Tag.Pictures.Length != 0)
                {                    
                    tagFile.Tag.Pictures = new TagLib.IPicture[] { getCover() };                    
                }
                perm.Demand();
                tagFile.Save();

                if (tagFile.Tag.Title.Equals(infoVar1))
                {
                    if (MessageBox.Show("Tags saved successfully", "Success", MessageBoxButtons.OK) == DialogResult.OK)
                    {
                        this.Close();
                        this.Owner.Close();
                    }
                }
            }

            catch (FileNotFoundException ex)
            {
                MessageBox.Show("Can't find the file. Please check the directory", "Oops... Something went wrong");
            }
        }
       
    }
}


