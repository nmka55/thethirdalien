using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class keywordInputForm : Form
    {
        public keywordInputForm(string artistname)
        {
            InitializeComponent();
            textBox1.Text = artistname;
        }       

        private void button1_Click(object sender, EventArgs e)
        {
            toNextPage();
        }
        
        private void toNextPage()
        {
            if (!String.IsNullOrEmpty(textBox1.Text))
            {
                string keyword = textBox1.Text.Replace(" ", "+");
                briefResult form3 = new briefResult(keyword);
                form3.ShowDialog(this.Owner);
                this.Hide();
            }
            else
                MessageBox.Show("Please, provide the name of the artist to search for tags");
        }
        //if enter keys is pressed form will proceed to next form
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                toNextPage();
            }
        }
        //if form closed by user, it will enable the main form
        private void keywordInputForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                this.Owner.Enabled = true;
                this.Owner.BringToFront();
            }
        }
    }
}
