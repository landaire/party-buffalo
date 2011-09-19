using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CLKsFATXLib;
using Extensions;

namespace Party_Buffalo.Forms
{
    public partial class PropertiesForm : Form
    {

        File xFile;
        Folder xFolder;
        int clicks = 0;
        PropertyGrid propertyGrid1 = new PropertyGrid();
        public PropertiesForm(File f)
        {
            InitializeComponent();
            contentImage1.ContextMenu = c_pic1;
            contentImage2.ContextMenu = c_pic2;
            xFile = f;
            this.Text = "Properties -- " + f.Name;
            if (!f.IsSTFSPackage())
            {
                //The package isn't an stfs package -- disable that shit
                groupBox4.Enabled = false;
            }
            else
            {
                LoadSTFS();
            }
            LoadGeneral(f);
            tabPage1.Click += new EventHandler(tabPage1_Click);
#if TRACE
            //groupBox1.Visible = false;
#endif
#if DEBUG
            TabPage tabPage2 = new TabPage();
                propertyGrid1.Dock = DockStyle.Fill;
                tabPage2.Controls.Add(this.propertyGrid1);
                tabPage2.Location = new System.Drawing.Point(4, 22);
                tabPage2.Name = "tabPage2";
                tabPage2.Padding = new System.Windows.Forms.Padding(3);
                tabPage2.Size = new System.Drawing.Size(509, 320);
                tabPage2.TabIndex = 1;
                tabPage2.Text = "Entry";
                tabPage2.UseVisualStyleBackColor = true;

                tabControl1.TabPages.Add(tabPage2);
#endif
                this.Load += new EventHandler(PropertiesForm_Load);
        }

        void PropertiesForm_Load(object sender, EventArgs e)
        {
            if (xFolder != null)
            {
                tabPage1.Text = xFolder.Name;
            }
            else
            {
                tabPage1.Text = xFile.Name;
            }
        }

        void tabPage1_Click(object sender, EventArgs e)
        {
            clicks++;
            if (clicks == 10 && tabControl1.TabPages.Count == 1)
            {
                TabPage tabPage2 = new TabPage();
                propertyGrid1.Dock = DockStyle.Fill;
                tabPage2.Controls.Add(this.propertyGrid1);
                tabPage2.Location = new System.Drawing.Point(4, 22);
                tabPage2.Name = "tabPage2";
                tabPage2.Padding = new System.Windows.Forms.Padding(3);
                tabPage2.Size = new System.Drawing.Size(509, 320);
                tabPage2.TabIndex = 1;
                tabPage2.Text = "Entry";
                tabPage2.UseVisualStyleBackColor = true;

                tabControl1.TabPages.Add(tabPage2);
            }
        }

        public PropertiesForm(Folder f)
        {
            InitializeComponent();
            xFolder = f;
            this.Text = "Properties -- " + f.Name;
            groupBox4.Enabled = false;
            groupBox1.Enabled = false;
            LoadGeneral(f);
            tabPage1.Click += new EventHandler(tabPage1_Click);
#if DEBUG
            TabPage tabPage2 = new TabPage();
            propertyGrid1.Dock = DockStyle.Fill;
            tabPage2.Controls.Add(this.propertyGrid1);
            tabPage2.Location = new System.Drawing.Point(4, 22);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new System.Windows.Forms.Padding(3);
            tabPage2.Size = new System.Drawing.Size(509, 320);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Entry";
            tabPage2.UseVisualStyleBackColor = true;

            tabControl1.TabPages.Add(tabPage2);
#endif
            this.Load +=new EventHandler(PropertiesForm_Load);
        }

        private void LoadGeneral(Entry xFile)
        {
            string ftype = xFile.EntryType;
            l_fileType.Text = ftype;
            if (!xFile.IsFolder)
            {
                l_size.Text = "0x" + ((File)xFile).Size.ToString("X") + " (" + CLKsFATXLib.VariousFunctions.ByteConversion(((File)xFile).Size) + ")";
                l_sizeOnDisk.Text = "0x" + CLKsFATXLib.VariousFunctions.UpToNearestCluster(((File)xFile).Size, xFile.PartitionInfo.ClusterSize).ToString("X") + " (" + CLKsFATXLib.VariousFunctions.ByteConversion(CLKsFATXLib.VariousFunctions.UpToNearestCluster(((File)xFile).Size, xFile.PartitionInfo.ClusterSize)) + ")";
            }
            else
            {
                l_size.Text = "0x" + ((long)xFolder.BlocksOccupied.Length * xFolder.PartitionInfo.ClusterSize).ToString("X");
                l_sizeOnDisk.Text = l_size.Text;
            }
            l_Created.Text = xFile.CreationDate.ToString();
            l_Modified.Text = xFile.ModifiedDate.ToString();
            l_Accessed.Text = xFile.AccessedDate.ToString();
            l_FullPath.Text = xFile.FullPath;
            toolTip1.SetToolTip(l_FullPath, l_FullPath.Text);
            l_Flags.Text = "";
            CLKsFATXLib.Geometry.Flags[] flags = xFile.Flags;
            for (int i = 0; i < flags.Length; i++)
            {
                if (i == flags.Length - 1)
                {
                    l_Flags.Text += flags[i].ToString();
                }
                else
                {
                    l_Flags.Text += flags[i].ToString() + ", ";
                }
            }
            if (l_Flags.Text == "")
            {
                l_Flags.Text = "None";
            }

            propertyGrid1.SelectedObject = xFile;
        }

        private void LoadSTFS()
        {
            contentTitle.Text = xFile.ContentName();
            gameName.Text = xFile.TitleName(); ;
            profileID.Text = xFile.ProfileID().ToHexString();
            deviceID.Text = xFile.DeviceID().ToHexString();
            consoleID.Text = xFile.ConsoleID().ToHexString();
            titleID.Text = xFile.TitleID().ToString("X");
            if (titleID.Text == "00")
            {
                titleID.Text = "00000000";
            }
            try
            {
                contentImage1.Image = xFile.ContentIcon();
                contentImage2.Image = xFile.TitleIcon();
            }
            catch { }
        }

        private void menuItem1_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PNG(*.png)|*.png";
            sfd.FileName = ContentImageName + " Content Image.png";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                contentImage1.Image.Save(sfd.FileName);
            }
        }

        private void menuItem2_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PNG(*.png)|*.png";
            sfd.FileName = TitleImageName + " Title Image.png";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                contentImage2.Image.Save(sfd.FileName);
            }
        }

        string ContentImageName
        {
            get
            {
                return (xFile.ContentName() != "" && xFile.ContentName() != null) ? xFile.ContentName() : xFile.TitleID().ToString("X2");
            }
        }

        string TitleImageName
        {
            get
            {
                return (xFile.TitleName() != "" && xFile.TitleName() != null) ? xFile.TitleName() : ContentImageName;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            /* Idk, something's wrong with my stream to where even though
             * the offset is at the end of the stream, it's all like NO,
             * WE NEED TO READ MORE! and tries going beyond the end.  Sooo...
             * no MD5.*/
            //System.IO.Stream s = xFile.GetStream();
            //System.Security.Cryptography.MD5CryptoServiceProvider m = new System.Security.Cryptography.MD5CryptoServiceProvider();
            //textBox1.Text = m.ComputeHash(s).ToHexString();
            //s.Close();
            textBox1.Text = new Crc32().ComputeHash(xFile.GetStream()).ToHexString();
        }
    }
}
