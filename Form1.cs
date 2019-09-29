using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;

namespace INFOIBV
{
    public partial class INFOIBV : Form
    {
        private Bitmap InputImage;
        private Bitmap InputImage2;
        private Bitmap OutputImage;
        Color[,] Image;
        Color[,] Image2;
        bool doubleProgress = false;
        string modeSize, mode;
        bool[,] potentialEdge, H;
        int[,] outerBound;

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

        private void LoadImageButton2_Click_1(object sender, EventArgs e)
        {
            if (openImageDialog.ShowDialog() == DialogResult.OK)             // Open File Dialog
            {
                string file = openImageDialog.FileName;                     // Get the file name
                imageFileName.Text = file;                                  // Show file name
                if (InputImage2 != null) InputImage2.Dispose();               // Reset image
                InputImage2 = new Bitmap(file);                              // Create new Bitmap from file
                if (InputImage2.Size.Height <= 0 || InputImage2.Size.Width <= 0 ||
                    InputImage2.Size.Height > 512 || InputImage2.Size.Width > 512) // Dimension check
                    MessageBox.Show("Error in image dimensions (have to be > 0 and <= 512)");
            }
        }

        //This project was made by:
        //Steven van Blijderveen	5553083
        //Jeroen Hijzelendoorn		6262279
        //As an assignment to be delivered by at most sunday 22 september 2019

