namespace WindowsFormsApplication1
{
    partial class finalResultTab
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(finalResultTab));
            this.panel1 = new System.Windows.Forms.Panel();
            this.button1 = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.titleCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.trackNumberCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.GenreCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.albumCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.yearCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.artistCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.diskCountCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.diskNumberCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.trackCountCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.checkBox1);
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.dataGridView1);
            this.panel1.Location = new System.Drawing.Point(-1, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(433, 569);
            this.panel1.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(355, 166);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 4;
            this.button1.Text = "Save tag";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(13, 172);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(161, 17);
            this.checkBox1.TabIndex = 3;
            this.checkBox1.Text = "Save with this album artwork";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(42, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Album Artwork";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.titleCol,
            this.trackNumberCol,
            this.GenreCol,
            this.albumCol,
            this.yearCol,
            this.artistCol,
            this.diskCountCol,
            this.diskNumberCol,
            this.trackCountCol});
            this.dataGridView1.Location = new System.Drawing.Point(3, 201);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.Size = new System.Drawing.Size(427, 357);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellClick);
            this.dataGridView1.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellDoubleClick);
            // 
            // titleCol
            // 
            this.titleCol.HeaderText = "Title";
            this.titleCol.Name = "titleCol";
            this.titleCol.ReadOnly = true;
            this.titleCol.Width = 200;
            // 
            // trackNumberCol
            // 
            this.trackNumberCol.HeaderText = "Track Number";
            this.trackNumberCol.Name = "trackNumberCol";
            this.trackNumberCol.ReadOnly = true;
            // 
            // GenreCol
            // 
            this.GenreCol.HeaderText = "Genre";
            this.GenreCol.Name = "GenreCol";
            this.GenreCol.ReadOnly = true;
            // 
            // albumCol
            // 
            this.albumCol.HeaderText = "Album";
            this.albumCol.Name = "albumCol";
            this.albumCol.ReadOnly = true;
            this.albumCol.Visible = false;
            // 
            // yearCol
            // 
            this.yearCol.HeaderText = "Year";
            this.yearCol.Name = "yearCol";
            this.yearCol.ReadOnly = true;
            this.yearCol.Visible = false;
            // 
            // artistCol
            // 
            this.artistCol.HeaderText = "Artist";
            this.artistCol.Name = "artistCol";
            this.artistCol.ReadOnly = true;
            this.artistCol.Visible = false;
            // 
            // diskCountCol
            // 
            this.diskCountCol.HeaderText = "Disk Count";
            this.diskCountCol.Name = "diskCountCol";
            this.diskCountCol.ReadOnly = true;
            this.diskCountCol.Visible = false;
            // 
            // diskNumberCol
            // 
            this.diskNumberCol.HeaderText = "Disk Number";
            this.diskNumberCol.Name = "diskNumberCol";
            this.diskNumberCol.ReadOnly = true;
            this.diskNumberCol.Visible = false;
            // 
            // trackCountCol
            // 
            this.trackCountCol.HeaderText = "Track Count";
            this.trackCountCol.Name = "trackCountCol";
            this.trackCountCol.ReadOnly = true;
            this.trackCountCol.Visible = false;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(13, 25);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(135, 141);
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            // 
            // finalResultTab
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(431, 570);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "finalResultTab";
            this.Text = "Choose track";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.finalResultTab_FormClosed);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn titleCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn trackNumberCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn GenreCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn albumCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn yearCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn artistCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn diskCountCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn diskNumberCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn trackCountCol;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
    }
}