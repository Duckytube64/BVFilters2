using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace INFOIBV
{
    public partial class INFOIBV : Form
    {
        private Bitmap InputImage;
        private Bitmap OutputImage;
        Color[,] Image;
        bool doubleProgress = false;

        public INFOIBV()
        {
            InitializeComponent();
        }

        private void LoadImageButton_Click(object sender, EventArgs e)
        {
            if (openImageDialog.ShowDialog() == DialogResult.OK)             // Open File Dialog
            {
                string file = openImageDialog.FileName;                     // Get the file name
                imageFileName.Text = file;                                  // Show file name
                if (InputImage != null) InputImage.Dispose();               // Reset image
                InputImage = new Bitmap(file);                              // Create new Bitmap from file
                if (InputImage.Size.Height <= 0 || InputImage.Size.Width <= 0 ||
                    InputImage.Size.Height > 512 || InputImage.Size.Width > 512) // Dimension check
                    MessageBox.Show("Error in image dimensions (have to be > 0 and <= 512)");
                else
                    pictureBox1.Image = (Image)InputImage;                 // Display input image
            }
        }

        //This project was made by:
        //Steven van Blijderveen	5553083
        //Jeroen Hijzelendoorn		6262279
        //As an assignment to be delivered by at most sunday 22 september 2019

        private void applyButton_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // Get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // Reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // Create new output image
            Image = new Color[InputImage.Size.Width, InputImage.Size.Height];       // Create array to speed-up operations (Bitmap functions are very slow)

            // Setup progress bar
            progressBar.Visible = true;
            progressBar.Minimum = 1;
            progressBar.Maximum = InputImage.Size.Width * InputImage.Size.Height;
            progressBar.Value = 1;
            progressBar.Step = 1;

            // Copy input Bitmap to array            
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Image[x, y] = InputImage.GetPixel(x, y);                // Set pixel color in array at (x,y)
                }
            }

            //==========================================================================================
            // TODO: include here your own code
            // example: create a negative image

            string filter = (string)comboBox1.SelectedItem;

            switch (filter)
            {                
                case ("Erosion"):
                    ErosionOrDialation(true);
                    break;
                case ("Dialation"):
                    ErosionOrDialation(false);
                    break;
                case ("Opening"):
                    doubleProgress = true;
                    ErosionOrDialation(true);
                    ErosionOrDialation(false);
                    doubleProgress = false;
                    break;
                case ("Closing"):
                    doubleProgress = true;
                    ErosionOrDialation(false);
                    ErosionOrDialation(true);
                    doubleProgress = false;
                    break;
                case ("Complement"):
                    Complement();
                    break;
            /*    case ("And"):
                    And();
                    break;    */
                case ("Nothing"):
                default:
                    break;
            }
            //==========================================================================================

            // Copy array to output Bitmap
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    OutputImage.SetPixel(x, y, Image[x, y]);               // Set the pixel color at coordinate (x,y)
                }
            }

            pictureBox2.Image = (Image)OutputImage;                         // Display output image
            progressBar.Visible = false;                                    // Hide progress bar
        }       

        private void ErosionOrDialation(bool IsErosion)
        {
            bool[,] H = GetH();
            int baseMinColor;
            int rounds;
            try
            {
                rounds = int.Parse(textBox1.Text);
            }
            catch
            {
                return;
            }

            if (IsErosion)
                baseMinColor = 255;
            else
                baseMinColor = 0;

            for (int Nr = 0; Nr < rounds; Nr++)
            {
                Color[,] OriginalImage = new Color[InputImage.Size.Width, InputImage.Size.Height];   // Duplicate the original image
                for (int x = 0; x < InputImage.Size.Width; x++)
                {
                    for (int y = 0; y < InputImage.Size.Height; y++)
                    {
                        OriginalImage[x, y] = Image[x, y];
                    }
                }

                for (int x = 0; x < InputImage.Size.Width; x++)
                {
                    for (int y = 0; y < InputImage.Size.Height; y++)
                    {
                        int minColor = baseMinColor;
                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                if (H[i + 1, j + 1] && x + i >= 0 && y + j >= 0 && x + i < InputImage.Size.Width && y + j < InputImage.Size.Height)
                                {
                                    if (IsErosion)
                                        minColor = Math.Min(minColor, OriginalImage[x + i, y + j].R);
                                    else
                                        minColor = Math.Max(minColor, OriginalImage[x + i, y + j].R);
                                }
                            }
                        }
                        Image[x, y] = Color.FromArgb(minColor, minColor, minColor);         // Set the new pixel color at coordinate (x,y)
                        if (doubleProgress)
                        {
                            if (y % (rounds * 2) == 0)
                                progressBar.PerformStep();                          // Increment progress bar
                        }
                        else if (y % rounds == 0)
                            progressBar.PerformStep();                              // Increment progress bar
                    }
                }
            }
        }

        private void Complement()
        {
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Color pixelColor = Image[x, y];                         // Get the pixel color at coordinate (x,y)
                    Color updatedColor = Color.FromArgb(255 - pixelColor.R, 255 - pixelColor.G, 255 - pixelColor.B); // Negative image
                    Image[x, y] = updatedColor;                             // Set the new pixel color at coordinate (x,y)
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }
        }

       /* private void And()
        {
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    if (Image[x, y] == Image2.GetPixel(x, y))       // Nog een manier maken om een tweede Image te laden
                        Image[x, y] = Image[x, y];
                    else
                        Image[x, y] = Color.White;
                }
            }
        }

    */

        private bool[,] GetH()
        {
            bool[,] H = new bool[3, 3];
            H[0, 0] = checkBox1.Checked;
            H[1, 0] = checkBox2.Checked;
            H[2, 0] = checkBox3.Checked;
            H[0, 1] = checkBox4.Checked;
            H[1, 1] = checkBox5.Checked;
            H[2, 1] = checkBox6.Checked;
            H[0, 2] = checkBox7.Checked;
            H[1, 2] = checkBox8.Checked;
            H[2, 2] = checkBox9.Checked;
            return H;
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (OutputImage == null) return;                                // Get out if no output image
            if (saveImageDialog.ShowDialog() == DialogResult.OK)
                OutputImage.Save(saveImageDialog.FileName);                 // Save the output image
        }

        // Copies right image to the left panel
        private void button1_Click(object sender, EventArgs e)
        {
            if (OutputImage == null) return;                                // Get out if no output image
            pictureBox1.Image = pictureBox2.Image;
            InputImage = new Bitmap(pictureBox2.Image);
        }
    }
}