        private void applyButton_Click(object sender, EventArgs e)
        {
            string filter = (string)comboBox1.SelectedItem;
            if (filter == "Structuring element")                            // This function should also work when no image is chosen yet
            {
                mode = comboBox2.Text;
                modeSize = textBox2.Text;
                SetH();

                string message = "Structuring element set as: \n";
                int size = int.Parse(modeSize);
                for (int x = 0; x < size * 2 - 1; x++)
                {
                    for (int y = 0; y < size * 2 - 1; y++)
                    {
                        if (H[x, y])
                        {
                            message += "1 ";
                        }
                        else
                        {
                            message += "0 ";
                        }
                    }
                    message += "\n";
                }

                MessageBox.Show(message, "Structuring element visual", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            bool image2Used = false;
            if (InputImage == null) return;                                 // Get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // Reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // Create new output image
            Image = new Color[InputImage.Size.Width, InputImage.Size.Height];       // Create array to speed-up operations (Bitmap functions are very slow)
            if (InputImage2 != null)
            {
                Image2 = new Color[InputImage2.Size.Width, InputImage2.Size.Height];
                image2Used = true;
            }


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
                    if (image2Used)
                        Image2[x, y] = InputImage2.GetPixel(x, y);
                }
            }

            //==========================================================================================
            // TODO: include here your own code
            // example: create a negative image

            switch (filter)
            {
                case ("Erosion"):
                    ErosionOrDialation(true);
                    break;
                case ("Dilation"):
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
                case ("And"):
                    And();
                    break;
                case ("Or"):
                    Or();
                    break;
                case ("Value counting"):
                    ValueCounting();
                    break;
                case ("Boundary trace"):
                    BoundaryTrace();
                    break;
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
            int size;
            int baseMinColor;
            int rounds;
            try
            {
                size = int.Parse(modeSize) - 1;
                rounds = int.Parse(textBox1.Text);
            }
            catch
            {
                return;
            }

            if (IsErosion)
                baseMinColor = 0;
            else
                baseMinColor = 255;

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
                        for (int i = -(size); i <= size; i++)
                        {
                            for (int j = -(size); j <= size; j++)
                            {
                                if (H[i + size, j + size] && x + i >= 0 && y + j >= 0 && x + i < InputImage.Size.Width && y + j < InputImage.Size.Height) // Do nothing if selected position is out of bounds
                                {
                                    if (IsErosion)
                                        minColor = Math.Max(minColor, OriginalImage[x + i, y + j].R);
                                    else
                                        minColor = Math.Min(minColor, OriginalImage[x + i, y + j].R);
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


        private void And()
        {
            if(InputImage.Size == InputImage2.Size)
                for (int x = 0; x < InputImage.Size.Width; x++)
                {
                    for (int y = 0; y < InputImage.Size.Height; y++)
                    {
                        if (Image[x, y].R == 0 && Image2[x, y].R == 0)
                            Image[x, y] = Color.FromArgb(0,0,0);
                        else
                            Image[x, y] = Color.FromArgb(255,255,255);
                        progressBar.PerformStep();                              // Increment progress bar
                    }
                }
        }

        private void Or()
        {
            if (InputImage.Size == InputImage2.Size)
                for (int x = 0; x < InputImage.Size.Width; x++)
                {
                    for (int y = 0; y < InputImage.Size.Height; y++)
                    {
                        if (Image[x, y].R == 0 || Image2[x, y].R == 0)
                            Image[x, y] = Color.FromArgb(0, 0, 0);
                        else
                            Image[x, y] = Color.FromArgb(255, 255, 255);
                        progressBar.PerformStep();                              // Increment progress bar
                    }
                }
        }

        private void ValueCounting()
        {
            chart1.Series.Clear();
            chart1.ResetAutoValues();
            int[] values = new int[256];
            int valuecounter = 0;
            for (int x = 0; x < InputImage.Size.Width; x++)
             {
                 for (int y = 0; y < InputImage.Size.Height; y++)
                 {
                    int value = Image[x, y].R;
                    if(values[value] == 0)
                    {
                        valuecounter++;
                    }
                    values[value]++;
                    progressBar.PerformStep();                              // Increment progress bar
                 }
            }

            var values1 = chart1.Series.Add("Values");
            for (int i = 0; i < 256; i++)
            {
                values1.Points.AddY(values[i]);
            }

            label1.Text = "Aantal values: " + valuecounter;
        }

        private void BoundaryTrace()
        {
            // For the BoundaryTrace we chose an 8-neighbourhood to determine if a pixel is a boundary
            // This is because we believe that pixels aren't really part of an edge if they aren't directly next to a white pixel
            potentialEdge = new bool[InputImage.Size.Width, InputImage.Size.Height]; // Initialize boolian array to keep track of boundary pixels
            Color[,] OriginalImage = new Color[InputImage.Size.Width, InputImage.Size.Height];   // Duplicate the original image
            bool startFound = false;
            Point start = Point.Empty;

            for (int x = 0; x < InputImage.Size.Width; x++)             
                for (int y = 0; y < InputImage.Size.Height; y++)                
                    OriginalImage[x, y] = Image[x, y];          

            outerBound = new int[InputImage.Size.Width, InputImage.Size.Height];     // Will keep track of the state of boundary pixels in potentialEdge:
            outerBound[start.X, start.Y] = 0;                                               // 0 = not yet visited/ 1 = visited/ 2 = bridge pixel (connection between shapes of 1 pixel)

            for (int x = 0; x < InputImage.Size.Width; x++)                 // Fill in the array of edge pixels
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    if (OriginalImage[x, y].R == 0)
                    {
                        if (!startFound)
                        {
                            start = new Point(x, y);
                            startFound = true;
                        }
                        for (int i = -1; i <= 1; i++)                       // Check the entire 8-neighbourhood for white pixels                        
                            for (int j = -1; j <= 1; j++)
                                if (x + i > 0 && y + j > 0 && x + i < InputImage.Size.Width && y + j < InputImage.Size.Height && OriginalImage[x + i, y + j].R == 255)                                
                                    potentialEdge[x, y] = true;                                                              
                    }
                    progressBar.PerformStep();                              // Increment progress bar
                }

            List<Point> sequence = new List<Point>();
            sequence.Add(start);
            bool[,] temp = new bool[InputImage.Size.Width, InputImage.Size.Height];
            Array.Copy(potentialEdge, temp, potentialEdge.Length);

            sequence = FollowBound(start, sequence, 1, CountBoundaryLength(start, temp));

            string message = "The following coordinates are boundarypixels: \n";
            int counter = 0;

            for (int x = 0; x < InputImage.Size.Width; x++)            
                for (int y = 0; y < InputImage.Size.Height; y++)                
                    if (outerBound[x, y] == 1)                     
                        Image[x, y] = Color.FromArgb(0, 0, 0);

            foreach(Point p in sequence)
            {
                counter++;
                message += "(" + p.X + "," + p.Y + "), ";
                if (counter % 6 == 0)
                    message += "\n";
            }
            string header = "List of boundarypixels";
            MessageBoxButtons buttons = MessageBoxButtons.OK;
            DialogResult result;

            result = MessageBox.Show(message, header, buttons, MessageBoxIcon.Information);
        }

        private int CountBoundaryLength(Point start, bool[,] boundary)                  // Counts how long the outerBound is
        {
            int count = 0;
            for (int i = -1; i <= 1; i++)            
                for (int j = -1; j <= 1; j++)
                {
                    if (start.X + i > 0 && start.Y + j > 0 && start.X + i < InputImage.Size.Width && start.Y + j < InputImage.Size.Height && boundary[start.X + i, start.Y + j])
                    {
                        boundary[start.X + i, start.Y + j] = false;
                        count += CountBoundaryLength(new Point(start.X + i, start.Y + j), boundary) + 1;
                    }
                }
            return count;
        }

        private List<Point> FollowBound(Point p, List<Point> sequence, int currLength, int length)                       // Use potentialEdge to guide the algorithm along the boundary recursively
        {
            Point newP;
            if (p.IsEmpty)
                return sequence;
            if (currLength == length)
                return sequence;
            if (p.X + 1 < InputImage.Size.Width && potentialEdge[p.X + 1, p.Y] && outerBound[p.X + 1, p.Y] != 1)    // Check right neighbour
            {
                outerBound[p.X + 1, p.Y] = 1;
                newP = new Point(p.X + 1, p.Y);
                sequence.Add(newP);
                return FollowBound(newP, sequence, currLength + 1, length);
            }
            else if (p.Y + 1 < InputImage.Size.Height && potentialEdge[p.X, p.Y + 1] && outerBound[p.X, p.Y + 1] != 1)  // Check lower neighbour
            {
                outerBound[p.X, p.Y + 1] = 1;
                newP = new Point(p.X, p.Y + 1);
                sequence.Add(newP);
                return FollowBound(newP, sequence, currLength + 1, length);
            }
            else if (p.X + 1 < InputImage.Size.Width && p.Y + 1 < InputImage.Size.Height && potentialEdge[p.X + 1, p.Y + 1] && outerBound[p.X + 1, p.Y + 1] != 1)   // Check down-right neighbour
            {
                outerBound[p.X + 1, p.Y + 1] = 1;
                newP = new Point(p.X + 1, p.Y + 1);
                sequence.Add(newP);
                return FollowBound(newP, sequence, currLength + 1, length);
            }
            else if (p.X - 1 > 0 && potentialEdge[p.X - 1, p.Y] && outerBound[p.X - 1, p.Y] != 1)    // Check left neighbour
            {
                outerBound[p.X - 1, p.Y] = 1;
                newP = new Point(p.X + 1, p.Y);
                sequence.Add(newP);
                return FollowBound(new Point(p.X + 1, p.Y), sequence, currLength + 1, length);
            }
            else if (p.X - 1 > 0 && p.Y + 1 < InputImage.Size.Height && potentialEdge[p.X - 1, p.Y + 1] && outerBound[p.X - 1, p.Y + 1] != 1)   // Check lower-left neighbour
            {
                outerBound[p.X - 1, p.Y + 1] = 1;
                newP = new Point(p.X - 1, p.Y + 1);
                sequence.Add(newP);
                return FollowBound(newP, sequence, currLength + 1, length);
            }
            else if (p.Y - 1 > 0 && potentialEdge[p.X, p.Y - 1] && outerBound[p.X, p.Y - 1] != 1)   // Check upper neighbour
            {
                outerBound[p.X, p.Y - 1] = 1;
                newP = new Point(p.X, p.Y - 1);
                sequence.Add(newP);
                return FollowBound(newP, sequence, currLength + 1, length);
            }
            else if (p.X - 1 > 0 && p.Y - 1 > 0 && potentialEdge[p.X - 1, p.Y - 1] && outerBound[p.X - 1, p.Y - 1] != 1)    // Check upper-left neighbour
            {
                outerBound[p.X - 1, p.Y - 1] = 1;
                newP = new Point(p.X - 1, p.Y - 1);
                sequence.Add(newP);
                return FollowBound(new Point(p.X - 1, p.Y - 1), sequence, currLength + 1, length);
            }
            else if (p.X + 1 < InputImage.Size.Width && p.Y - 1 > 0 && potentialEdge[p.X + 1, p.Y - 1] && outerBound[p.X + 1, p.Y - 1] != 1)    // Check upper-right neighbour
            {
                outerBound[p.X + 1, p.Y - 1] = 1;
                newP = new Point(p.X + 1, p.Y - 1);
                sequence.Add(newP);
                return FollowBound(newP, sequence, currLength + 1, length);
            }
            else                                                                                    // If no neighbouring edges can be found, but not all edges have been found, backtrack by one and see if there are remaining edges there
            {
                newP = sequence[sequence.Count - 2];
                sequence.Add(newP);
                return FollowBound(newP, sequence, currLength, length);
            }
        }

        private void SetH()
        {
            bool[,] newH = new bool[3, 3];
            int size;
            try
            {
                size = int.Parse(modeSize);                     // Try to get the inputted size - if it's a number
                newH = new bool[size * 2 - 1, size * 2 - 1];
            }
            catch
            {
                return;
            }

            for (int i = 0; i < size * 2 - 1; i++)
            {
                for (int j = 0; j < size * 2 - 1; j++)
                {
                    if (mode == "Plus")
                    {
                        if (i == newH.GetLength(0) / 2 || j == newH.GetLength(1) / 2)
                            newH[i, j] = true;
                    }
                    else if (mode == "Rectangle")
                    {
                        newH[i, j] = true;
                    }
                }
            }            
            H = newH;
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