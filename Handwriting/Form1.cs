using Accord.Neuro;
using Accord.Neuro.Learning;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Handwriting
{
    public partial class Form1 : Form
    {
        ActivationNetwork network;
        PerceptronLearning teacher;

        public Form1()
        {
            InitializeComponent();
            drawingBoard.PenSize = 4;
        }

        private void BtnClearCanvas_Click(object sender, EventArgs e)
        {
            drawingBoard.Clear();
            lblResult.Text = "-";
        }

        private void BtnLoadData_Click(object sender, EventArgs e)
        {
            StringReader reader = new StringReader(File.ReadAllText("Data\\optdigits-tra.txt"));
            int trainingStart = 0;
            int trainingCount = 1500;

            int testingStart = 1500;
            int testingCount = 500;

            int c1 = 0;
            int c2 = 0;
            int trainingSet = 0;
            int testingSet = 0;

            dataGridView1.Rows.Clear();
            dataGridView1.RowTemplate.Height = 40;

            dataGridView2.Rows.Clear();
            dataGridView2.RowTemplate.Height = 40;

            while (true)
            {
                char[] buffer = new char[33 * 32];
                int read = reader.ReadBlock(buffer, 0, buffer.Length);
                string label = reader.ReadLine();

                if (read < buffer.Length || label == null)
                    break;

                if (c1 > trainingStart && c1 <= trainingStart + trainingCount)
                {
                    Bitmap bitmap = Ultilities.Extract(new string(buffer));
                    double[] features = Ultilities.Extract(bitmap);
                    int clabel1 = Int32.Parse(label);
                    dataGridView1.Rows.Add(c1, bitmap, clabel1, features);
                    trainingSet++;
                    
                }
                else if (c1 > testingStart && c1 <= testingStart + testingCount)
                {
                    Bitmap bitmap = Ultilities.Extract(new string(buffer));
                    double[] features = Ultilities.Extract(bitmap);
                    int clabel1 = Int32.Parse(label);
                    dataGridView2.Rows.Add(c2, bitmap, clabel1, features);
                    testingSet++;
                    c2++;
                }
                c1++;
            }
            btnLearn.Enabled = true;
        }

        private double[] Out(int value)
        {
            double[] re = new double[10] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
            re[value] = 1.0;
            return re;
        }

        private void BtnLearn_Click(object sender, EventArgs e)
        {
            int rows = dataGridView1.Rows.Count;
            double[][] input = new double[rows - 1][];
            double[][] output = new double[rows - 1][];
            network = new ActivationNetwork(new ThresholdFunction(), 1024, 10);
            teacher = new PerceptronLearning(network);
            double error = int.MaxValue;
            double lbl;
            for (int i = 0; i < rows - 1; i++)
            {
                input[i] = (double[])dataGridView1.Rows[i].Cells[3].Value;
                lbl = Convert.ToDouble(dataGridView1.Rows[i].Cells[2].Value);
                output[i] = Out(Convert.ToInt32(dataGridView1.Rows[i].Cells[2].Value));
            }
            //int count = 2000;
            while (true)
            {
                error = teacher.RunEpoch(input, output);
                if (error < 0.5)
                    break;
            }
            btnVerify.Enabled = true;
            btnClassify.Enabled = true;
        }

        private void BtnVerify_Click(object sender, EventArgs e)
        {
            int correctCount = 0;
            foreach (DataGridViewRow dgvRows in dataGridView2.Rows)
            {
                if (dgvRows.Cells[2].Value != null)
                {
                    double[] input = (double[])dgvRows.Cells[3].Value;
                    int expected = (int)dgvRows.Cells[2].Value;

                    double[] netOutput = network.Compute(input);
                    int predicted = netOutput.ToList().IndexOf(netOutput.Max());
                    if (predicted == expected)
                    {
                        correctCount++;
                        dgvRows.Cells[0].Style.BackColor = Color.Green;
                        dgvRows.Cells[1].Style.BackColor = Color.Green;
                        dgvRows.Cells[2].Style.BackColor = Color.Green;
                    }
                }
            }
            MessageBox.Show($"Độ chính xác {correctCount}/432", "",
                           MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnClassify_Click(object sender, EventArgs e)
        {
            try
            {
                double[] input = drawingBoard.GetDigit();
                double[] predicted = network.Compute(input);
                int result = predicted.ToList().IndexOf(predicted.Max());
                lblResult.Text = $"{result}";
            }
            catch(Exception ex)
            {
                MessageBox.Show("Hãy huấn luyện trước!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                BtnClearCanvas_Click(this, EventArgs.Empty);
            }
        }

        private void DrawingBoard_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
                BtnClassify_Click(this, EventArgs.Empty);

        }
    }
}
