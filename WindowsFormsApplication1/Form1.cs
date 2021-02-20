using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TagLib;
using System.IO;
using System.Security.Permissions;


namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
           this.MaximumSize = new System.Drawing.Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
          

        }

        List<string> files = new List<string>();
        public static string passFile;
        TagLib.File tagFile;
        
        //Fills dataGridView by opened file/(s) infos
        private void fillTableFields(string filename)
        {
            TagLib.File tempFile = TagLib.File.Create(filename);  // track is the name of the mp3   
            string thefile = Path.GetFileName(filename);
            string title = tempFile.Tag.Title;
            string album = tempFile.Tag.Album;
            string lyrics = tempFile.Tag.Lyrics;
            uint year = tempFile.Tag.Year;
            string artists = String.Join(",", tempFile.Tag.AlbumArtists);
            string genre = String.Join(",", tempFile.Tag.Genres);
            uint track = tempFile.Tag.Track;
            uint disk = tempFile.Tag.Disc;
            string composer = String.Join(",", tempFile.Tag.Composers);
            
            string[] theInfo = new string[] { filename, thefile, title, artists, album, track.ToString(), year.ToString(), genre, disk.ToString(), lyrics, composer };
            dataGridView1.Rows.Add(theInfo);
        }

        //Fills textboxs by infos that currently selected row on dataGridView
        private void fillFields(string filename)
        {
            try
            {
                tagFile = TagLib.File.Create(filename);  // track is the filepath of the mp3    

                //if file have artowrk it will be displayed on picturebox    
                if (tagFile.Tag.Pictures.Length != 0)
                {
                    pictureBox1.Image = Image.FromStream(new MemoryStream(tagFile.Tag.Pictures[0].Data.Data));
                    pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                }
                else
                {
                    pictureBox1.Image = null;
                }

                yearField.Text = tagFile.Tag.Year.ToString();
                TitleField.Text = tagFile.Tag.Title;
                AlbumField.Text = tagFile.Tag.Album;
                AlbumAritstField.Text = String.Join(",", tagFile.Tag.AlbumArtists);
                genreField.Text = String.Join(",", tagFile.Tag.Genres);
                ComposerField.Text = String.Join(",", tagFile.Tag.Composers);
                diskField.Text = tagFile.Tag.Disc.ToString();
                trackField.Text = tagFile.Tag.Track.ToString();
                trackCountField.Text = tagFile.Tag.TrackCount.ToString();
                diskCountField.Text = tagFile.Tag.DiscCount.ToString();
                richTextBox1.Text = tagFile.Tag.Lyrics;
            }  
            catch(FileNotFoundException ex)
            {
                MessageBox.Show("File "+Path.GetFileName(filename)+" not found");
            }    
        }
       
        //Gets infos in the textboxs and saves to mp3
        private void saveButton_Click(object sender, EventArgs e)
        {            
           try
            {
                FileIOPermission perm = new FileIOPermission(FileIOPermissionAccess.AllAccess, tagFile.Name);
                // TagLib.File tagFile = TagLib.File.Create(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value.ToString());
                tagFile.Tag.Year = Convert.ToUInt32(yearField.Text);
                tagFile.Tag.Title = TitleField.Text;
                tagFile.Tag.Album = AlbumField.Text;
                tagFile.Tag.AlbumArtists = AlbumAritstField.Text.Split(',');
                tagFile.Tag.Track = Convert.ToUInt32(trackField.Text);
                tagFile.Tag.TrackCount = Convert.ToUInt32(trackCountField.Text);
                tagFile.Tag.Disc = Convert.ToUInt32(diskField.Text);
                tagFile.Tag.DiscCount = Convert.ToUInt32(diskCountField.Text);
                tagFile.Tag.Genres = genreField.Text.Split(',');
                tagFile.Tag.Composers = ComposerField.Text.Split(',');
                tagFile.Tag.Lyrics = richTextBox1.Text;
                if (pictureBox1.Image != null)
                {
                    picToFile();
                }
                perm.Demand();
                tagFile.Save();
                if (tagFile.Tag.Title.Equals(TitleField.Text))
                {
                    MessageBox.Show("Tags saved successfully", "Success", MessageBoxButtons.OK);
                    refreshTable();
                }
            }

            catch(NullReferenceException ex)
            {
                MessageBox.Show("There is nothing to save. Please add some files first", "Oops... Something went wrong");
            }
        }

        //checkes the iTunes database. Sets the owner form of all children forms
        private void button1_Click(object sender, EventArgs e)
        {
           try
            {
                keywordInputForm form2 = new keywordInputForm(AlbumAritstField.Text);
                passFile = dataGridView1.CurrentRow.Cells[0].Value.ToString();
                form2.ShowDialog(this);
            }
            catch(NullReferenceException ex)
            {
                MessageBox.Show("There is nothing to tag. Please add some files first", "Oops... Something went wrong");
            }
        }

        private void clearTagButton_Click(object sender, EventArgs e)
        {
            try
            {
                //  TagLib.File tagFile = TagLib.File.Create(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value.ToString());
                tagFile.Tag.Clear();
                tagFile.Save();
                if (String.IsNullOrEmpty(tagFile.Tag.Title))
                {
                    MessageBox.Show("Tags are cleared for this file", "Tag Clearing Succeeded", MessageBoxButtons.OK);
                    refreshTable();

                }
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show("There is nothing to clear. Please add some files first", "Oops... Something went wrong");
            }
        }
              
        //If cell clicked the textboxes will be filled by the file info of selected cell
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {            
            dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Selected = true;
            fillFields(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value.ToString());
        }


        private void button3_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure to delete this file?", "File Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                System.IO.File.Delete(dataGridView1.CurrentRow.Cells[0].Value.ToString());
                foreach (var deleted in files)
                {
                    files.RemoveAt(files.IndexOf(dataGridView1.CurrentRow.Cells[0].Value.ToString()));
                    dataGridView1.Rows.RemoveAt(dataGridView1.CurrentRow.Index);
                    break;
                }
                refreshTable();
            }
            
        }

        internal void refreshTable()
        {
            dataGridView1.Rows.Clear();
            foreach (string t in files)
            {                
                fillTableFields(t);
            }
                foreach(Control tb in panel1.Controls)
                {
                    if(tb is System.Windows.Forms.TextBox)
                    {
                        tb.Text = "";
                    }
                }            
            richTextBox1.Text = null;
            pictureBox1.Image = null;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Right)
            {                
                mymenu.Show(Cursor.Position);
            }
        }

        private void picToFile()
        {
            TagLib.Picture pic = new TagLib.Picture();
            pic.Type = TagLib.PictureType.FrontCover;
            pic.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
            MemoryStream ms = new MemoryStream();
            pictureBox1.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            ms.Position = 0;
            pic.Data = TagLib.ByteVector.FromStream(ms);

            if (tagFile.Tag.Pictures.Length == 0)
            {
                tagFile.Tag.Pictures = new TagLib.IPicture[] {pic};
            }

            else
            {
                MemoryStream mem = new MemoryStream();
                pictureBox1.Image.Save(mem, System.Drawing.Imaging.ImageFormat.Jpeg);
                tagFile.Tag.Pictures[0].Data = ms.ToArray();
            }

        }

        private void mymenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if(e.ClickedItem == addCoverOption)
            {
                OpenFileDialog openFileDialog2 = new OpenFileDialog();
                openFileDialog2.Title = "Select an Image File";
                openFileDialog2.Filter = "Image Files(*.bmp; *.jpg; *.jpeg,*.png)| *.BMP; *.JPG; *.JPEG; *.PNG";
                openFileDialog2.Multiselect = false;
                openFileDialog2.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                if (openFileDialog2.ShowDialog() == DialogResult.OK)
                {
                    pictureBox1.Image = Image.FromFile(openFileDialog2.FileName);
                }
            }
            else
            {
                tagFile.Tag.Pictures = null;
                pictureBox1.Image = null;
            }
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm about = new AboutForm();
            about.ShowDialog();
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Select a Music File";
            openFileDialog1.Filter = "Music files |*.mp3; *.m4a";
            openFileDialog1.Multiselect = true;
            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            files.Clear();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                dataGridView1.Rows.Clear();
                foreach (string name in openFileDialog1.FileNames)
                {
                    fillTableFields(name);
                    files.Add(name);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            foreach (Control tb in panel1.Controls)
            {
                if (tb is System.Windows.Forms.TextBox)
                {
                    tb.Text = "";
                }
            }

            pictureBox1.Image = null;
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
    }

    
    

